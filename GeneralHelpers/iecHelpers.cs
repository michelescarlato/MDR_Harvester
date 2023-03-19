using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Eventing.Reader;
using System.Text.RegularExpressions;

namespace MDR_Harvester.Extensions;

[SuppressMessage("ReSharper", "UnusedMember.Local")]
public class IECHelpers
{
    public List<Criterion>? GetNumberedCriteria(string sid, string? input_string, string type)
    {
        input_string = input_string.StringClean();
        if (string.IsNullOrEmpty(input_string))
        {
            return null;
        }
        
        // Default is to split the input on carriage returns, but it may not have any!
        
        List<string> lines = input_string.Split('\n', 
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries ).ToList();
        
        /*  for testing
        if (sid == "ACTRN12605000041651" || sid == "ACTRN12605000125628"
                                         || sid == "ACTRN12605000274673" || sid == "")
        {
            int aaa = 1;
        }
        */
        
        if (lines.Count == 1)
        {
            // no carriage return separators in the input string...
            // try and split lines using common separators or sequences of separators

            lines = TryToSplitLine(input_string);
        }
        
        int single_crit = type == "inclusion" ? 1 : 2;     
        
        if (lines.Count == 1)   // still unable to split string...
        {
            // return as a single string, though sometimes a single criterion is still numbered.
            
            if (input_string.StartsWith("1.") && !input_string.Contains("2."))
            {
                input_string = input_string[2..].Trim();
            }
            if (input_string.StartsWith("1)") && !input_string.Contains("2)"))
            {
                input_string = input_string[2..].Trim();
            }
              
            int no_sep = single_crit + 1000;
            string no_sep_type = type + " with no separator";
            List<Criterion> single_cr = new() 
                { new Criterion(1, "All", 0, 1, no_sep, no_sep_type, input_string) };
            return single_cr;
        }
        
        // if only a few large lines may be they are each a listing (does happen occasionally!)
        
        if (lines.Count is > 1 and < 5)
        {
            List<string> expanded_lines = new();
            foreach (string l in lines)
            {
                if (l.Length > 750)
                {
                    List<string> possible_lines = TryToSplitLine(l);
                    expanded_lines.AddRange(possible_lines);
                }
                else
                {
                    expanded_lines.Add(l);
                }
            }
            lines = expanded_lines;
        }
        
        // Now send lines to routine to obtain individual criteria
        
        List<Criterion> cr = GetCriteria(lines, type, single_crit);
        List<Criterion> rcr = TryToRepairSplitCriteria(cr, type, single_crit);
        
        return rcr.OrderBy(c => c.SeqNum).ToList();
    }

