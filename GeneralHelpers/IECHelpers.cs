namespace MDR_Harvester.Extensions;

public static class IECH
{
    public static readonly Dictionary<string, string> Regexes;

    static IECH()
    {
        // use constructor to set up dictionary of regex expressions

        Regexes = new Dictionary<string, string>()
        {
            { "retab5", @"^[a-z]\.\t" }, // alpha-period followed by tab   a.\t, b.\t
            { "reha", @"^[a-z]{1}\." }, // alpha period. a., b.
            { "rehacap", @"^[A-Z]{1}\." }, // alpha caps period. A., B.
            { "rehadb", @"^\([a-z]{1}\)" }, // alpha in brackets. (a), (b)
            { "rehab", @"^[a-z]{1}\)" }, // alpha with right bracket. a), b)
            { "renha", @"^\d{1,2}[a-z]{1}\s" }, // number plus letter  Na, Nb
            
            { "retab1", @"^-\t" }, // hyphen followed by tab, -\t, -\t 
            { "retab2", @"^\d{1,2}\t" }, // number followed by tab, 1\t, 2\t
            { "retab3", @"^\uF0A7\t" }, // unknown character followed by tab
            { "retab4", @"^\*\t" }, // asterisk followed by tab    *\t, *\t
            
            { "rebrnum", @"^\(\d{1,2}\)" }, // bracketed numbers (1), (2)
            { "rebrnumdot", @"^\d{1,2}\)\." }, // number followed by right bracket and dot 1)., 2).
            { "resbrnum", @"^\d{1,2}\)" }, // number followed by right bracket 1), 2)
            { "rebrnumcol", @"^\d{1,2}\:" }, // number followed by colon 1:, 2:
            { "renumdotbr", @"^\d{1,2}\.\)" }, // number followed by dot and right bracket  1.), 2.)
            { "resqbrnum", @"^\[\d{1,2}\]" }, // numbers in square brackets   [1], [2]
            { "resqrtnum", @"^\d{1,2}\]" }, // numbers with right square bracket   1], 2]
            { "resnumdashb", @"^\d{1,2}\-\)" }, //  numbers and following dash, right bracket  1-), 2-)
            { "resnumdash", @"^\d{1,2}\-" }, //  numbers and following dash  1-, 2-
            { "resnumslash", @"^\d{1,2}\/" }, //  numbers and following slash  1/, 2/
            
            { "rebull", @"^[\u2022,\u2023,\u25E6,\u2043,\u2219]" }, // various bullets 1
            { "rebull1", @"^[\u2212,\u2666,\u00B7,\uF0B7]" }, // various bullets 2
            { "reso", @"^o " }, // open 'o' bullet followed by space, o , o
            { "resotab", @"^o\t" }, // open 'o' bullet followed by tab  o\t, o\t
            
            { "reslatbr", @"^\(x{0,3}(|ix|iv|v?i{0,3})\)" }, // roman numerals double bracket   (i), (ii)
            { "reslat", @"^x{0,3}(|ix|iv|v?i{0,3})\)" }, // roman numerals right brackets    i), ii)
            { "reslatdot", @"^x{0,3}(|ix|iv|v?i{0,3})\." }, // roman numerals dots   i., ii.
            
            { "resssh", @"^\d{1,2}\.\d{1,2}\.\d{1,2}\.\d{1,2}" }, // numeric Sub-sub-sub-heading. N.n.n.n
            { "ressh", @"^\d{1,2}\.\d{1,2}\.\d{1,2}" }, // numeric Sub-sub-heading. N.n.n            
            { "resh", @"^\d{1,2}\.\d{1,2}\." }, // numeric Sub-heading. N.n.     
            { "resh1", @"^\d{1,2}\.\d{1,2}\s" }, // numeric Sub-heading space (without final period) N.n
            { "recrit4", @"^\d{1,2}\.\d{1,2}[A-Z]" }, // number-dot-number cap letter  - might give false positives
            { "recrit", @"^\d{1,2}\.\s" }, // number period and space  1. , 2.         
            { "recrit1", @"^\d{1,2}\." }, // number period only - can give false positives
            { "recrit2", @"^\d{1,2}\s" }, // number space only - can give false positives         
            { "recrit3", @"^\d{1,2}[A-Z]" }, // number-cap letter  - might give false positives     
            
            { "redash", @"^-" }, // dash only   -, -
            { "redoubstar", @"^\*\*" }, // two asterisks   **, **
            { "restar", @"^\*" }, // asterisk only   *, *
            { "resemi", @"^;" }, // semi-colon only   ;, ; 
            { "request", @"^\?" }, // question mark only   ?, ?
            { "reinvquest", @"^¿" }, // inverted question mark only   ¿, ¿
            
            { "reespacenum", @"^(E|e)\s?\d{1,2}" }, // exclusion as E or e numbers, optional space E 01, E 02
            { "reispacenum", @"^(I|i)\s?\d{1,2}" }, // inclusion as I or i numbers, optional space i1, i2
            { "rethreeenum", @"^(1|2)\d{1,2}\.?\s?" }, // 1 or 2 followed by 2 numbers, optional dot or space
        };
    }

    
    public static List<string> CoalesceVeryShortLines(List<string> preLines)
    {
        // Function deals with a rare but possible problem with very short lines.
        // May be, or include, 'or' or 'and', or be the result of a spurious CR (e.g. immediately
        // after a line number). In general therefore add such very small lines to the preceding
        // line (unless it is the first line or very short and starts with a number). 
        // N.B. Lines are already trimmed, in calling procedure.

        List<string> checked_lines = new();
        for (int j = 0; j < preLines.Count; j++)
        {
            if (preLines[j].Length >= 4)
            {
                checked_lines.Add(preLines[j]); // skip process if line length is 4 or more
            }
            else
            {
                if (j == 0)
                {
                    preLines[1] = preLines[0] + " " + preLines[1]; // if first line, add to beginning of following line
                }
                else
                {
                    bool includes_digit = preLines[j].Any(char.IsDigit);
                    if (includes_digit)
                    {
                        if (j < preLines.Count - 1) // if not the final line
                        {
                            preLines[j + 1] = preLines[j] + " " + preLines[j + 1]; // add to following line
                        }
                        else
                        {
                            checked_lines[^1] += " " + preLines[j]; // add to preceding line, already transferred 
                        }
                    }
                    else
                    {
                        checked_lines[^1] += " " + preLines[j]; // add to preceding line, already transferred
                    }
                }
            }
        }
        return checked_lines;
    }
    
    
    public static bool CheckIfAllLinesEndConsistently(List<iec_line> lines, int allowance)
    {
        int valid_end_chars = 0;
        foreach (var t in lines)
        {
            char end_char = t.text[^1];
            if (end_char is '.' or ';' or ',')
            {
                valid_end_chars++;
            }

        }
        return valid_end_chars >= lines.Count - allowance;
    }
    
