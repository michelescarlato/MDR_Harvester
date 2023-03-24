using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
namespace MDR_Harvester.Extensions;

[SuppressMessage("ReSharper", "UnusedMember.Local")]
public static class IECHelpers
{
    private static readonly Dictionary<string, string> Regexes;
    static IECHelpers()
    {
        // use constructor to set up dictionary of regex expressions

        Regexes = new Dictionary<string, string>()
        { 
           {"recrit", @"^\d{1,2}\. "},                   // number period and space  1. , 2. 
           {"resh", @"^\d{1,2}\.\d{1,2}\."},             // numeric Sub-heading. N.n.
           {"resh1", @"^\d{1,2}\.\d{1,2} "},             // numeric Sub-heading space (without final period) N.n
           {"ressh", @"^\d{1,2}\.\d{1,2}\.\d{1,2}\."},   // numeric Sub-sub-heading. N.n.n.
           {"retab5", @"^[a-z]\.\t"},                    // alpha-period followed by tab   a.\t, b.\t
           {"reha", @"^[a-z]{1}\."},                     // alpha period. a., b.
           {"rehacap", @"^[A-Z]{1}\."},                  // alpha caps period. A., B.
           {"rehadb", @"^\([a-z]{1}\)"},                 // alpha in brackets. (a), (b)
           {"rehab", @"^[a-z]{1}\)"},                    // alpha with right bracket. a), b)
           {"renha", @"^\d{1,2}[a-z]{1} "},              // number plus letter  Na, Nb
           {"retab1", @"^-\t"},                          // hyphen followed by tab, -\t, -\t 
           {"retab2", @"^\d{1,2}\t"},                    // number followed by tab, 1\t, 2\t
           {"retab3", @"^\uF0A7\t"},                     // unknown character followed by tab
           {"retab4", @"^\*\t"},                         // asterisk followed by tab    *\t, *\t
           {"rebrnum", @"^\(\d{1,2}\)"},                 // bracketed numbers (1), (2)
           {"rebrnumdot", @"^\d{1,2}\)\."},              // number followed by right bracket and dot 1)., 2).
           {"resbrnum", @"^\d{1,2}\)"},                  // number followed by right bracket 1), 2)
           {"rebrnumcol", @"^\d{1,2}\:"},                // number followed by colon 1:, 2:
           {"renumdotbr", @"^\d{1,2}\.\)"},              // number followed by dot and right bracket  1.), 2.)
           {"resqbrnum", @"^\[\d{1,2}\]"},               // numbers in square brackets   [1], [2]
           {"resqrtnum", @"^\d{1,2}\]"},                 // numbers with right square bracket   1], 2]
           {"resnumdashb", @"^\d{1,2}\-\)"},             //  numbers and following dash, right bracket  1-), 2-)
           {"resnumdash", @"^\d{1,2}\-"},                //  numbers and following dash  1-, 2-
           {"rebull", @"^[\u2022,\u2023,\u25E6,\u2043,\u2219]"},  // various bullets 1
           {"rebull1", @"^[\u2212,\u2666,\u00B7,\uF0B7]"},        // various bullets 2
           {"reso", @"^o "},                              // open 'o' bullet followed by space
           {"reslatbr", @"^\(x{0,3}(|ix|iv|v?i{0,3})\)"}, // roman numerals double bracket
           {"reslat", @"^x{0,3}(|ix|iv|v?i{0,3})\)"},     // roman numerals right brackets
           {"recrit1", @"^\d{1,2}\."},                    // number period only - can give false positives
           {"recrit2", @"^\d{1,2} "},                      // number space only - can give false positives           
           {"reslatdot", @"^x{0,3}(|ix|iv|v?i{0,3})\."},  // roman numerals dots
           {"redash", @"^-"},                             // dash only   -, -
           {"redoubstar", @"^\*\*"},                      // two asterisks   **, **
           {"restar", @"^\*"},                            // asterisk only   *, *
           {"resemi", @"^;"},                             // semi-colon only   ;, ; 
           {"request", @"^\?"},                           // question mark only   ?, ?
           {"reinvquest", @"^¿"},                         // inverted question mark only   ¿, ¿
           {"reespacenum", @"^E \d{1,2}"},                // exclusion as E numbers  E 01, E 02
           {"reispacenum", @"^I \d{1,2}"},                // inclusion as I numbers  I 01, I 02
        };
         
    }
    
    
    public static List<Criterion>? GetNumberedCriteria(string sid, string? input_string, string type)
    {
        input_string = input_string.StringClean();
        if (string.IsNullOrEmpty(input_string))
        {
            return null;
        }
         
        // Initial task is to create a list of lines, as separated by any carriage returns in the text.
        // The proportion of I/E source strings that are split using carriage returns appears to 
        // vary with the source, but in the majority of cases (the data is split this way.
        // There are, however, many cases of spurious CRs splitting lines that are really one statement, 
        // as well as many examples where the criteria list is provided as a single line, without CRs.
       
        type_values tv = new(type);
        tv.sd_sid = sid;
        List<string> cr_lines = input_string.Split('\n', 
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries ).ToList();

        // do some initial cleaning 

        List<string> cleaned_lines = new();
        foreach (string s in cr_lines)
        {
            string this_line = s.TrimPlus()!;
            if (!string.IsNullOrEmpty(this_line) && !this_line.Contains(new string('_', 4)))
            {
                this_line = this_line.Replace("..", ".");
                this_line = this_line.Replace(",.", ".");
                this_line = this_line.Replace("\n\n", "\n");
                cleaned_lines.Add(this_line);
            }
        }
        
        // then transfer data to list of iec_line structures

        List<iec_line> lines = new();
        if (cleaned_lines.Count == 1)   // No CRs in source
        { 
            lines.Add(new iec_line(1, tv.no_sep, "none", "All", cleaned_lines[0], 0, 1, tv.getSequenceStart() + "0A"));
        } 
        else
        {
            List<iec_line> prelines = new();
            int n = 0;
            foreach (string s in cleaned_lines)
            {
                n++;
                prelines.Add(new iec_line(n, tv.type, "cr", s)); // seq num therefore reflects original ordering of lines
            }

            // prelines used here because of a very rare but possible problem with very short lines
            // may be, or include, 'or' or 'and', or be the result of a spurious CR (e.g. immediately
            // after a line number). In general therefore add such very small lines to the preceding
            // line (unless it is the first line or very short and starts with a number). 
            // N.B. Lines are already trimmed, from above.
            
            for (int j = 0; j < prelines.Count; j++)
            {
                if (prelines[j].text.Length < 6)
                {
                    if (j == 0)
                    {
                        prelines[1].text = prelines[0].text + " " + prelines[1].text;
                    }
                    else if (prelines[j].text.Length < 4 && char.IsDigit(prelines[j].text[0]))
                    {
                        if (j < prelines.Count - 1)
                        {
                            prelines[j + 1].text = prelines[j].text + " " + prelines[j + 1].text;
                        }
                        else
                        {
                            lines.Last().text += " " + prelines[j].text;
                        }
                    }
                    else
                    {
                       lines.Last().text += " " + prelines[j].text;
                    }
                }
                else
                {
                    lines.Add(prelines[j]);
                }
            }

            // Initially try to find leader characters for each split line
            // then try to correct common errors in the list

            List<iec_line> cr_list = IdentifyLineLeaders(lines, tv);
            lines = TryToRepairSplitLines(cr_list, tv);
        }
        
        // then process each line to see if it includes sequences or separators itself
        // if multiple separators then the first occuring needs to be used
        // recursive process ends with a list of criterion objects

        List<iec_line> expanded_lines = new();
        foreach (iec_line l in lines)
        {
            List<iec_line> possible_lines = TryToSplitLine(l, (int)l.indent_level!, tv); // see if a 'composite' line
            if (possible_lines.Count > 1)
            {
                expanded_lines.AddRange(possible_lines); // will be 'split' or 'seq' list of criteria (or both)
            }
            else
            {
                expanded_lines.Add(l); 
            }
        }
  
        List<Criterion> crits = new();
        foreach (iec_line ln in expanded_lines)
        {
            crits.Add(new Criterion(ln.seq_num, ln.type, tv.getTypeName(ln.type), ln.split_type, 
                          ln.leader, ln.indent_level, ln.indent_seq_num, ln.sequence_string, 
                          ln.text.TrimStart(' ', '-', '.', ',')));
        }
        
        return crits.OrderBy(c => c.SequenceString).ToList();
    }

    
    private static List<iec_line> IdentifyLineLeaders(List<iec_line> crLines, type_values tv)
    {
        // Examine each line for possible leader characters.

        int level = 0, num_no_leader = 0;
        string oldLdrName = "none";
        List<Level> levels = new() { new Level("none", 0) };
       
        for (int i = 0; i < crLines.Count; i++)
        {
            string this_line = crLines[i].text;
            string ldrName = "none";     // initial defaults - signify no leader found
            string leader = "";
            
            foreach (KeyValuePair<string, string> r in Regexes)
            {
                string regex_pattern = r.Value;
                if (Regex.Match(this_line, regex_pattern).Success)
                {
                    ldrName = r.Key;
                    leader = Regex.Match(this_line, regex_pattern).Value;

                    // some regex patterns have to have additional checks. In other cases 
                    // simply break out of the loop with the matched pattern value.
                    
                    if (ldrName.StartsWith("reha"))
                    {
                        if (ldrName == "reha")
                        {
                            // hdrName = "reha" by default 
                            // Care needed here as 'i.' and 'v.' in the roman sequence also match
                            // this regex, and will 'hit' it first and thus be categorised wrongly....
                            // Needs to be checked. An 'i. could be the first line in the set, but if not...
                            // if a real 'i.' preceding entry at same level would normally be 'h'
                            // if a real 'v.' preceding entry at same level would normally be 'u'

                            if (leader is "i." or "v.")
                            {
                                string preceding_leader = leader == "i." ? "h." : "u.";
                                int j = 1;
                                while (i - j >= 0 && crLines[i - j].indent_level != level)
                                {
                                    j++; // use to get closest previous entry at same level
                                }
                                if (i == 0 || crLines[i - j].leader != preceding_leader)
                                {
                                    ldrName = "reslatdot";
                                }
                            }
                            else if (leader is "e.")
                            {
                                // a very small chance (though it occurs) that
                                // this is a spurious line beginning with e.g. (will
                                // usually be merged with the line before later in the process)

                                string rest_of_text = this_line[2..];
                                if (rest_of_text.StartsWith("g."))
                                {
                                    ldrName = "none"; // not really a match for anything
                                    leader = "";
                                }
                            }
                            break;
                        }

                        if (ldrName == "rehadb")
                        {
                            // similar issue for this header type (alpha in double brackets) as above
                            // regex is @"^\([a-z]{1}\)"

                            if (leader is "(i)" or "(v)")
                            {
                                string preceding_leader = leader == "(i)" ? "(h)" : "(u)";
                                int j = 1;
                                while (i - j >= 0 && crLines[i - j].indent_level != level)
                                {
                                    j++; // use to get closest previous entry at same level
                                }
                                if (i == 0 || crLines[i - j].leader != preceding_leader)
                                {
                                    ldrName = "reslatbr";
                                }
                            }
                            break;
                        }

                        if (ldrName == "rehab")
                        {
                            // similar issue for this header type (alpha with right bracket) as above
                            // regex is @"^[a-z]{1}\)"

                            if (leader is "i)" or "v)")
                            {
                                string preceding_leader = leader == "i)" ? "h)" : "u)";
                                int j = 1;
                                while (i - j >= 0 && crLines[i - j].indent_level != level)
                                {
                                    j++; // use to get closest previous entry at same level
                                }
                                if (i == 0 || crLines[i - j].leader != preceding_leader)
                                {
                                    ldrName = "reslat";
                                }
                            }
                            break;
                        }

                        if (ldrName == "rehacap")
                        {
                            // regex pattern is @"^[A-Z]{1}\."}

                            if (leader is "N.")
                            {
                                // a very small chance (though it occurs) that
                                // this is a spurious line beginning with N.B. (will
                                // usually be merged with the line before later in the process)

                                string rest_of_text = this_line[2..];
                                if (rest_of_text.StartsWith("B."))
                                {
                                    ldrName = "none"; // not really a match for anything
                                    leader = "";
                                }
                            }
                            break;
                        }
                    }

                    if (ldrName == "resnumdash")
                    {
                        // hdrName = "resnumdash", regex_pattern = @"^\d{1,2}\-" by default 
                        // may need to be put back together if the first character of the text is also
                        // a number - indicates that this is a numeric range (e.g. of age, weight)
                        
                        string rest_of_text = this_line[leader.Length..].Trim();
                        if (char.IsDigit(rest_of_text[0]))
                        {
                            ldrName = "none";   // not really a match for anything
                            leader = "";
                        }
                        break;
                    }

                    if (ldrName == "resh1")
                    {
                        // hdrName = "resh1", regex_pattern = @"^\d{1,2}\.\d{1,2} "
                        // number.period.number space
                        // can be a mistaken match for number-period followed immediately by the 
                        // beginning of the text if it starts with a number.
                        // Need to check the plausibility of the sequence
                        
                         bool genuine = true;  // as the starting point
                         if (i == 0)
                         {
                             genuine = false; // very unlikely to be genuinely a N.n if first in any sequence 
                         }
                         else
                         {
                             string ldr = leader.Trim();
                             int first_dot = ldr.IndexOf(".", 0, StringComparison.Ordinal);
                             string first_num_s = ldr[..first_dot];
                             string second_num_s = ldr[(first_dot + 1)..].Trim();

                             if (int.TryParse(first_num_s, out int first_number)
                                 && int.TryParse(second_num_s, out int second_number))
                             {
                                 // should all parse successfully to here given initial match 

                                 string prev_ldr = crLines[i - 1].leader!;
                                 if (!Regex.Match(prev_ldr, @"^\d{1,2}\.\d{1,2}").Success)
                                 {
                                     // previous line was not N.n, therefore not likely to be a
                                     // genuine N.n leader here unless second number is 1 and 
                                     // first number the same or one more than the previous one.

                                     genuine = false;
                                     if (second_number == 1)
                                     {
                                         if (Regex.Match(prev_ldr, @"\d{1,2}\.").Success)
                                         {
                                             string prev_num_s =
                                                 prev_ldr[..prev_ldr.IndexOf(".", 0, StringComparison.Ordinal)];
                                             if (int.TryParse(prev_num_s, out int prev_number))
                                             {
                                                 if (first_number == prev_number || first_number == prev_number + 1)
                                                 {
                                                     genuine = true;
                                                 }
                                             }
                                         }
                                     }
                                 }
                                 else
                                 {
                                     // previous number was also N.n - therefore highly likely this one is also

                                     genuine = true;
                                 }
                             }
                         }
                         if (!genuine)
                         {
                             // change the found pattern to include only the first number and point
                             ldrName = "recrit1";
                             leader = Regex.Match(this_line, @"^\d{1,2}\.").Value;
                         }
                         break;
                    }

                    if (ldrName == "recrit")
                    {
                         // Turn into recrit1, without the space, to ensure that the header type
                         // remains the same even if there are variations in spacing in the source.
                         
                         ldrName = "recrit1";
                         leader = leader.Trim();
                         break;
                    }
                    
                    if (ldrName == "recrit2")
                    {
                        // hdrName = "recrit2", regex_pattern = @"^\d{1,2} " by default 
                        // may need to be put back together if a number appears out of sequence
                        // Can occur with lines split on carriage returns
                        // (number then almost certainly part of the text, with a preceding carriage return)

                        if (int.TryParse(leader, out int leader_num))
                        {
                            if (leader_num != 1 && leader_num != levels[level].levelNum + 1)
                            {
                                ldrName = "none";
                            }
                        }
  
                    }

                    break; // in all other cases simply break as an appropriate match found
                }
            }

            if (ldrName != "none")
            {
                // If the leader style has changed use the GetLevel function
                // to obtain the appropriate indent level for the new header type

                if (ldrName != oldLdrName)
                {
                    level = GetLevel(ldrName, levels);
                    
                    // if level = 1, (and not the first) have 'returned to a 'top level' leader
                    // the levels array therefore needs to be cleared so that identification of
                    // lower level leaders is kept 'local' to an individual top level element, and 
                    // built up as necessary for each top level element

                    if (level == 1 && levels.Count != 1)
                    {
                        levels.RemoveRange(2, levels.Count - 2);
                    }
                }
                crLines[i].leader = leader;  
                crLines[i].indent_level = level;
                crLines[i].indent_seq_num = ++levels[level].levelNum;  // increment before applying
                crLines[i].text = this_line[leader.Length..].Trim();
            }
            else
            {
                num_no_leader++;   // keep a tally as ALL the lines may be without a leader
                
                if (i == crLines.Count - 1)
                {
                    // initially at least, make this final line without any 'leader' character
                    // a supplement (at the same indent level as the previous criteria).

                    crLines[i].leader = "supp";
                    crLines[i].indent_level = level;
                    crLines[i].indent_seq_num = ++levels[level].levelNum;  // increment before applying
                    crLines[i].type = tv.post_crit;
                }
                else
                {
                    // Otherwise, by default, add a line without any 'header' character as a sub-header
                    // in the list (at the same indent level as the previous criteria) 
                    
                    crLines[i].leader = "Hdr";
                    crLines[i].indent_level = level;
                    crLines[i].indent_seq_num = ++levels[level].levelNum;  // increment before applying
                    crLines[i].type = tv.grp_hdr;
                }
            }
            
            oldLdrName = ldrName;
        }

        // check the 'all without a leader' possibility - allowing a single exception

        if ((crLines.Count > 4 && num_no_leader >= crLines.Count - 1) ||
            (crLines.Count > 1 && num_no_leader == crLines.Count))
        {
            // none of the lines had a leader character. If they (or most of them) had proper 
            // termination then it is possible that they are simply differentiated by the CRs alone...
            
            bool assume_crs_only = false;
            string use_as_header = "";

            int valid_end_chars = 0;
            foreach (var t in crLines)
            {
                char end_char = t.text[^1];
                if (end_char is '.' or ';' or ',')
                {
                    valid_end_chars++;
                }
            }

            if (valid_end_chars >= crLines.Count - 1)
            {
                assume_crs_only = true;
            }

            if (!assume_crs_only)
            {
                int valid_start_chars = 0;
                foreach (var t in crLines)
                {
                    // May be no termination applied but each (can be bar 1) line starts with a capital letter

                    string start_char = t.text[0].ToString();
                    if (start_char == start_char.ToUpper())
                    {
                        valid_start_chars++;
                    }
                }

                if (valid_start_chars >= crLines.Count - 1)
                {
                    assume_crs_only = true;
                }
            }

            if (!assume_crs_only)
            {
                int valid_start_chars = 0;
                if (crLines.Count > 3)
                {
                    foreach (var t in crLines)
                    {
                        // More tentative / risky but if every line starts with a lower case letter
                        // and there are a reasonable number of lines...(4+)
                        // chances are each line can be assumed to be a criterion
                        
                        string start_char = t.text[0].ToString();
                        if (start_char == start_char.ToLower())
                        {
                            valid_start_chars++;
                        }
                    }

                    if (valid_start_chars >= crLines.Count)
                    {
                        assume_crs_only = true;
                    }
                }
            }


            if (!assume_crs_only)
            {
                // a chance that an unknown bullet character has been used to start each line
                // start with the second line (as the first may be different) and see if they are all the same
                // Don't test letters as some people use formulaic criteria all starting with the same word

                char test_char = crLines[1].text[0];
                if (!char.IsLetter(test_char))
                {
                    int valid_start_chars = 0;
                    for (int k = 1; k < crLines.Count; k++)
                    {
                        // May be no termination applied but each line starts with a capital letter

                        char start_char = crLines[k].text[0];
                        if (start_char == test_char)
                        {
                            valid_start_chars++;
                        }
                    }

                    if (valid_start_chars == crLines.Count - 1)
                    {
                        assume_crs_only = true;
                        use_as_header = test_char.ToString();
                    }
                }
            }

            if (assume_crs_only)
            {
                int line_num = 0;
                string leaderString = use_as_header == "" ? "@" : use_as_header;
                for (int n = 0; n < crLines.Count; n++)
                {
                    if (use_as_header != "") // single character only
                    {
                        if (n == 0)
                        {
                            if (crLines[0].text[0].ToString() == use_as_header)
                            {
                                crLines[0].text = crLines[0].text[1..];
                            }
                        }
                        else
                        {
                            if (crLines[n].text.Length >= 2)
                            {
                                crLines[n].text = crLines[n].text[1..];
                            }
                        }
                    }

                    crLines[n].split_type = "cr assumed";   
                    
                    // Identify what appear to be headers but only make initial hdr
                    // have indent 0, if it fits the normal pattern
                    if (crLines[n].text.EndsWith(':') || crLines[n].text == crLines[n].text.ToUpper())
                    {
                        crLines[n].leader = leaderString + "Hdr";
                        crLines[n].type = tv.grp_hdr;

                        if (n == 0)
                        {  
                            crLines[n].indent_level = 0;
                            crLines[n].indent_seq_num = 1;
                        }
                        else
                        {
                            line_num++;
                            crLines[n].indent_level = 1;
                            crLines[n].indent_seq_num = line_num;
                        }
                    }
                    else
                    {
                        line_num++;
                        crLines[n].leader = leaderString;
                        crLines[n].indent_level = 1;
                        crLines[n].indent_seq_num = line_num;
                        crLines[n].type = tv.type;
                    }
                }
            }
        }

        return crLines;
    }