    public List<Criterion> GetCriteria(List<string> lines, string type, int single_crit)
    {
        // Establish criteria list to receive results,
        // and set up criterion type codes to be used.

        List<Criterion> cr = new();
        int cr_index = 0;
        int post_crit = single_crit + 200;
        int grp_hdr = single_crit + 300;

        string single_type = type + " criterion";
        string post_crit_type = type + " criteria supplementary statement";
        string grp_hdr_type = type + " criteria group heading";

        Regex ressh = new Regex(@"^\d{1,2}\.\d{1,2}\.\d{1,2}\.");
        Regex resh = new Regex(@"^\d{1,2}\.\d{1,2}\.");
        Regex resh1 = new Regex(@"^\d{1,2}\.\d{1,2} ");
        Regex reha = new Regex(@"^[a-z]{1}\.");
        Regex rehadb = new Regex(@"^\([a-z]{1}\)");
        Regex rehab = new Regex(@"^[a-z]{1}\)");
        Regex renha = new Regex(@"^\d{1,2}[a-z]{1} ");
        Regex retab1 = new Regex(@"^-\t");
        Regex retab2 = new Regex(@"^\d{1,2}\t");
        Regex retab3 = new Regex(@"^\uF0A7\t");
        Regex retab4 = new Regex(@"^\*\t");
        Regex retab5 = new Regex(@"^[a-z]\.\t");
        Regex rebrnum = new Regex(@"^\(\d{1,2}\)");
        Regex resbrnum = new Regex(@"^\d{1,2}\)");
        Regex rebrnumdot = new Regex(@"^\d{1,2}\)\.");
        Regex rebrnumcol = new Regex(@"^\d{1,2}\)\:");
        Regex renumdotbr = new Regex(@"^\d{1,2}\.\)");
        Regex resqbrnum = new Regex(@"^\[\d{1,2}\]");
        Regex resnumdash = new Regex(@"^\d{1,2}\-");
        Regex resnumdashb = new Regex(@"^\d{1,2}\-\)");
        Regex rebull = new Regex(@"^[\u2022,\u2023,\u25E6,\u2043,\u2219]");
        Regex rebull1 = new Regex(@"^[\u2212,\u2666,\u00B7,\uF0B7]");
        Regex reso = new Regex(@"^o ");
        Regex reslatbr = new Regex(@"^\(x{0,3}(|ix|iv|v?i{0,3})\)");
        Regex reslat = new Regex(@"^x{0,3}(|ix|iv|v?i{0,3})\)");
        Regex reslatdot = new Regex(@"^x{0,3}(|ix|iv|v?i{0,3})\.");
        Regex redash = new Regex(@"^-");
        Regex restar = new Regex(@"^\*");
        Regex resemi = new Regex(@"^;");
        Regex request = new Regex(@"^\?");
        Regex recrit = new Regex(@"^\d{1,2}\. ");
        Regex recrit1 = new Regex(@"^\d{1,2}\.");
        Regex recrit2 = new Regex(@"^\d{1,2} ");

        int level = 0;
        string oldHdrName = "none";
        List<Level> levels = new() { new Level("none", 0) };

        for (int i = 0; i < lines.Count; i++)
        {
            string this_line = lines[i].TrimPlus()!;
            if (!string.IsNullOrEmpty(this_line)
                && !this_line.Contains(new string('_', 4)))
            {
                this_line = this_line.Replace("..", ".");
                this_line = this_line.Replace(",.", ".");
                this_line = this_line.Replace("\n\n", "\n");

                string hdrName;
                string regex_pattern;
                if (recrit.IsMatch(this_line)) //  Number period and space  1. , 2. 
                {
                    hdrName = "recrit";
                    regex_pattern = @"^\d{1,2}\. ";
                }
                else if (resh.IsMatch(this_line)) // Numeric Sub-heading. N.n.
                {
                    hdrName = "resh";
                    regex_pattern = @"^\d{1,2}\.\d{1,2}\.";
                }
                else if (resh1.IsMatch(this_line)) // Numeric Sub-heading (without final period) N.n
                {
                    hdrName = "resh1";
                    regex_pattern = @"^\d{1,2}\.\d{1,2} ";
                }
                else if (ressh.IsMatch(this_line)) // Numeric Sub-sub-heading. N.n.n.
                {
                    hdrName = "ressh";
                    regex_pattern = @"^\d{1,2}\.\d{1,2}\.\d{1,2}\.";
                }
                else if (reha.IsMatch(this_line)) // Alpha heading. a., b.
                {
                    // care needed here as the first of the roman sequence, i.
                    // and 'v.' in the same sequence, will 'hit' this
                    // regex first and be categorised wrongly....
                    string leader = Regex.Match(this_line, @"^[a-z]{1}\.").Value;

                    // if a real 'i.' current number at this level should be 8 ( = h)
                    // if a real 'v.' current number at this level should be 21 ( = u)
                    if ((leader == "i." && levels[level].levelNum != 8)
                        || leader == "v." && levels[level].levelNum != 21)
                    {
                        hdrName = "reslatdot";
                        regex_pattern = @"^x{0,3}(|ix|iv|v?i{0,3})\.";
                    }
                    else
                    {
                        hdrName = "reha";
                        regex_pattern = @"^[a-z]{1}\.";
                    }
                }
                else if (rehadb.IsMatch(this_line)) // Alpha in brackets. (a), (b)
                {
                    hdrName = "rehadb";
                    regex_pattern = @"^\([a-z]{1}\)";
                }
                else if (rehab.IsMatch(this_line)) // Alpha with right bracket. a), b)
                {
                    hdrName = "rehab";
                    regex_pattern = @"^[a-z]{1}\)";
                }
                else if (renha.IsMatch(this_line)) // Number plus letter  Na, Nb
                {
                    hdrName = "renha";
                    regex_pattern = @"^\d{1,2}[a-z]{1} ";
                }
                else if (retab1.IsMatch(this_line)) // Hyphen followed by tab, -\t, -\t 
                {
                    hdrName = "retab1";
                    regex_pattern = @"^-\t";
                }
                else if (retab2.IsMatch(this_line)) // Number followed by tab, 1\t, 2\t
                {
                    hdrName = "retab2";
                    regex_pattern = @"^\d{1,2}\t";
                }
                else if (retab3.IsMatch(this_line)) // Unknown character followed by tab
                {
                    hdrName = "retab3";
                    regex_pattern = @"^\uF0A7\t";
                }
                else if (retab4.IsMatch(this_line)) // Asterisk followed by tab    *\t, *\t
                {
                    hdrName = "retab4";
                    regex_pattern = @"^\*\t";
                }
                else if (retab5.IsMatch(this_line)) // Alpha-period followed by tab   a.\t, b.\t
                {
                    hdrName = "retab5";
                    regex_pattern = @"^[a-z]\.\t";
                }
                else if (rebrnum.IsMatch(this_line)) // Bracketed numbers (1), (2)
                {
                    hdrName = "rebrnum";
                    regex_pattern = @"^\(\d{1,2}\)";
                }
                else if (resbrnum.IsMatch(this_line)) // number followed by right bracket 1), 2)
                {
                    hdrName = "resbrnum";
                    regex_pattern = @"^\d{1,2}\)";
                }
                else if (rebrnumcol.IsMatch(this_line)) // number followed by colon 1:, 2:
                {
                    hdrName = "rebrnumcol";
                    regex_pattern = @"^\d{1,2}\)\:";
                }
                else if (rebrnumdot.IsMatch(this_line)) // number followed by right bracket and dot 1)., 2).
                {
                    hdrName = "rebrnumdot";
                    regex_pattern = @"^\d{1,2}\)\.";
                }
                else if (renumdotbr.IsMatch(this_line)) // number followed by dot and right bracket  1.), 2.)
                {
                    hdrName = "renumdotbr";
                    regex_pattern = @"^\d{1,2}\.\)";
                }
                else if (resqbrnum.IsMatch(this_line)) //  numbers in square brackets   [1], [2]
                {
                    hdrName = "resqbrnum";
                    regex_pattern = @"^\[\d{1,2}\]";
                }
                else if (resnumdash.IsMatch(this_line)) //  numbers and following dash  1-, 2-
                {
                    hdrName = "resnumdash";
                    regex_pattern = @"^\d{1,2}\-";
                }
                else if (resnumdashb.IsMatch(this_line)) //  numbers and following dash, right bracket  1-), 2-)
                {
                    hdrName = "resnumdashb";
                    regex_pattern = @"^\d{1,2}\-\)";
                }
                else if (restar.IsMatch(this_line)) //  Asterisk only   *, *
                {
                    hdrName = "restar";
                    regex_pattern = @"^\*";
                }
                else if (resemi.IsMatch(this_line)) //  semi-colon only   ;, ; 
                {
                    hdrName = "resemi";
                    regex_pattern = @"^;";
                }
                else if (request.IsMatch(this_line)) //  semi-colon only   ?, ? 
                {
                    hdrName = "request";
                    regex_pattern = @"^\?";
                }
                else if (rebull.IsMatch(this_line)) // various bullets
                {
                    hdrName = "rebull";
                    regex_pattern = @"^[\u2022,\u2023,\u25E6,\u2043,\u2219]";
                }
                else if (rebull1.IsMatch(this_line)) // various bullets
                {
                    hdrName = "rebull1";
                    regex_pattern = @"^[\u2212,\u2666,\u00B7,\uF0B7]";
                }
                else if (reso.IsMatch(this_line)) // open 'o' bullet followed by space
                {
                    hdrName = "reso";
                    regex_pattern = @"^o ";
                }
                else if (reslatbr.IsMatch(this_line)) // roman numerals double bracket
                {
                    hdrName = "reslatbr";
                    regex_pattern = @"^\(x{0,3}(|ix|iv|v?i{0,3})\)";
                }
                else if (reslat.IsMatch(this_line)) // roman numerals right brackets
                {
                    hdrName = "reslat";
                    regex_pattern = @"^x{0,3}(|ix|iv|v?i{0,3})\)";
                }
                else if (reslatdot.IsMatch(this_line)) // roman numerals dots
                {
                    hdrName = "reslatdot";
                    regex_pattern = @"^x{0,3}(|ix|iv|v?i{0,3})\.";
                }
                else if (redash.IsMatch(this_line)) //  Asterisk only
                {
                    hdrName = "redash";
                    regex_pattern = @"^-";
                }
                else if (recrit1.IsMatch(this_line)) //  Number period only - can (very rarely) give false positives
                {
                    hdrName = "recrit1";
                    regex_pattern = @"^\d{1,2}\.";
                }
                else if (recrit2.IsMatch(this_line)) //  Number space only - can (rarely) give false positives
                {
                    hdrName = "recrit2";
                    regex_pattern = @"^\d{1,2} ";

                    // may need to be put back together if a number appears out of sequence

                    string leader = Regex.Match(this_line, @"^\d{1,2} ").Value.Trim();
                    if (int.TryParse(leader, out int leader_num))
                    {
                        if (leader_num != 1 && leader_num != levels[level].levelNum + 1)
                        {
                            hdrName = "none";
                            regex_pattern = @"";
                        }
                    }
                }
                else
                {
                    hdrName = "none";
                    regex_pattern = @"";
                }

                if (hdrName != "none")
                {
                    // Assumed to be a criterion
                    // If the header has changed use the GetLevel function
                    // to obtain the appropriate indent level for the new header type

                    if (hdrName != oldHdrName)
                    {
                        level = GetLevel(hdrName, levels);
                    }

                    levels[level].levelNum++; // sequence number within current level

                    // Store details of line leader and line text as criterion at current level

                    string leader = Regex.Match(this_line, regex_pattern).Value;
                    string clipped_line = Regex.Replace(this_line, regex_pattern, string.Empty).Trim();
                    cr.Add(new Criterion(i + 1, leader, level, levels[level].levelNum,
                        single_crit, single_type, clipped_line));
                    cr_index++;
                }
                else
                {
                    if (i == lines.Count - 1)
                    {
                        // initially at least, add a final line without any 'header' character
                        // as a supplement (at the same indent level as the previous criteria).

                        cr.Add(new Criterion(i + 1, "Spp", level, levels[level].levelNum, post_crit,
                            post_crit_type, this_line));
                        cr_index++;
                    }
                    else
                    {
                        // Otherwise, by default, add a line without any 'header' character as a sub-header
                        // in the list (at the same indent level as the previous criteria) 
                        // though it may be the result of a spurious CR in the text.
                        // if the latter likely to start with lower case and not have a colon at the end.

                        char init = this_line[0];
                        if (cr_index > 0 && !this_line.EndsWith(':')
                                         && !cr[cr_index - 1].CritText!.EndsWith('.')
                                         && char.ToLower(init) == init)
                        {
                            cr[cr_index - 1].CritText += " " + this_line;
                            cr[cr_index - 1].CritText = cr[cr_index - 1].CritText!.Replace("  ", " ");
                        }
                        else
                        {
                            cr.Add(new Criterion(i + 1, "Hdr", level, levels[level].levelNum, grp_hdr,
                                grp_hdr_type, this_line));
                            cr_index++;
                        }
                    }
                }

                oldHdrName = hdrName;
            }
        }

        return cr;
    }