    public static bool CheckIfAllLinesStartWithCaps(List<iec_line> lines, int allowance)
    {
        int valid_start_chars = 0;
        foreach (var t in lines)
        {
            string start_char = t.text[0].ToString();
            if (start_char == start_char.ToUpper())
            {
                valid_start_chars++;
            }
        }
        return valid_start_chars >= lines.Count - allowance;
    }
    
    public static bool CheckIfAllLinesStartWithLowerCase(List<iec_line> lines, int allowance)
    {
        int valid_start_chars = 0;
        foreach (var t in lines)
        {
            string start_char = t.text[0].ToString();
            if (start_char == start_char.ToLower())
            {
                valid_start_chars++;
            }
        }
        return valid_start_chars >= lines.Count - allowance;
    }
    
    
    public static int GetLevel(string hdr_name, List<Level> levels)
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
    
    
    
    public static List<iec_line> SplitOnSeperator(iec_line line, string splitter, int loop_depth, type_values tv)
    {
        string input_string = line.text;
        string seq_base = line.type == tv.no_sep ? tv.getSequenceStart() + "01." : line.sequence_string + ".";

        string[] split_lines = input_string.Split(splitter,
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

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


    public static List<iec_line> SplitUsingSequence(iec_line input_line, Func<int, string> GetStringToFind,
        Func<int, string> GetNextStringToFind, string checkChar, int loop_depth, type_values tv)
    {
        string input_string = input_line.text;
        List<iec_line> split_strings = new();
        string seq_base = input_line.type == tv.no_sep
            ? tv.getSequenceStart() + "01."
            : input_line.sequence_string + ".";
        int level_seq_num = 0;
        string firstLeader = GetStringToFind(1);
        int firstLeaderPos = input_string.IndexOf(firstLeader, 0, StringComparison.Ordinal);
        if (firstLeaderPos > 2) // add any prefix as the initial line, if more than 2 letters
        {
            // watch the a) / (a) possibility
            if (firstLeader == "a)" && input_string[firstLeaderPos - 1] == '(')
            {
                firstLeaderPos--; // go back to include the leading bracket if it was there
            }

            ++level_seq_num;
            split_strings.Add(new iec_line(input_line.seq_num, tv.grp_hdr, "seq", "Hdr",
                input_string[..firstLeaderPos], loop_depth + 1, level_seq_num,
                seq_base + level_seq_num.ToString("0#"))); // no leader - therefore a hdr

        }

        int i = 1;
        int line_start = 0;
        int line_end = 0;

        while (line_end > -1)
        {
            string string_to_find = GetStringToFind(i);
            string next_string_to_find = GetNextStringToFind(i);
            line_start = input_string.IndexOf(string_to_find, line_start, StringComparison.Ordinal);
            if (string_to_find == "a)" && line_start > 0 && input_string[line_start - 1] == '(')
            {
                line_start--; // include the leading bracket if it was there for "a)"
                string_to_find = "(a)";
            }

            string line;
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
                    split_strings[^1].text += line.Trim(); // add to last string
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
                else if (checkChar is "a.")
                {
                    // need to check following character is not part of e.g. or i.e.
                    line_end = FetchNextButCheckNotAbbrev(input_string, line_start + 3,
                        next_string_to_find);
                }
                else if (checkChar is "-" or "¿")
                {
                    // need to check preceding characters as not representing a number or letter.
                    line_end = FetchNextButCheckNotHyphen(input_string, line_start + 3, next_string_to_find);
                }
                else if (checkChar is "*")
                {
                    // need to check preceding characters as not representing a number or letter.
                    line_end = FetchNextButCheckNotMultip(input_string, line_start + 3, next_string_to_find);
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
                    string first_part = line[..(colon_pos + 1)];
                    string second_part = line[(colon_pos + 1)..];

                    ++level_seq_num;
                    split_strings.Add(new iec_line(input_line.seq_num, tv.grp_hdr, "seq", "Hdr",
                        first_part, loop_depth + 1, level_seq_num, seq_base + level_seq_num.ToString("0#")));

                    ++level_seq_num;
                    split_strings.Add(new iec_line(input_line.seq_num, tv.type, "seq", string_to_find,
                        second_part, loop_depth + 1, level_seq_num, seq_base + level_seq_num.ToString("0#")));
                }
                else
                {
                    ++level_seq_num;
                    split_strings.Add(new iec_line(input_line.seq_num, tv.type, "seq", string_to_find,
                        line, loop_depth + 1, level_seq_num, seq_base + level_seq_num.ToString("0#")));
                }

                line_start = line_end;
                i++;
            }
        }

        return split_strings;
    }


    public static int FetchNextButCheckForFollowingDigit(string input_string, int string_pos, string string_to_find)
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

    public static int FetchNextButCheckForPrecedingDigit(string input_string, int string_pos, string string_to_find)
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

    public static int FetchNextButCheckSeparatedFromPreceding(string input_string, int string_pos,
        string string_to_find)
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

    public static int FetchNextButCheckNotHyphen(string input_string, int string_pos, string string_to_find)
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


    public static int FetchNextButCheckNotAbbrev(string input_string, int string_pos, string string_to_find)
    {
        // only applies to these situations, when next string to find would 'e' and 'i' 
        // and e.g. or i.e. could be identified wrongly

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

            if (string_to_find == "e")
            {
                char test_char1 = input_string[string_pos + 1];
                char test_char2 = input_string[string_pos + 2];

                if (test_char1 == '.' && test_char2 == 'g')
                {
                    result_obtained = false;
                }
            }

            if (string_to_find == "i")
            {
                char test_char1 = input_string[string_pos + 1];
                char test_char2 = input_string[string_pos + 2];

                if (test_char1 == '.' && test_char2 == 'e')
                {
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

    public static int FetchNextButCheckNotMultip(string input_string, int string_pos, string string_to_find)
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
                int char_pos1 = string_pos, char_pos2 = string_pos;
                while (char_pos1 > 0 && input_string[char_pos1] != ' ')
                {
                    char_pos1--;
                }

                while (char_pos2 < input_string.Length - 1 && input_string[char_pos2] != ' ')
                {
                    char_pos2++;
                }

                if (char.IsDigit(input_string[char_pos1]) && char.IsDigit(input_string[char_pos2]))
                {
                    result_obtained = false; // looks like a multiplication!
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
    
    
    

}


public class type_values
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
    
    public type_values(string type_stem, string sid)
    {
        sd_sid = sid;
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
    
public class iec_line
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

public class Splitter
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
    
public record seqLevel
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
public enum roman
#pragma warning restore CS8981
{
    i = 1, ii, iii, iv, v, vi, vii, viii, ix, x,
    xi, xii, xiii, xiv, xv, xvi, xvii, xviii, xix, xx, 
    xxi, xxii, xxiii, xxiv, xxv
}
    
public enum romanCaps
{
    I = 1, II, III, IV, V, VI, VII, VIII, IX, X,
    XI, XII, XIII, XIV, XV, XVI, XVII, XVIII, XIX, XX,
    XXI, XXII, XXIII, XXIV, XXV
}

