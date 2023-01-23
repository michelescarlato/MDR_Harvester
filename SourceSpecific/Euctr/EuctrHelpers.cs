using System.Text.RegularExpressions;
using MDR_Harvester.Extensions;

namespace MDR_Harvester.Euctr;

public static class EuctrExtensions
{
    public static bool NameAlreadyPresent(this string candidate_name, List<StudyTitle> titles)
    {
        if (titles.Count == 0)
        {
            return false;
        }
        else
        {
            bool res = false;
            foreach (StudyTitle t in titles)
            {
                if (t.title_text?.ToLower() == candidate_name.ToLower())
                {
                    res = true;
                    break;
                }
            }

            return res;
        }
    }

    public static bool IMPAlreadyThere(this string imp_name, List<StudyTopic> topics)
    {
        if (topics.Count == 0)
        {
            return false;
        }
        else
        {
            bool res = false;
            foreach (StudyTopic t in topics)
            {
                if (imp_name.ToLower() == t.original_value?.ToLower())
                {
                    res = true;
                    break;
                }
            }

            return res;
        }
    }

    public static string? GetLanguageFromMemberState(this string? member_state)
    {
        if (string.IsNullOrEmpty(member_state))
        {
            return null;
        }

        string ms_lc = member_state.ToLower();
        string seclang = ms_lc switch
        {
            _ when ms_lc.Contains("spain")
                   || ms_lc.Contains("span") => "es",
            _ when ms_lc.Contains("portug") => "pt",
            _ when ms_lc.Contains("france")
                   || ms_lc.Contains("french") => "fr",
            _ when ms_lc.Contains("german")
                   || ms_lc.Contains("liecht")
                   || ms_lc.Contains("austri") => "de",
            _ when ms_lc.Contains("ital") => "it",
            _ when ms_lc.Contains("dutch")
                   || ms_lc.Contains("neder")
                   || ms_lc.Contains("nether") => "nl",
            _ when ms_lc.Contains("danish")
                   || ms_lc.Contains("denm") => "da",
            _ when ms_lc.Contains("swed") => "sv",
            _ when ms_lc.Contains("norw") => "no",
            _ when ms_lc.Contains("fin") => "fi",
            _ when ms_lc.Contains("icelan") => "is",
            _ when ms_lc.Contains("polish") => "pl",
            _ when ms_lc.Contains("hungar") => "hu",
            _ when ms_lc.Contains("czech") => "cs",
            _ when ms_lc.Contains("slovak") => "sk",
            _ when ms_lc.Contains("sloven") => "sl",
            _ when ms_lc.Contains("greece")
                   || ms_lc.Contains("greek")
                   || ms_lc.Contains("cypr") => "el",
            _ when ms_lc.Contains("eston") => "et",
            _ when ms_lc.Contains("latv") => "lv",
            _ when ms_lc.Contains("lithu") => "lt",
            _ when ms_lc.Contains("croat") => "hr",
            _ when ms_lc.Contains("roman") => "ro",
            _ when ms_lc.Contains("bulga") => "bg",
            _ => "??"
        };

        return seclang;
    }
}
/*
internal class EuctrHelpers
{
    public List<Criterion>? GetNumberedCriteria(string sid, string input_string, string type)
    {
        if (string.IsNullOrEmpty(input_string))
        {
            return null;
        }
        else
        {
            // Establish criteria list to receive results,
            // and set up criterion type codes to be used.

            List<Criterion> cr = new();

            int single_crit = type == "inclusion" ? 1 : 2;
            int all_crit = single_crit + 10;
            int pre_crit = single_crit + 100;
            int post_crit = single_crit + 200;
            int grp_hdr = single_crit + 300;
            int no_sep = single_crit + 1000;

            string single_type = type + " criterion";
            string all_crit_type = type + " criteria (as one statement)";
            string pre_crit_type = type + " criteria prefix statement";
            string post_crit_type = type + " criteria supplementary statement";
            string grp_hdr_type = type + " criteria group heading";
            string no_sep_type = type + " with no separator";

            string[] lines = input_string.Split('\n');
            if (lines.Length == 1)
            {
                // no carriage return separators in the input string...
                cr.Add(new Criterion(1, "All", 0, 1, no_sep, no_sep_type, input_string));
                return cr;
            }
            else
            {
                Regex ressh = new Regex(@"^\d{1,2}\.\d{1,2}\.\d{1,2}\.");
                Regex resh = new Regex(@"^\d{1,2}\.\d{1,2}\.");
                Regex resh1 = new Regex(@"^\d{1,2}\.\d{1,2} ");
                Regex reha = new Regex(@"^[a-z]{1}\.");
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
                Regex resqbrnum = new Regex(@"^\[\d{1,2}\]");
                Regex rebull = new Regex(@"^[\u2022,\u2023,\u25E6,\u2043,\u2219]");
                Regex rebull1 = new Regex(@"^[\u2212,\u2666,\u00B7,\uF0B7]");
                Regex reso = new Regex(@"^o ");
                Regex reslat = new Regex(@"^x{0,3}(ix|iv|v?i{0,3})\)");
                Regex redash = new Regex(@"^-");
                Regex restar = new Regex(@"^\*");                
                Regex recrit = new Regex(@"^\d{1,2}\. ");
                Regex recrit1 = new Regex(@"^\d{1,2}\.");

                int level = 0;
                string hdr_name = "";
                string old_hdr_name = "none";
                string regex_pattern, leader, clipped_line =  "";
                List<Level> levels = new(){new Level("none", 0)}; 
               
                for (int i = 0; i < lines.Length; i++)
                {
                    string? this_line = lines[i].TrimPlus()!;
                    if (!string.IsNullOrEmpty(this_line)
                        && !this_line.Contains(new string('_', 4)))
                    {
                        this_line = this_line.Replace("..", ".");
                        this_line = this_line.Replace(",.", ".");
                        this_line = this_line.Replace("\n\n", "\n");

                        if (recrit.IsMatch(this_line)) //  Number period and space  1. , 2. 
                        {
                            hdr_name = "recrit";
                            regex_pattern = @"^\d{1,2}\. ";
                        }
                        else if (resh.IsMatch(this_line)) // Numeric Sub-heading. N.n.
                        {
                            hdr_name = "resh";
                            regex_pattern = @"^\d{1,2}\.\d{1,2}\.";
                        }
                        else if (resh1.IsMatch(this_line)) // Numeric Sub-heading (without final period) N.n
                        {
                            hdr_name = "resh1";
                            regex_pattern = @"^\d{1,2}\.\d{1,2} ";
                        }
                        else if (ressh.IsMatch(this_line)) // Numeric Sub-sub-heading. N.n.n.
                        {
                            hdr_name = "ressh";
                            regex_pattern = @"^\d{1,2}\.\d{1,2}\.\d{1,2}\.";
                        }
                        else if (reha.IsMatch(this_line)) // Alpha heading. a., b.
                        {
                            hdr_name = "reha";
                            regex_pattern = @"^[a-z]{1}\.";
                        }
                        else if (rehab.IsMatch(this_line)) // Alpha heading. a), b)
                        {
                            hdr_name = "rehab";
                            regex_pattern = @"^[a-z]{1}\)";
                        }
                        else if (renha.IsMatch(this_line)) // Number plus letter - Na, Nb
                        {
                            hdr_name = "renha";
                            regex_pattern = @"^\d{1,2}[a-z]{1} ";
                        }
                        else if (retab1.IsMatch(this_line)) // Hyphen followed by tab, -\t, -\t 
                        {
                            hdr_name = "retab1";
                            regex_pattern = @"^-\t";
                        }
                        else if (retab2.IsMatch(this_line)) // Number followed by tab, -\1, -\2 
                        {
                            hdr_name = "retab2";
                            regex_pattern = @"^\d{1,2}\t"; 
                        }
                        else if (retab3.IsMatch(this_line)) // Unknown character followed by tab
                        {
                            hdr_name = "retab3";
                            regex_pattern = @"^\uF0A7\t";
                        }
                        else if (retab4.IsMatch(this_line)) // Asterisk followed by tab
                        {
                            hdr_name = "retab4";
                            regex_pattern = @"^\*\t";
                        }
                        else if (retab5.IsMatch(this_line)) // Alpha-period followed by tab   a.\t, b.\t
                        {
                            hdr_name = "retab5";
                            regex_pattern = @"^[a-z]\.\t";
                        }
                        else if (rebrnum.IsMatch(this_line)) // Bracketed numbers (1), (2)
                        {
                            hdr_name = "rebrnum";
                            regex_pattern = @"^\(\d{1,2}\)";
                        }
                        else if (restar.IsMatch(this_line)) //  Asterisk only
                        {
                            hdr_name = "restar";
                            regex_pattern = @"^\*";
                        }
                        else if (resbrnum.IsMatch(this_line)) // Alpha-period followed by tab   a.\t, b.\t
                        {
                            hdr_name = "resbrnum";
                            regex_pattern = @"^\d{1,2}\)";
                        }
                        else if (rebrnumdot.IsMatch(this_line)) // Bracketed numbers (1), (2)
                        {
                            hdr_name = "rebrnumdot";
                            regex_pattern = @"^\d{1,2}\)\.";
                        }
                        else if (resqbrnum.IsMatch(this_line)) //  Asterisk only
                        {
                            hdr_name = "resqbrnum";
                            regex_pattern = @"^\[\d{1,2}\]";
                        }
                        else if (rebull.IsMatch(this_line)) // various bullets
                        {
                            hdr_name = "rebull";
                            regex_pattern = @"^[\u2022,\u2023,\u25E6,\u2043,\u2219]";
                        }
                        else if (rebull1.IsMatch(this_line)) // various bullets
                        {
                            hdr_name = "rebull1";
                            regex_pattern = @"^[\u2212,\u2666,\u00B7,\uF0B7]";
                        }
                        else if (reso.IsMatch(this_line)) // various bullets
                        {
                            hdr_name = "reso";
                            regex_pattern = @"^o ";
                        }
                        else if (reslat.IsMatch(this_line)) // various bullets
                        {
                            hdr_name = "reslat";
                            regex_pattern = @"^x{0,3}(ix|iv|v?i{0,3})\)";
                        }
                        else if (redash.IsMatch(this_line)) //  Asterisk only
                        {
                            hdr_name = "redash";
                            regex_pattern = @"^-";
                        }
                        else if (recrit1.IsMatch(this_line)) //  Number period only - can (rarely) give false positives
                        {
                            hdr_name = "recrit1";
                            regex_pattern = @"^\d{1,2}\.";
                        }
                        else
                        {
                            hdr_name = "none";
                            regex_pattern = @"";
                        }
                        
                       
                        if (hdr_name != "none")
                        { 
                            if (hdr_name != old_hdr_name)
                            {
                                level = GetLevel(hdr_name, levels);
                            }
                            levels[level].levelNum++;

                            leader = Regex.Match(this_line, regex_pattern).Value;
                            clipped_line = Regex.Replace(this_line, regex_pattern, string.Empty).Trim();
                            cr.Add(new Criterion(i + 1, leader, level, levels[level].levelNum,
                                single_crit, single_type, clipped_line));
                        }
                        else
                        {
                            if (i == lines.Length - 1)
                            {
                                cr.Add(new Criterion(i + 1, "Spp", level, levels[level].levelNum, post_crit,
                                    post_crit_type, this_line));
                            }
                            else
                            {
                                cr.Add(new Criterion(i + 1, "Hdr", level, levels[level].levelNum, grp_hdr, 
                                    grp_hdr_type, this_line));
                            }
                        }
                        
                        old_hdr_name = hdr_name;
                    }
                }
                
                // Repair some of the more obvious mis-interpretations
                // Work backwards and re-aggregate lines split with spurious \n

                if (cr.Count == 2 && cr[0].CritTypeId == grp_hdr
                                  && cr[1].CritTypeId == post_crit)
                {
                    // More likely that the second is a criterion after the heading
                    // rather than a 'supplement' statement.

                    cr[1].CritTypeId = single_crit;
                    cr[1].CritType = single_type;
                    cr[1].Leader = "(1)";
                }

                List<Criterion> cr2 = new();

                for (int i = cr.Count - 1; i >= 0; i--)
                {
                    bool transfer_crit = true;
                    if (cr[i].CritTypeId == grp_hdr
                        && i < cr.Count - 1 && i > 0
                        && !cr[i].CritText.EndsWith(':'))
                    {
                        // Does the following entry have an indentation
                        // level greater than the header? if not it is 
                        // probably not a 'true' header. Add it to the 
                        // preceding entry...
                        // 
                        // (N.B. Initial cr[0] is not checked, nor is 
                        // the last cr entry).

                        if (cr[i].IndentLevel >= cr[i + 1].IndentLevel)
                        {
                            // Almost certainly a spurious \n in the
                            // original string rather than a genuine header.

                            cr[i - 1].CritText += " " + cr[i].CritText;
                            cr[i - 1].CritText = cr[i - 1].CritText?.Replace("  ", " ");
                            transfer_crit = false;
                        }
                    }

                    if (cr[i].CritTypeId == post_crit && !string.IsNullOrEmpty(cr[i].CritText)
                            && !cr[i].CritText!.EndsWith(':')
                            && !cr[i].CritText!.StartsWith('*')
                            && !cr[i].CritText!.ToLower().StartsWith("note")
                            && !cr[i].CritText!.ToLower().StartsWith("for further details")
                            && !cr[i].CritText!.ToLower().StartsWith("for more information"))
                    {
                        // Almost always is a spurious supplement.
                        // Whether should be joined depends on whether there is an initial
                        // lower case or upper case letter... 

                        char init = cr[i].CritText![1];
                        if (char.ToLower(init) == init)
                        {
                            cr[i - 1].CritText += " " + cr[i].CritText;
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

                return cr2.OrderBy(c => c.SeqNum).ToList();
            }
        }
    }

    private int GetLevel(string hdr_name, List<Level> levels)
    {
        if (levels.Count == 1)
        {
            levels.Add(new Level(hdr_name, 0));
            return 1;
        }
        else
        {
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
    }
    

    public record Level
    {
        public Level(string _levelName, int _levelNum)
        {
            levelName = _levelName;
            levelNum = _levelNum;
        }
        public string levelName { get; set; }
        public int levelNum { get; set; }

    }
}
*/