    private List<Criterion> TryToRepairSplitCriteria(List<Criterion> cr, string type, int single_crit)
    {
        // Repair some of the more obvious mis-interpretations
        // for example...
        
        int post_crit = single_crit + 200;
        int grp_hdr = single_crit + 300;
        string single_type = type + " criterion";
        
        if (cr.Count == 2 && cr[0].CritTypeId == grp_hdr
                          && cr[1].CritTypeId == post_crit)
        {
            // More likely that the second is a criterion after the heading
            // rather than a 'supplement' statement.

            cr[1].CritTypeId = single_crit;
            cr[1].CritType = single_type;
            cr[1].Leader = "(1)";
        }

        // Work backwards and re-aggregate lines split with spurious \n.

        List<Criterion> cr2 = new();

        for (int i = cr.Count - 1; i >= 0; i--)
        {
            bool transfer_crit = true; // by default
            string? thisText = cr[i].CritText;

            if (cr[i].CritTypeId == grp_hdr)
            {
                string lowtext = thisText?.ToLower() ?? "";
                if (lowtext is "inclusion criteria" or "inclusion criteria:"
                    || lowtext.Contains("key inclusion criteria") || lowtext.Contains("inclusion criteria include"))
                {
                    transfer_crit = false;
                }

                if (lowtext is "exclusion criteria" or "exclusion criteria:"
                    || lowtext.Contains("key exclusion criteria") || lowtext.Contains("exclusion criteria include"))
                {
                    transfer_crit = false;
                }
            }

            if (!string.IsNullOrEmpty(thisText))
            {
                if (cr[i].CritTypeId == grp_hdr
                    && i < cr.Count - 1 && i > 0
                    && !thisText.EndsWith(':'))
                {
                    // This is really a double check on the aggregation process above
                    // Does the following entry have an indentation level greater than the header? 
                    // if not it is probably not a 'true' header. Add it to the preceding entry...
                    // (N.B. Initial cr[0] is not checked, nor is the last cr entry).

                    if (cr[i].IndentLevel >= cr[i + 1].IndentLevel)
                    {
                        // Almost certainly a spurious \n in the
                        // original string rather than a genuine header.

                        cr[i - 1].CritText += " " + thisText;
                        cr[i - 1].CritText = cr[i - 1].CritText?.Replace("  ", " ");
                        transfer_crit = false;
                    }
                }

                if (cr[i].CritTypeId == post_crit && !thisText.EndsWith(':')
                                                  && !thisText.StartsWith('*')
                                                  && !thisText.ToLower().StartsWith("note")
                                                  && !thisText.ToLower().StartsWith("for further details")
                                                  && !thisText.ToLower().StartsWith("for more information"))
                {
                    // Almost always is a spurious supplement.
                    // Whether should be joined depends on whether there is an initial
                    // lower case or upper case letter... 

                    char init = cr[i].CritText![0];
                    if (char.ToLower(init) == init)
                    {
                        cr[i - 1].CritText += " " + thisText;
                        cr[i - 1].CritText = cr[i - 1].CritText?.Replace("  ", " ");
                        transfer_crit = false;
                    }
                    else
                    {
                        cr[i].CritTypeId = single_crit;
                        cr[i].CritType = single_type;
                        cr[i].IndentLevel = cr[i - 1].IndentLevel;
                        cr[i].LevelSeqNum = cr[i - 1].LevelSeqNum + 1;
                    }
                }

                if (transfer_crit)
                {
                    cr2.Add(cr[i]);
                }
            }
        }
        
        return cr2.OrderBy(c => c.SeqNum).ToList();
    }