    private static List<iec_line> TryToRepairSplitLines(List<iec_line> crLines, type_values tv)
    {
        // Repair some of the more obvious mis-interpretations
        // Work backwards and re-aggregate lines split with spurious \n.

        List<iec_line> revised_lines = new();

        for (int i = crLines.Count - 1; i >= 0; i--)
        {
            bool transfer_crit = true; // by default
            string? thisText = crLines[i].text;

            // remove simple headings with no information

            if (crLines[i].type == tv.grp_hdr)
            {
                string lowtext = thisText?.ToLower() ?? "";
                if (lowtext is "inclusion:" or "inclusion criteria" or "inclusion criteria:"
                    || lowtext.Contains("key inclusion criteria") || lowtext.Contains("inclusion criteria include"))
                {
                    transfer_crit = false;
                }

                if (lowtext is "exclusion:" or "exclusion criteria" or "exclusion criteria:"
                    || lowtext.Contains("key exclusion criteria") || lowtext.Contains("exclusion criteria include"))
                {
                    transfer_crit = false;
                }
            }

            // Try and identify spurious 'headers' and supplementary lines
            // i.e. lines with no leader characters, that were caused by odd CRs

            if (!string.IsNullOrEmpty(thisText))
            {
                if (crLines[i].type == tv.grp_hdr && i < crLines.Count - 1 && i > 0)
                {
                    // if line starts with 'Note' very likely to be a 'header' giving supp. information
                    // also do not try to merge upward if preceding line ends with ':'

                    if (!thisText.ToLower().StartsWith("note") && !crLines[i - 1].text.EndsWith(':'))
                    {
                        // headers assumed to normally end with ':', but other checks made in addition
                        // (N.B. Initial and last entries are not checked).

                        char initChar = thisText[0];
                        if (!thisText.EndsWith(':'))
                        {
                            // Does the entry following the header have an indentation level greater than the header?,
                            // as would be expected with a 'true' header.
                            // If not, add it to the preceding entry as it is 
                            // likely to be a spurious \n in the original string rather than a genuine header.

                            // Also if no end colon, starts with a lower case letter or digit, and
                            // previous line does not add in a full stop.

                            if (crLines[i].indent_level >= crLines[i + 1].indent_level
                                || (!crLines[i - 1].text.EndsWith('.')
                                    && (char.ToLower(initChar) == initChar || char.IsDigit(initChar))))
                            {
                                // Almost certainly a spurious \n in the
                                // original string rather than a genuine header.

                                crLines[i - 1].text += " " + thisText;
                                crLines[i - 1].text = crLines[i - 1].text.Replace("  ", " ");
                                transfer_crit = false;
                            }
                        }

                        if (thisText.EndsWith(':')
                            && initChar.ToString() == initChar.ToString().ToLower() || char.IsDigit(initChar))
                        {
                            // Header line that has a colon but starts with a lower case letter
                            // merge it 'upwards' to the line before

                            string prev_line = crLines[i - 1].text;
                            char prev_last_char = prev_line[^1];
                            if (prev_last_char is not ('.' or ';' or ':'))
                            {
                                crLines[i - 1].text += " " + thisText;
                                crLines[i - 1].text = crLines[i - 1].text.Replace("  ", " ");
                                crLines[i - 1].type = tv.grp_hdr;
                                transfer_crit = false;
                            }
                        }
                    }
                }

                // check to see if a 'supplement' is better characterised as a normal criterion

                if (crLines[i].type == tv.post_crit && i > 0 
                            && !thisText.EndsWith(':') && !thisText.StartsWith('*') 
                            && !thisText.ToLower().StartsWith("note") && !thisText.ToLower().StartsWith("other ")
                            && !thisText.ToLower().StartsWith("for further details")
                            && !thisText.ToLower().StartsWith("for more information"))
                {
                    // Almost always is a spurious supplement.
                    // Whether should be joined depends on whether there is an initial
                    // lower case or upper case letter... 

                    char initLetter = crLines[i].text[0];
                    if (char.ToLower(initLetter) == initLetter)
                    {
                        crLines[i - 1].text += " " + thisText;
                        crLines[i - 1].text = crLines[i - 1].text.Replace("  ", " ");
                        transfer_crit = false;
                    }
                    else
                    {
                        if (i > 0)
                        {
                            crLines[i].indent_level = crLines[i - 1].indent_level;
                            crLines[i].indent_seq_num = crLines[i - 1].indent_seq_num + 1;
                        }
                    }
                }

                if (transfer_crit)
                {
                    revised_lines.Add(crLines[i]);
                }
            }
        }

        // Put things back in correct order

        revised_lines = revised_lines.OrderBy(c => c.seq_num).ToList();

        if (tv.sd_sid == "PACTR201402000761317")
        {
            int a = 1;
        }
        
        // Clarify situation with one or two criteria only

        if (revised_lines.Count == 1)
        {
            revised_lines[0].seq_num = 1;
            revised_lines[0].split_type = "none";
            revised_lines[0].type = tv.no_sep;
            revised_lines[0].leader = "All";
            revised_lines[0].indent_level = 0;
            revised_lines[0].indent_seq_num = 1;
            revised_lines[0].sequence_string = tv.getSequenceStart() + "0A";
        }
        else if (revised_lines.Count == 2 && revised_lines[0].type == tv.grp_hdr)
        {
            // More likely that these are a pair of criteria statements (or multiple criteria statements)
            // header may be genuine but unusual

            revised_lines[0].seq_num = 1;
            revised_lines[1].seq_num = 2;
            revised_lines[0].split_type = "cr pair";
            revised_lines[1].split_type = "cr pair";

            string top_text = revised_lines[0].text;
            if (!top_text.EndsWith(":")
                && !top_text.ToLower().Contains("criteria"))
            {
                revised_lines[0].type = tv.type;
                revised_lines[0].leader = "-1-";
                revised_lines[0].indent_level = 1;
                revised_lines[0].indent_seq_num = 1;

                revised_lines[1].type = tv.type;
                revised_lines[1].leader = "-2-";
                revised_lines[1].indent_level = 1;
                revised_lines[1].indent_seq_num = 2;
            }
            else
            {
                revised_lines[1].type = tv.type;
                revised_lines[1].leader = "-1-";
                revised_lines[1].indent_level = 1;
                revised_lines[1].indent_seq_num = 1;
            }
        }


        if (revised_lines.Count > 1)
        {
            // Add in sequence strings to try to 
            // ensure numbering is continuous and reflects levels
            
            revised_lines = revised_lines.OrderBy(c => c.seq_num).ThenBy(c => c.indent_seq_num).ToList();
            
            string sequence_start = tv.getSequenceStart(); //starts with e or i (or g)
            int old_level = -1;
            string sequence_base = sequence_start;
            string seq_string = "";
            int[] level_pos = { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            int current_level_pos = 0;

            foreach (iec_line t in revised_lines)
            {
                int level = (int)t.indent_level!; //  assume always non-null
                if (level == 0)
                {
                    seq_string = level_pos[0] > 0
                        ? sequence_start + "0" + level_pos[0]
                        : sequence_start + "00";
                    level_pos[0]++;
                }
                else
                {
                    if (level != old_level)
                    {
                        // a change of level so reset parameters to construct the sequence string

                        if (old_level != -1)
                        {
                            level_pos[old_level] = current_level_pos; // store the most recently used value
                        }

                        if (level == 1)
                        {
                            sequence_base = sequence_start;
                            current_level_pos = level_pos[1];
                        }
                        else
                        {
                            if (level > old_level)
                            {
                                sequence_base = seq_string + "."; // current string plus dot separator
                                current_level_pos = 0;
                            }
                            else
                            {
                                // level less than old level
                                // use current set of values to construct the base
                                sequence_base = sequence_start;
                                for (int b = 1; b < level; b++)
                                {
                                    sequence_base += level_pos[b].ToString("0#") + ".";
                                }

                                current_level_pos = level_pos[level]; // restore the previous value
                            }
                        }

                        old_level = level;
                    }

                    seq_string = sequence_base + (++current_level_pos).ToString("0#");
                }

                t.sequence_string = seq_string;
            }
        }
        return revised_lines;
    }

    
        
    private static int GetLevel(string hdr_name, List<Level> levels)
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


    private static List<iec_line> TryToSplitLine(iec_line iecLine, int loop_depth, type_values tv)
    {
        // Try and split lines using detected sequence or common separators
        // There may be more than one sequencing / splitting mechanism possible
        // Therefore need to investigate what is available and split using the first one that occurs
        // in the string - which could be very different from the first one discovered.
        // Set up a List that can hold the line lList that will be returned after the recursion
        // has ceased, and obtain the details of discovered sequences / splitters, if any found.

        // Function should produce the full set of split lines for any starting line, if that is possible.
        // Initially see if there are any splitters for input line
        // if there are not it should return the line as a List with one member.

        // If there is 1 or more splitters select and apply the relevant one.
        // then call the function recursively on each of the lines in the List of lines created,
        // unless the function has simply returned the single input line, as un-splittable

        List<iec_line> lines = new();
        List<Splitter> splitters = FindSplittersInString(iecLine.text);

        // Decide which splitter, if any, should be used.

        if (splitters.Count == 0)
        {
            lines.Add(iecLine);
        }
        else
        {
            // If there is 1 or more splitters select and apply the relevant one.
            // then call the function recursively on each of the lines in the List of lines created.

            int splitter_index_to_use = 0; // if only one splitter found this will select it automatically.
            if (splitters.Count > 1)
            {
                splitter_index_to_use = 0;
                for (int k = 0; k < splitters.Count; k++)
                {
                    if (splitters[k].pos_starts < splitters[splitter_index_to_use].pos_starts)
                    {
                        splitter_index_to_use = k;
                    }
                }
            }

            Splitter sp = splitters[splitter_index_to_use];
            List<iec_line> split_lines = sp.type == 1
                ? SplitUsingSequence(iecLine, sp.f_start!, sp.f_end!, sp.check_char!, loop_depth, tv) // split on sequence
                : SplitOnSeperator(iecLine, sp.string_splitter!, loop_depth, tv); // split on a separator

            if (split_lines.Count > 1)
            {
                foreach (iec_line ln in split_lines)
                {
                    lines.AddRange(TryToSplitLine(ln, loop_depth + 1, tv));
                }
            }
            else
            {
                lines.Add(iecLine);
            }
        }

        return lines;

    }

    
    private static List<Splitter> FindSplittersInString(string input_string)
    {
        List<Splitter> splitters = new();

        // Try typical separators
        
        int semicolon_count = (input_string.Length - input_string.Replace("; ", "").Length) / 2;
        if (semicolon_count > 2)
        {
            // additional checks here - ensure 3 rather than 2 and reasonable distance apart
            int pos1 =  input_string.IndexOf(';', 0);
            int pos2 =  input_string.IndexOf(';', pos1 + 1);
            int pos3 =  input_string.IndexOf(';', pos1 + 2);
            if (pos3 - pos2 > 10 && pos2 - pos1 > 10)
            {
                splitters.Add(new Splitter(2, input_string.IndexOf("; ", 0, StringComparison.Ordinal), "; "));
            }
        }
        
        if (input_string.Count(c => c == '\u2022') > 1)
        {
            splitters.Add(new Splitter(2, input_string.IndexOf('\u2022', 0), '\u2022'.ToString()));
        }
        
        if (input_string.Count(c => c == '\u2023') > 1)
        {
            splitters.Add(new Splitter(2, input_string.IndexOf('\u2023', 0), '\u2023'.ToString()));
        }
        
        if (input_string.Count(c => c == '?') > 1)
        {
            splitters.Add(new Splitter(2, input_string.IndexOf("?", 0, StringComparison.Ordinal), "?"));
        }
        
        // additional checks here - ensure 3 single asterisks
        int singlestar_count = input_string.Count(c => c == '*')  ;
        int doublestar_count = (input_string.Length - input_string.Replace("**", "").Length) ;
        if (singlestar_count - doublestar_count > 2)
        {
            splitters.Add(new Splitter(2, input_string.IndexOf("*", 0, StringComparison.Ordinal), "*"));
        }

        // then examine possible sequences

        if (input_string.Contains('1') && input_string.Contains('2'))
        {
            // Check for numeric sequences
            // Test 1 (Test 1 has to be checked before test 2 or it will be masked by it.)

            if (input_string.Contains("1.)") && input_string.Contains("2.)"))
            {
                int pos1 = input_string.IndexOf("1.)", 0, StringComparison.Ordinal);
                int pos2 = input_string.IndexOf("2.)", 0, StringComparison.Ordinal);
                if (pos2 - pos1 > 6)
                {
                    string GetStringToFind(int i) => i + ".)";
                    string GetNextStringToFind(int i) => (i + 1) + ".)";
                    splitters.Add(new Splitter(1, pos1, GetStringToFind, GetNextStringToFind, ""));
                }
            }

            // test 2

            if (input_string.Contains("1.") && input_string.Contains("2."))
            {
                // First part finds the position, if any, of "1." that is not a number in the form 1.x
                // Then see if there a "2." that is also not a number in the form of 2.X

                int pos1 = FetchNextButCheckForFollowingDigit(input_string, 0, "1.");
                if (pos1 > -1)
                {
                    int pos2 = FetchNextButCheckForFollowingDigit(input_string, pos1 + 3, "2.");
                    if (pos2 > -1) // both "1." and "2." found, in the right order
                    {
                        string GetStringToFind(int i) => i + ".";
                        string GetNextStringToFind(int i) => (i + 1) + ".";
                        splitters.Add(new Splitter(1, pos1, GetStringToFind, GetNextStringToFind, "."));
                    }
                }
            }

            // test 3

            if (input_string.Contains("(1)") && input_string.Contains("(2)"))
            {
                // Test 3 has to be checked before test 4 or it will be masked by it.

                int pos1 = input_string.IndexOf("(1)", 0, StringComparison.Ordinal);
                int pos2 = input_string.IndexOf("(2)", 0, StringComparison.Ordinal);
                if (pos2 - pos1 > 6)
                {
                    string GetStringToFind(int i) => "(" + i + ")";
                    string GetNextStringToFind(int i) => "(" + (i + 1) + ")";
                    splitters.Add(new Splitter(1, pos1, GetStringToFind, GetNextStringToFind, ""));
                }
            }

            // test 4

            if (input_string.Contains("1)") && input_string.Contains("2)"))
            {
                // Checks the position, if any, of "1)" that is not preceded directly by
                // a digit or a dash, or a digit-dot combination, and then repeats for 2)

                int pos1 = FetchNextButCheckForPrecedingDigit(input_string, 0, "1)");
                if (pos1 > -1)
                {
                    int pos2 = FetchNextButCheckForPrecedingDigit(input_string, pos1 + 3, "2)");
                    if (pos2 > -1) // both "1)" and "2)" found, in the right order and format
                    {
                        string GetStringToFind(int i) => i + ")";
                        string GetNextStringToFind(int i) => (i + 1) + ")";
                        splitters.Add(new Splitter(1, pos1, GetStringToFind, GetNextStringToFind, ")"));
                    }
                }
            }

            // test 5

            if (input_string.Contains("1/") && input_string.Contains("2/"))
            {
                // First find the position, if any, of "1/" that is not a number in the form 1/X
                // Then see if there is a "2/" that is also not a number in the form of 2/X

                int pos1 = FetchNextButCheckForFollowingDigit(input_string, 0, "1/");
                if (pos1 > -1)
                {
                    int pos2 = FetchNextButCheckForFollowingDigit(input_string, pos1 + 3, "2/");
                    if (pos2 > -1) // both "1/" and "2/" found, in the right order
                    {
                        string GetStringToFind(int i) => i + "/";
                        string GetNextStringToFind(int i) => (i + 1) + "/";
                        splitters.Add(new Splitter(1, pos1, GetStringToFind, GetNextStringToFind, "/"));
                    }
                }
            }

            // test 6

            if (input_string.Contains("1-)") && input_string.Contains("2-)"))
            {
                // Test 6 has to be checked before test 7 or it will be masked by it.

                int pos1 = input_string.IndexOf("1-)", 0, StringComparison.Ordinal);
                int pos2 = input_string.IndexOf("2-)", 0, StringComparison.Ordinal);
                if (pos2 - pos1 > 6)
                {
                    string GetStringToFind(int i) => i + "-)";
                    string GetNextStringToFind(int i) => (i + 1) + "-)";
                    splitters.Add(new Splitter(1, pos1, GetStringToFind, GetNextStringToFind, ""));
                }
            }

            // test 7

            if (input_string.Contains("1-") && input_string.Contains("2-"))
            {
                int pos1 = FetchNextButCheckForFollowingDigit(input_string, 0, "1-");
                if (pos1 > -1)
                {
                    int pos2 = FetchNextButCheckForFollowingDigit(input_string, pos1 + 3, "2-");
                    if (pos2 - pos1 > 5)
                    {
                        string GetStringToFind(int i) => i + "-";
                        string GetNextStringToFind(int i) => (i + 1) + "-";
                        splitters.Add(new Splitter(1, pos1, GetStringToFind, GetNextStringToFind, "n-"));
                    }
                }
            }

            // test 8

            if (input_string.Contains("1]") && input_string.Contains("2]"))
            {
                int pos1 = input_string.IndexOf("1]", 0, StringComparison.Ordinal);
                int pos2 = input_string.IndexOf("2]", 0, StringComparison.Ordinal);
                if (pos2 - pos1 > 6)
                {
                    string GetStringToFind(int i) => i + "]";
                    string GetNextStringToFind(int i) => (i + 1) + "]";
                    splitters.Add(new Splitter(1, pos1, GetStringToFind, GetNextStringToFind, ""));
                }
            }

            // test 9

            if (input_string.Contains("1:") && input_string.Contains("2:"))
            {
                int pos1 = input_string.IndexOf("1:", 0, StringComparison.Ordinal);
                int pos2 = input_string.IndexOf("2:", 0, StringComparison.Ordinal);
                if (pos2 - pos1 > 6)
                {
                    string GetStringToFind(int i) => i + ":";
                    string GetNextStringToFind(int i) => (i + 1) + ":";
                    splitters.Add(new Splitter(1, pos1, GetStringToFind, GetNextStringToFind, ""));
                }
            } 
            
            
            // test 10
            
            if (input_string.Contains("1 ") && input_string.Contains("2 ") && input_string.Contains("3 "))
            {
                // digits followed by spaces likely to be common. Three are therefore required.
                // A check also implemented that checks if preceding character is not a letter / number,
                // or a number and decimal point, or the words 'visit', cohort', 'group', stage' or 'phase'

                int pos1 = FetchNextButCheckSeparatedFromPreceding(input_string, 0, "1 ");
                if (pos1 > -1)
                {
                    int pos2 = FetchNextButCheckSeparatedFromPreceding(input_string, pos1 + 3, "2 ");
                    if (pos2 > -1)
                    {
                        int pos3 = FetchNextButCheckSeparatedFromPreceding(input_string, pos2 + 3, "3 ");
                        if (pos3 > -1 && pos3 - pos2 > 5 &&
                            pos2 - pos1 > 5) // "1 ","2 " and "3 " found, in the right order and format
                        {
                            string GetStringToFind(int i) => i + " ";
                            string GetNextStringToFind(int i) => (i + 1) + " ";
                            splitters.Add(new Splitter(1, pos1, GetStringToFind, GetNextStringToFind, " "));
                        }
                    }
                }
            }
        }

        if (input_string.Contains("ii"))
        {
            // check for roman numeral sequences
            // test 11

            if (input_string.Contains("(i)") && input_string.Contains("(ii)"))
            {
                int pos1 = input_string.IndexOf("(i)", 0, StringComparison.Ordinal);
                int pos2 = input_string.IndexOf("(ii)", 0, StringComparison.Ordinal);
                if (pos2 - pos1 > 6)
                {
                    string GetStringToFind(int i) => "(" + (roman)i + ")";
                    string GetNextStringToFind(int i) => "(" + (roman)(i + 1) + ")";
                    splitters.Add(new Splitter(1, pos1, GetStringToFind, GetNextStringToFind, ""));
                }
            }

            // test 12

            if (input_string.Contains("i.") && input_string.Contains("ii."))
            {
                int pos1 = input_string.IndexOf("i.", 0, StringComparison.Ordinal);
                int pos2 = input_string.IndexOf("ii.", 0, StringComparison.Ordinal);
                if (pos2 - pos1 > 6)
                {
                    string GetStringToFind(int i) => (roman)i + ".";
                    string GetNextStringToFind(int i) => (roman)(i + 1) + ".";
                    splitters.Add(new Splitter(1, pos1, GetStringToFind, GetNextStringToFind, ""));
                }
            }

            // test 13

            if (input_string.Contains("i)") && input_string.Contains("ii)"))
            {
                int pos1 = input_string.IndexOf("i)", 0, StringComparison.Ordinal);
                int pos2 = input_string.IndexOf("ii)", 0, StringComparison.Ordinal);
                if (pos2 - pos1 > 6)
                {
                    string GetStringToFind(int i) => (roman)i + ")";
                    string GetNextStringToFind(int i) => (roman)(i + 1) + ")";
                    splitters.Add(new Splitter(1, pos1, GetStringToFind, GetNextStringToFind, ""));
                }
            }
        }

        if (input_string.Contains(')'))
        {   
            // check for some remaining alpha based sequences
            // test 14
            
            if (input_string.Contains("a)") && input_string.Contains("(b)"))
            {
                // some bracketed letter sequences start with a) rather than (a) 

                int pos1 = input_string.IndexOf("a)", 0, StringComparison.Ordinal);
                int pos2 = input_string.IndexOf("(b)", 0, StringComparison.Ordinal);
                if (pos2 - pos1 > 5)
                {
                    string GetStringToFind(int i) => i == 1 ? (char)(i + 96) + ")" : "(" + (char)(i + 96) + ")";
                    string GetNextStringToFind(int i) => "(" + (char)(i + 97) + ")";
                    splitters.Add(new Splitter(1, pos1, GetStringToFind, GetNextStringToFind, ""));
                }
            }
        
            // test 15

            if (input_string.Contains("a)") && input_string.Contains("b)"))
            {
                int pos1 = input_string.IndexOf("a)", 0, StringComparison.Ordinal);
                int pos2 = input_string.IndexOf("b)", 0, StringComparison.Ordinal);
                if (pos2 - pos1 > 5)
                {
                    string GetStringToFind(int i) => (char)(i + 96) + ")";
                    string GetNextStringToFind(int i) => (char)(i + 97) + ")";
                    splitters.Add(new Splitter(1, pos1, GetStringToFind, GetNextStringToFind, ""));
                }
            }
        }

        // test 16

        if (input_string.Contains("a.") && input_string.Contains("b."))
        {
            int pos1 = input_string.IndexOf("a.", 0, StringComparison.Ordinal);
            int pos2 = input_string.IndexOf("b.", 0, StringComparison.Ordinal);
            if (pos2 - pos1 > 5)
            {
                string GetStringToFind(int i) => (char)(i + 96) + ".";
                string GetNextStringToFind(int i) => (char)(i + 97) + ".";
                splitters.Add(new Splitter(1, pos1, GetStringToFind, GetNextStringToFind, ""));
            }
        }
        
        // test 17

        if (input_string.Contains("A.") && input_string.Contains("B."))
        {
            int pos1 = input_string.IndexOf("A.", 0, StringComparison.Ordinal);
            int pos2 = input_string.IndexOf("B.", 0, StringComparison.Ordinal);
            if (pos2 - pos1 > 6)
            {
                string GetStringToFind(int i) => (char)(i + 64) + ".";
                string GetNextStringToFind(int i) => (char)(i + 65) + ".";
                splitters.Add(new Splitter(1, pos1, GetStringToFind, GetNextStringToFind, ""));
            }
        }

        // test 18
        
        if (input_string.Count(c => c == '-') > 2)
        {
            // dashes common as hyphens, therefore 3 or more genuine hyphens are required.
            // Hyphens without accompanying spaces will lead to spurious criteria.

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
                        splitters.Add(new Splitter(1, pos1, GetStringToFind, GetNextStringToFind, "-"));
                    }
                }
            }
        }

        // Test 19
        
        if (input_string.Count(c => c == '¿') > 2)
        {
            // ¿ without accompanying spaces can lead to spurious criteria.
            // Therefore check a space on at least one side of the ¿ (same as hyphen)

            int pos1 = FetchNextButCheckNotHyphen(input_string, 0, "¿");
            if (pos1 > -1)
            {
                int pos2 = FetchNextButCheckNotHyphen(input_string, pos1 + 2, "¿");
                if (pos2 > -1)
                {
                    int pos3 = FetchNextButCheckNotHyphen(input_string, pos2 + 2, "¿");
                    if (pos3 - pos2 > 4 && pos2 - pos1 > 4)
                    {
                        if (!input_string.Trim().StartsWith("¿"))
                        {
                            input_string = "-" + input_string; // ensure all lines treated the same
                        }

                        string GetStringToFind(int i) => "¿";
                        string GetNextStringToFind(int i) => "¿";
                        splitters.Add(new Splitter(1, pos1, GetStringToFind, GetNextStringToFind, "¿"));
                    }
                }
            }
        }
        
        return splitters;
    }
    

    private static List<iec_line> SplitOnSeperator(iec_line line, string splitter, int loop_depth, type_values tv)
    {
        string input_string = line.text;
        string seq_base = line.type == tv.no_sep ? tv.getSequenceStart() + "01."  : line.sequence_string + ".";
        
        string[] split_lines = input_string.Split(splitter, 
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries );
        
        // previous check means that there should be at least 2 members of lines
        
        List<iec_line> lines = new();
        string prefix = splitter == "; " ? ";" : splitter;
        int n = 1;
        foreach (string l in split_lines)
        {
            lines.Add(new iec_line(line.seq_num, tv.type, "split", prefix, l, loop_depth + 1, n, 
                seq_base + n.ToString("0#")));
            n++;
        }
        return lines;
    }
    

    private static List<iec_line> SplitUsingSequence(iec_line input_line, Func<int, string> GetStringToFind, 
                                 Func<int, string> GetNextStringToFind, string checkChar, int loop_depth, type_values tv)
    {
        string input_string = input_line.text;
        List<iec_line> split_strings = new();
        string seq_base = input_line.type == tv.no_sep ? tv.getSequenceStart() + "01."  : input_line.sequence_string + ".";
        int level_seq_num = 0;
        string firstLeader = GetStringToFind(1);
        int firstLeaderPos = input_string.IndexOf(firstLeader, 0, StringComparison.Ordinal);
        if (firstLeaderPos > 2)   // add any prefix as the initial line, if more than 2 letters
        {
            ++level_seq_num;
            split_strings.Add(new iec_line(input_line.seq_num, input_line.type, "seq", "Hdr", 
                            input_string[..firstLeaderPos], loop_depth + 1, level_seq_num, 
                            seq_base + level_seq_num.ToString("0#")));   // no leader - therefore a hdr

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
                line_start--; // include the leading bracket if it was there for "a)"
            }

            if (line_start + 5 > input_string.Length)
            {
                // Should be very rare but too near the end of the string to be
                // a viable criterion - amalgamate with the previous line and finish.
                // Last entry in the split strings list will be indexed as [i-2]
                // at this stage, as i has increased but nothing has yet been added
                // to split_strings on this loop.

                line = input_string[line_start..];
                if (split_strings.Count > 0)
                {
                    split_strings[^1].text += line.Trim();  // add to last string
                }
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
                else if (checkChar is "." or "/" or "n-")
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
                else if (checkChar is "-" or "¿")
                {
                    // need to check preceding characters as not representing a number or letter.
                    line_end = FetchNextButCheckNotHyphen(input_string, line_start + 3, next_string_to_find);
                }

                line = (line_end == -1) ? input_string[line_start..] : input_string[line_start..line_end];
                if (line.Length > string_to_find.Length)
                {
                    line = line[string_to_find.Length..].Trim();
                }

                if (line.Contains(':') && !line.EndsWith(':') && line.Length > 50)
                {
                    // return the line in two parts
                    
                    int colon_pos = line.IndexOf(":", 0, StringComparison.Ordinal);
                    string first_part = line[..colon_pos];
                    string second_part = line[(colon_pos + 1)..];
                    
                    ++level_seq_num;
                    split_strings.Add(new iec_line(input_line.seq_num, tv.type, "seq", "Hdr", 
                        first_part, loop_depth+ 1, level_seq_num, seq_base + level_seq_num.ToString("0#")));   
                    
                    ++level_seq_num;
                    split_strings.Add(new iec_line(input_line.seq_num, tv.type, "seq", string_to_find, 
                        second_part, loop_depth+ 1, level_seq_num, seq_base + level_seq_num.ToString("0#")));   
                }
                else
                {
                    ++level_seq_num;
                    split_strings.Add(new iec_line(input_line.seq_num, tv.type, "seq", string_to_find, 
                        line, loop_depth+ 1, level_seq_num, seq_base + level_seq_num.ToString("0#")));   
                }

                line_start = line_end;
                i++;
            }
        }

        return split_strings;
    }

    
    private static int FetchNextButCheckForFollowingDigit(string input_string, int string_pos, string string_to_find)
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
    
    private static int FetchNextButCheckForPrecedingDigit(string input_string, int string_pos, string string_to_find)
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
    
    private static int FetchNextButCheckSeparatedFromPreceding(string input_string, int string_pos, string string_to_find)
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
                    // immediately preceding digit or letter
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
            if (string_pos > 6)
            {
                string test_word = input_string[(string_pos - 6)..string_pos].ToLower();
                if (test_word is "visit " or "group " or "stage " or " part "
                    or "phase " or "ohort ")
                {
                    // number is part of preceding word 
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
    
    private static int FetchNextButCheckNotHyphen(string input_string, int string_pos, string string_to_find)
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
                    // therefore likely to be a hyphen, or an inverted question mark 
                    // standing in for an unrecognised character.
                    
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


    private class type_values
    {
        public int type { get; set; }
        public int post_crit { get; set; }
        public int grp_hdr { get; set; }
        public int no_sep { get; set; }
        public string type_name { get; set; }
        public string post_crit_name { get; set; }
        public string grp_hdr_name{ get; set; }
        public string no_sep_name{ get; set; }
        public string sd_sid{ get; set; }
        
        public type_values(string type_stem)
        {
            type = type_stem switch
            {
                "inclusion" => 1,
                "exclusion" => 2,
                "eligibility" => 3,
                _ => 4
            };
            post_crit = type + 200;
            grp_hdr = type + 300;
            no_sep = type + 1000;
            type_name = type_stem + " criterion";
            post_crit_name = type_stem + " supplementary statement";
            grp_hdr_name = type_stem + " criteria group heading";
            no_sep_name = type_stem + " criteria with no separator";
        }

        public string getTypeName(int typeId)
        {
            type_name = "??";
            char type_number = typeId.ToString()[^1];
            string type_stem = type_number switch
            {
                '1' => "inclusion",
                '2' => "exclusion",
                '3' => "eligibility",
                _ => "??"
            };
            type_name = typeId switch
            {
                > 1000 => type_stem + " criteria with no separator",
                > 300 => type_stem + " criteria group heading",
                > 200 => type_stem + " supplementary statement",
                _ => type_stem + " criterion"
            };

            return type_name;
        }
        
        public string getSequenceStart()
        {
            return type switch
            {
                1 => "n.",
                2 => "e.",
                3 => "g.",
                _ => "??"
            };
        }
    }
    
    private class iec_line
    {
        public int seq_num { get; set; }
        public int type { get; set; }
        public string split_type { get; set; }
        public string? leader { get; set; }
        public int? indent_level { get; set; }
        public int? indent_seq_num { get; set; }
        public string? sequence_string { get; set; }
        public string text { get; set; }
       
        public iec_line(int _seq_num, int _type, string _split_type, string _leader,
                        string _text, int? _indent_level, int? _indent_seq_num, string? _sequence_string)
        {
            seq_num = _seq_num;
            type = _type;            
            split_type = _split_type;
            leader = _leader;
            text = _text;
            indent_level = _indent_level;
            indent_seq_num = _indent_seq_num;
            sequence_string = _sequence_string;
        }
        
        public iec_line(int _seq_num, int _type, string _split_type, string _text)
        {
            seq_num = _seq_num;
            type = _type;            
            split_type = _split_type;
            text = _text;
        }
    }

    private class Splitter
    {
        public int type  { get; set; }               // 1 = sequence, 2 = splitter
        public int pos_starts  { get; set; }         // 0 based in string for first position of separator
        public Func<int, string>? f_start { get; set; }
        public Func<int, string>? f_end  { get; set; }
        public string? check_char  { get; set; }
        public string? string_splitter { get; set; }
        
        public Splitter(int _type, int _pos_starts, Func<int, string>? _f_start, 
                         Func<int, string>? _f_end, string? _check_char)
        {
            type = _type;
            pos_starts = _pos_starts;
            f_start = _f_start;
            f_end = _f_end;
            check_char = _check_char;
        }
       
        public Splitter(int _type, int _pos_starts, string? _string_splitter)
        {
            type = _type;
            pos_starts = _pos_starts;
            string_splitter = _string_splitter;
        }
        
  }
    
    
    private record Level
    {
        public string? levelName { get; set; }
        public int levelNum { get; set; }

        public Level(string? _levelName, int _levelNum)
        {
            levelName = _levelName;
            levelNum = _levelNum;
        }
    }
    
    private record seqLevel
    {
        public string? levelName { get; set; }
        public int levelNum { get; set; }

        public seqLevel(string? _levelName, int _levelNum)
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