    private int GetLevel(string hdr_name, List<Level> levels)
    {
        if (levels.Count == 1)
        {
            levels.Add(new Level(hdr_name, 0));
            return 1;
        }
        
        // See if the level header has been used - if so
        // return level, if not add and return new level

        for (int i = 0; i < levels.Count; i++)
        {
            if (hdr_name == levels[i].levelName)
            {
                return i;
            }
        }
        levels.Add(new Level(hdr_name, 0));
        return levels.Count - 1;
    }

    
    private List<string> TryToSplitLine(string input_string)
    {
        List<string> lines;
        // no carriage return separators in the input string...
        // try and split lines using common separators

        if (input_string.Count(c => c == '\u2022') > 1)
        {
            lines = SplitOnSeperator(input_string, '\u2022'.ToString());
        }
        else if (input_string.Count(c => c == '\u2023') > 1)
        {
            lines = SplitOnSeperator(input_string, '\u2023'.ToString());
        }
        else
        {
            lines = SplitOnSequence(input_string); // examine for sequences of numbers or letters;
            if (lines.Count == 1)
            {
                // still no splitting possible - try a simple semi-colon split, then others
                int semicolon_count = (input_string.Length - input_string.Replace("; ", "").Length) / 2;
                if (semicolon_count > 1)
                {
                    lines = SplitOnSeperator(input_string, "; ");
                }
                else if (input_string.Count(c => c == '?') > 1)
                {
                    lines = SplitOnSeperator(input_string, "?");
                }
                else if (input_string.Count(c => c == '*') > 1)
                {
                    lines = SplitOnSeperator(input_string, "*");
                }
            }
        }
        return lines;
    }

    
    private List<string> SplitOnSeperator(string input_string, string splitter)
    {
        string[] split_lines = input_string.Split(splitter, 
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries );
        
        List<string> lines = new();
        string prefix = splitter == "; " ? ";" : splitter;
        foreach (string l in split_lines)
        {
            lines.Add(prefix + l);
        }
        return lines;
    }
    
    
    private List<string>  SplitOnSequence(string input_string) 
        {
        List<string> split_strings = new();
        
        if (input_string.Contains("1.)") && input_string.Contains("2.)"))
        {
            // check for this numbering style first or it will be masked by the 
            // 1., 2. check below and never found
            
            int pos1 = input_string.IndexOf("1.)", 0, StringComparison.Ordinal);
            int pos2 = input_string.IndexOf("2.)", 0, StringComparison.Ordinal);
            if (pos2 - pos1 > 6)
            {
                string GetStringToFind(int i) => i + ".)";
                string GetNextStringToFind(int i) => (i + 1) + ".)";
                split_strings = split_string(input_string, GetStringToFind, GetNextStringToFind, "");
            }
        }
        
        else if (input_string.Contains("1."))
        {
            // First part finds the position, if any, of "1." that is not a number in the form 1.x
            // Then see if there a "2." that is also not a number in the form of 2.X

            int pos1 = FetchNextButCheckForFollowingDigit(input_string, 0, "1.");
            if (pos1 > -1)
            {
                int pos2 = FetchNextButCheckForFollowingDigit(input_string, pos1 + 3, "2.");
                if (pos2 > -1)
                {
                    // both "1." and "2." found, in the right order
                    // N.B. similar tests for n.X numbers found in the split_strings function

                    string GetStringToFind(int i) => i + ".";
                    string GetNextStringToFind(int i) => (i + 1) + ".";
                    split_strings = split_string(input_string, GetStringToFind, GetNextStringToFind, ".");
                }
            }
        }
        
        else if (input_string.Contains("(1)") && input_string.Contains("(2)"))
        {
            // check for this numbering style first or it will be masked by the 
            // 1), 2) check below and never found
            
            int pos1 = input_string.IndexOf("(1)", 0, StringComparison.Ordinal);
            int pos2 = input_string.IndexOf("(2)", 0, StringComparison.Ordinal);
            if (pos2 - pos1 > 6)
            {
                string GetStringToFind(int i) => "(" + i + ")";
                string GetNextStringToFind(int i) => "(" + (i + 1) + ")";
                split_strings = split_string(input_string, GetStringToFind, GetNextStringToFind, "");
            }
        }
        
        else if (input_string.Contains("1)"))
        {
            // First part finds the position, if any, of "1)" that is not preceded
            // directly by a digit or a dash, or a digit-dot combination
            // Then check for a second valid 2) header

            int pos1 = FetchNextButCheckForPrecedingDigit(input_string, 0, "1)");
            if (pos1 > -1)
            {
                int pos2 = FetchNextButCheckForPrecedingDigit(input_string, pos1 + 3, "2)");
                if (pos2 > -1)
                {
                    // both "1)" and "2)" found, in the right order and format
                    // N.B. similar tests for n) numbers found in the split_strings function

                    string GetStringToFind(int i) => i + ")";
                    string GetNextStringToFind(int i) => (i + 1) + ")";
                    split_strings = split_string(input_string, GetStringToFind, GetNextStringToFind, ")");
                }
            }
        }
        
        else if (input_string.Contains("1/"))
        {
            // First find the position, if any, of "1/" that is not a number in the form 1/x
            // Then see if there is a "2/" that is also not a number in the form of 2/X
            
            int pos1 = FetchNextButCheckForFollowingDigit(input_string, 0, "1/");
            if (pos1 > -1)
            {
                int pos2 = FetchNextButCheckForFollowingDigit(input_string, pos1 + 3, "2/");
                if (pos2 > -1)
                {
                    // both "1/" and "2/" found, in the right order
                    // N.B. similar tests for n.X numbers found in the split_strings function
                    
                    string GetStringToFind(int i) => i + "/";
                    string GetNextStringToFind(int i) => (i + 1) + "/";
                    split_strings = split_string(input_string, GetStringToFind, GetNextStringToFind, "/");
                }
            }
        }
        
        else if (input_string.Contains("1-)") && input_string.Contains("2-)"))
        {
            // check for this numbering style first or it will be masked by the 
            // 1-, 2- check below and never found
            
            string GetStringToFind(int i) => i + "-)";
            string GetNextStringToFind(int i) => (i + 1) + "-)";
            split_strings = split_string(input_string, GetStringToFind, GetNextStringToFind, "");
        }

        else if (input_string.Contains("1-") && input_string.Contains("2-"))
        {
            int pos1 = input_string.IndexOf("1-", 0, StringComparison.Ordinal);
            int pos2 = input_string.IndexOf("2-", 0, StringComparison.Ordinal);
            if (pos2 - pos1 > 5)
            {
                string GetStringToFind(int i) => i + "-";
                string GetNextStringToFind(int i) => (i + 1) + "-";
                split_strings = split_string(input_string, GetStringToFind, GetNextStringToFind, "");
            }
        }
        
        else if (input_string.Contains("1:") && input_string.Contains("2:"))
        {
            string GetStringToFind(int i) => i + ":";
            string GetNextStringToFind(int i) => (i + 1) + ":";
            split_strings = split_string(input_string, GetStringToFind, GetNextStringToFind, "");
        }
     
        else if (input_string.Contains("a)") && input_string.Contains("(b)"))
        {
            // some bracketed letter sequences start with a) rather than (a) 
            
            int pos1 = input_string.IndexOf("a)", 0, StringComparison.Ordinal);
            int pos2 = input_string.IndexOf("(b)", 0, StringComparison.Ordinal);
            if (pos2 - pos1 > 5)
            {
                string GetStringToFind(int i) => i==1 ? (char)(i + 96) + ")" : "(" + (char)(i + 96) + ")";
                string GetNextStringToFind(int i) => "(" + (char)(i + 97) + ")";
                split_strings = split_string(input_string, GetStringToFind, GetNextStringToFind, "");
            }
        }
        
        else if (input_string.Contains("a)") && input_string.Contains("b)"))
        {
            int pos1 = input_string.IndexOf("a)", 0, StringComparison.Ordinal);
            int pos2 = input_string.IndexOf("b)", 0, StringComparison.Ordinal);
            if (pos2 - pos1 > 5)
            {
                string GetStringToFind(int i) => (char)(i + 96) + ")";
                string GetNextStringToFind(int i) => (char)(i + 97) + ")";
                split_strings = split_string(input_string, GetStringToFind, GetNextStringToFind, "");
            }
        }
        
        else if (input_string.Contains("a.") && input_string.Contains("b."))
        {
            int pos1 = input_string.IndexOf("a.", 0, StringComparison.Ordinal);
            int pos2 = input_string.IndexOf("b.", 0, StringComparison.Ordinal);
            if (pos2 - pos1 > 5)
            {
                string GetStringToFind(int i) => (char)(i + 96) + ".";
                string GetNextStringToFind(int i) => (char)(i + 97) + ".";
                split_strings = split_string(input_string, GetStringToFind, GetNextStringToFind, "");
            }
        }
        
        else if (input_string.Contains("(i)") && input_string.Contains("(ii)"))
        {
            string GetStringToFind(int i) => "(" + (roman)i + ")";
            string GetNextStringToFind(int i) => "(" + (roman)(i + 1) + ")";
            split_strings = split_string(input_string, GetStringToFind, GetNextStringToFind, "");
        }
        
        else if (input_string.Contains("i.") && input_string.Contains("ii."))
        {
            string GetStringToFind(int i) => (roman)i + ".";
            string GetNextStringToFind(int i) => (roman)(i + 1) + ".";
            split_strings = split_string(input_string, GetStringToFind, GetNextStringToFind, "");
        }
        
        else if (input_string.Contains("i)") && input_string.Contains("ii)"))
        {
            string GetStringToFind(int i) => (roman)i + ")"; 
            string GetNextStringToFind(int i) => (roman)(i + 1) + ")";
            split_strings = split_string(input_string, GetStringToFind, GetNextStringToFind, "");
        }

        else if (input_string.Contains("1 ") && input_string.Contains("2 "))
        {
            // digits followed by spaces likely to be common
            // therefore check placed near end of list and a check implemented that
            // preceding character is not a letter / number, or a number and decimal point)

            int pos1 = FetchNextButCheckSeparatedFromPreceding(input_string, 0, "1 ");
            if (pos1 > -1)
            {
                int pos2 = FetchNextButCheckSeparatedFromPreceding(input_string, pos1 + 3, "2 ");
                if (pos2 - pos1 > 5)
                {
                    // both "1 " and "2 " found, in the right order and format
                    // N.B. similar tests for n-space numbers found in the split_strings function
                    
                    string GetStringToFind(int i) => i + " ";
                    string GetNextStringToFind(int i) => (i + 1) + " ";
                    split_strings = split_string(input_string, GetStringToFind, GetNextStringToFind, " ");
                }
            }
        }
        
        else if (input_string.Count(c => c == '-') > 2)
        {
            // dashes common as hyphens, therefore this check placed at end of list
            // and 3 or more genuine hyphens are required. Hyphens with spaces will
            // lead to spurious criteria.

            int pos1 = FetchNextButCheckNotHyphen(input_string, 0, "-");
            if (pos1 > -1)
            {
                int pos2 = FetchNextButCheckNotHyphen(input_string, pos1 + 2, "-");
                if (pos2 > -1)
                {
                    int pos3 = FetchNextButCheckNotHyphen(input_string, pos2 + 2, "-");
                    if (pos3 - pos2 > 4 && pos2 - pos1 > 4)
                    {
                        if (!input_string.Trim().StartsWith("-"))
                        {
                            input_string = "-" + input_string; // ensure all lines treated the same
                        }
                        string GetStringToFind(int i) => "-";
                        string GetNextStringToFind(int i) => "-";
                        split_strings = split_string(input_string, GetStringToFind, GetNextStringToFind, "-");
                    }
                }
            }
        }

        if (split_strings.Count == 0)
        {
            split_strings.Add(input_string);  // return input if no split possible
        }
        return split_strings;
    }
    

    private List<string> split_string(string input_string, Func<int, string> GetStringToFind, 
                                 Func<int, string> GetNextStringToFind, string checkChar)
    {
        List<string> split_strings = new();
        string firstheader = GetStringToFind(1);
        int firstheaderpos = input_string.IndexOf(firstheader, 0, StringComparison.Ordinal);
        if (firstheaderpos > 2)   // add any prefix as the initial line, if more than 2 letters
        {
            split_strings.Add(input_string[..firstheaderpos]);   // no leader - therefore a hdr
        }
        
        int i = 1;
        int line_start = 0; int line_end = 0;
        string line = "";
        
        while (line_end > -1)
        {
            string string_to_find = GetStringToFind(i);
            string next_string_to_find = GetNextStringToFind(i);
            line_start = input_string.IndexOf(string_to_find, line_start, StringComparison.Ordinal);
            if (string_to_find == "a)" && line_start > 0 && input_string[line_start - 1] == '(')
            {
                line_start--;   // include the leading bracket if it was there for "a)"
            }

            if (line_start + 5 > input_string.Length)
            {
                // Should be very rare but too near the end of the string to be
                // a viable criterion - amalgamate with the previous line and finish.
                // Last entry in the split strings list will be indexed as [i-1]
                
                line = input_string[line_start..];
                split_strings[i - 1] += line.Trim();
                line_end = -1;
            }
            else
            {  
                if (checkChar is "")   
                {  
                    // the +3 for the start of the search for the next leader is to stop roman
                    // numerals being confused. Otherwise searching for 'v' gets the 'v' in 'iv'...
                    line_end = input_string.IndexOf(next_string_to_find, line_start + 3, StringComparison.Ordinal);
                }
                else if (checkChar is "." or "/")
                {
                    // need to check putative headers for following decimal numbers.
                    line_end = FetchNextButCheckForFollowingDigit(input_string, line_start + 3, next_string_to_find);
                }
                else if (checkChar is ")")
                {
                    // need to check for preceding numbers or a dash mimicking the next_string_to_find.
                    line_end = FetchNextButCheckForPrecedingDigit(input_string, line_start + 3, next_string_to_find);
                }
                else if (checkChar is " ")
                {
                    // need to check preceding characters as not representing a number or letter.
                    line_end = FetchNextButCheckSeparatedFromPreceding(input_string, line_start + 3,
                        next_string_to_find);
                }
                else if (checkChar is "-")
                {
                    // need to check preceding characters as not representing a number or letter.
                    line_end = FetchNextButCheckNotHyphen(input_string, line_start + 3, next_string_to_find);
                }
            }
            
            line = (line_end == -1) ? input_string[line_start..] : input_string[line_start..line_end];
            split_strings.Add(line.Trim());
            line_start = line_end;
            i++;
        }
        return split_strings;
    }

    
    private int FetchNextButCheckForFollowingDigit(string input_string, int string_pos, string string_to_find)
    {   
        int result = -1;
        int spos = string_pos == 0 ? 0 : string_pos + string_to_find.Length;
        while (string_pos < input_string.Length - string_to_find.Length)
        {
            string_pos = input_string.IndexOf(string_to_find, spos, StringComparison.Ordinal);
            if (string_pos == -1 || string_pos >= input_string.Length - string_to_find.Length)
            {
                result = -1;
                break;
            }
            if (!char.IsDigit(input_string[string_pos + string_to_find.Length]))
            {
                result = string_pos;
                break;
            }
            spos = string_pos + string_to_find.Length;
        }
        return result;
    }
    
    private int FetchNextButCheckForPrecedingDigit(string input_string, int string_pos, string string_to_find)
    {
        int result = -1;
        int spos = string_pos == 0 ? 0 : string_pos + string_to_find.Length;
        while (string_pos < input_string.Length - string_to_find.Length)
        {
            string_pos = input_string.IndexOf(string_to_find, spos, StringComparison.Ordinal);
            if (string_pos == -1 || string_pos >= input_string.Length - string_to_find.Length)
            {
                result = -1;
                break;
            }
            bool result_obtained = true;
            if (string_pos > 0)
            {
                char test_char = input_string[string_pos - 1];
                if (test_char == '-' || char.IsDigit(test_char))
                {
                    //preceding digit or dash
                    result_obtained = false;
                }
            }
            if (string_pos > 1)
            {
                char test_char1 = input_string[string_pos - 1];
                char test_char2 = input_string[string_pos - 2];
                if (test_char1 == '.' && char.IsDigit(test_char2))
                {
                    // preceding digit plus period
                    result_obtained = false;
                }
            }
            if (result_obtained)
            {
                result = string_pos;
                break;
            }
            spos = string_pos + string_to_find.Length;
        }
        return result;
    }
    
    private int FetchNextButCheckSeparatedFromPreceding(string input_string, int string_pos, string string_to_find)
    {
        int result = -1;
        int spos = string_pos == 0 ? 0 : string_pos + string_to_find.Length;
        while (string_pos < input_string.Length - string_to_find.Length)
        {
            string_pos = input_string.IndexOf(string_to_find, spos, StringComparison.Ordinal);
            if (string_pos == -1 || string_pos >= input_string.Length - string_to_find.Length)
            {
                result = -1;
                break;
            }
            bool result_obtained = true;
            if (string_pos > 0)
            {
                char test_char = input_string[string_pos - 1];
                if (char.IsDigit(test_char) || char.IsLetter(test_char))
                {
                    //preceding digit or letter
                    result_obtained = false;
                }
            }
            if (string_pos > 1)
            {
                char test_char1 = input_string[string_pos - 1];
                char test_char2 = input_string[string_pos - 2];
                if (test_char1 == '.' && char.IsDigit(test_char2))
                {
                    // preceding digit plus period
                    result_obtained = false;
                }
            }
            if (result_obtained)
            {
                result = string_pos;
                break;
            }
            spos = string_pos + string_to_find.Length;
        }
        return result;
        
    }
    
    private int FetchNextButCheckNotHyphen(string input_string, int string_pos, string string_to_find)
    {
        int result = -1;
        int spos = string_pos == 0 ? 0 : string_pos + string_to_find.Length;
        while (string_pos < input_string.Length - string_to_find.Length)
        {
            string_pos = input_string.IndexOf(string_to_find, spos, StringComparison.Ordinal);
            if (string_pos == -1 || string_pos >= input_string.Length - string_to_find.Length)
            {
                result = -1;
                break;
            }
            bool result_obtained = true;
            if (string_pos > 0)
            {
                char test_char1 = input_string[string_pos - 1];
                char test_char2 = input_string[string_pos + 1];
                if ((char.IsDigit(test_char1) || char.IsLetter(test_char1)) 
                    && (char.IsDigit(test_char2) || char.IsLetter(test_char2)))
                {
                    // character 'squeezed' by alphanumerics each side
                    // therefore likely to be a hyphen
                    result_obtained = false;
                }
            }
            if (result_obtained)
            {
                result = string_pos;
                break;
            }
            spos = string_pos + string_to_find.Length;
        }
        return result;
    }

    
    public record Level
    {
        public string? levelName { get; set; }
        public int levelNum { get; set; }

        public Level(string? _levelName, int _levelNum)
        {
            levelName = _levelName;
            levelNum = _levelNum;
        }
    }

#pragma warning disable CS8981
    private enum roman
#pragma warning restore CS8981
    {
        i = 1, ii, iii, iv, v, vi, vii, viii, ix, x,
        xi, xii, xiii, xiv, xv, xvi, xvii, xviii, xix, xx, 
        xxi, xxii, xxiii, xxiv, xxv
    }
    
    private enum romanCaps
    {
        I = 1, II, III, IV, V, VI, VII, VIII, IX, X,
        XI, XII, XIII, XIV, XV, XVI, XVII, XVIII, XIX, XX,
        XXI, XXII, XXIII, XXIV, XXV
    }
}
