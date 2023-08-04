using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
namespace MDR_Harvester.Extensions;

[SuppressMessage("ReSharper", "UnusedMember.Local")]
public static class IECFunctions
{

    public static List<Criterion>? GetNumberedCriteria(string sid, string? input_string, string type)
    {
        // first trim, replace tags & non breaking spaces, regularise apostrophes and
        // line endings, decode html escapes etc.

        input_string = input_string.FullClean();

        if (string.IsNullOrEmpty(input_string))
        {
            return null;
        }

        // Some further cleaning 

        input_string = input_string.Replace("..", ".");
        input_string = input_string.Replace(",.", ".");
        input_string = input_string.Replace("\n\n", "\n");

        if (input_string.Length < 4)
        {
            return null; // rarely, the data has a '-' or other short place holder to indicate no criteria
        }

        // Task now is to create a list of lines, as separated by any carriage returns in the text.
        // The proportion of I/E source strings that are split using carriage returns appears to 
        // vary with the source, but in the majority of cases the data is split this way.
        // There are, however, many cases of spurious CRs splitting lines that are really one statement, 
        // as well as many examples where the criteria list is provided as a single line, without CRs.

        type_values tv = new(type, sid);
        List<string> raw_lines = input_string.Split('\n',
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();

        // do some initial tidying of the lines. Rarely, blank lines with _ characters found

        List<string> cleaned_lines = new();
        foreach (string s in raw_lines)
        {
            string this_line = s.TrimEnds()!;
            if (!string.IsNullOrEmpty(this_line) && !this_line.Contains(new string('_', 4)))
            {
                cleaned_lines.Add(this_line);
            }
        }

        // Join any odd lines with 1, 2, or 3 characters to the preceding or following line (depending on content)

        List<string> checked_lines = IECH.CoalesceVeryShortLines(cleaned_lines);

        // then transfer data to list of iec_line structures (each iec_line will be processed further below).

        List<iec_line> final_cr_lines = new(); // target list to develop
        if (checked_lines.Count == 1) // No CRs in source, simply add single line
        {
            final_cr_lines.Add(new iec_line(1, tv.no_sep, "none", "All", checked_lines[0], 0, 1,
                tv.getSequenceStart() + "0A"));
        }
        else
        {
            List<iec_line> raw_iec_list = new();
            int n = 0;
            foreach (string s in checked_lines)
            {
                raw_iec_list.Add(new iec_line(++n, tv.type, "cr", s));
            }

            // Initially try to find leader characters for each split line
            // then try to correct common errors in the list

            List<iec_line> processed_list = IdentifyLineLeaders(raw_iec_list, tv);
            final_cr_lines = TryToRepairSplitLines(processed_list, tv);
        }

        // then process each line to see if it includes sequences or separators itself
        // if multiple separators then the first occuring needs to be used
        // recursive process ends with a list of criterion objects

        List<iec_line> expanded_lines = new();
        foreach (iec_line l in final_cr_lines)
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
            ln.text = ln.text.TrimStart('-', '.', ',', ')').Trim();
            crits.Add(new Criterion(ln.seq_num, ln.type, tv.getTypeName(ln.type), ln.split_type,
                ln.leader, ln.indent_level, ln.indent_seq_num, ln.sequence_string, ln.text));
        }

        return crits.OrderBy(c => c.SeqNum).ToList();
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
            string ldrName = "none"; // initial defaults - signify no leader found
            string leader = "";

            foreach (KeyValuePair<string, string> r in IECH.Regexes)
            {
                string regex_pattern = r.Value;
                if (Regex.Match(this_line, regex_pattern).Success)
                {
                    ldrName = r.Key;
                    leader = Regex.Match(this_line, regex_pattern).Value;

                    // some regex patterns have to have additional checks. In other cases 
                    // simply break out of the loop with the matched pattern value.

                    if (ldrName.StartsWith("recrit"))
                    {
                        if (ldrName == "recrit")
                        {
                            // Turn into recrit1, without the space, to ensure that the header type
                            // remains the same even if there are variations in spacing in the source.

                            ldrName = "recrit1";
                            leader = leader.Trim();
                            break;
                        }

                        if (ldrName == "recrit1")
                        {
                            // regex_pattern = @"^\d{1,2}." 
                            // does it really match recrit 4?, regex = @"^\d{1,2}\.\d{1,2}[A-Z]"},

                            if (Regex.Match(this_line, @"^\d{1,2}\.\d{1,2}[A-Z]").Success)
                            {
                                ldrName = "recrit4";
                                leader = Regex.Match(this_line, @"^\d{1,2}\.\d{1,2}[A-Z]").Value;
                                leader = leader[..^1]; // lose the initial letter of the text from the leader
                            }

                            break;
                        }

                        if (ldrName == "recrit2")
                        {
                            // hdrName = "recrit2", regex_pattern = @"^\d{1,2} " 

                            if (int.TryParse(leader.Trim(), out int leader_num))
                            {
                                // should parse OK given regex match
                                // May need to be ignored if a number appears out of sequence
                                // Also the case if number is followed by a time period or unit
                                // - almost always part of the line above 

                                // ReSharper disable once ReplaceWithSingleAssignment.True
                                bool genuine = true;

                                if (i == 0 && leader_num != 1)
                                {
                                    genuine = false; // probably, but the converse is not true
                                }

                                // if associated with a time period unlikely to be genuine 

                                string rest_of_text = this_line[leader.Length..].Trim().ToLower();
                                if (rest_of_text.StartsWith("secs")
                                    || rest_of_text.StartsWith("second")
                                    || rest_of_text.StartsWith("mins")
                                    || rest_of_text.StartsWith("minute")
                                    || rest_of_text.StartsWith("hour")
                                    || rest_of_text.StartsWith("day")
                                    || rest_of_text.StartsWith("week")
                                    || rest_of_text.StartsWith("month")
                                    || rest_of_text.StartsWith("year")
                                    || rest_of_text.StartsWith("mg")
                                    || rest_of_text.StartsWith("ml")
                                    || rest_of_text.StartsWith("kg")
                                    || rest_of_text.StartsWith("g/")
                                    || rest_of_text.StartsWith("cm")
                                    || rest_of_text.StartsWith("patient")
                                   )
                                {
                                    genuine = false;
                                }

                                if (i > 0)
                                {
                                    string prev_ldr = crLines[i - 1].leader!.Trim();
                                    if (int.TryParse(prev_ldr, out _))
                                    {
                                        // preceding line has number space heading too
                                        // therefore more likely to be genuine
                                        genuine = true;
                                    }
                                    else
                                    {
                                        // unless the first number, unlikely to be a genuine number
                                        // though some number lists do start at 2! and others have a sequence 
                                        // interrupted by CRs at random intervals.

                                        // If there is a following number, or the previous line looks like a header,
                                        // it is more likely to be genuine.

                                        string prev_ldr2 = "", post_ldr = "";
                                        if (i > 1)
                                        {
                                            prev_ldr2 = crLines[i - 1].leader!.Trim();
                                        }

                                        if (i < crLines.Count - 1)
                                        {
                                            if (Regex.Match(crLines[i + 1].text, @"^\d{1,2} ").Success)
                                            {
                                                post_ldr = Regex.Match(crLines[i + 1].text, @"^\d{1,2} ").Value.Trim();
                                            }
                                        }

                                        if ((prev_ldr2 != "" && int.TryParse(prev_ldr2, out int _))
                                            || (post_ldr != "" && int.TryParse(post_ldr, out int _)))
                                        {
                                            // line is probably genuine
                                        }
                                        else
                                        {
                                            // seems to be isolated in the sequence
                                            genuine = false;
                                        }
                                    }
                                }

                                if (!genuine)
                                {
                                    // change the found pattern to none, line becomes a 'header' and likely
                                    // to be merge with the one before

                                    ldrName = "none";
                                    leader = "";
                                }
                            }

                            break;
                        }

                        if (ldrName == "recrit3")
                        {
                            // regex = @"^\d{1,2}[A-Z]"}

                            // Need to lose the first letter of the text -
                            // this has been used to identify the leader but is not really part of it.
                            // Leader needs to lose that character.

                            leader = leader[..^1];
                            break;
                        }
                    }

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

                    if (ldrName == "rethreeenum")
                    {
                        // Regex is @"^(1|2)\d{1,2}\.?\s?"
                        // Can be a spurious CR followed by a number, a.g. after an equals sign
                        // or before a unit. Should normally also be part of a sequence.

                        bool genuine = true; // as the starting point
                        string rest_of_text = this_line[leader.Length..].Trim().ToLower();
                        if (rest_of_text.StartsWith("mg") || rest_of_text.StartsWith("cm")
                                                          || rest_of_text.StartsWith("kg") ||
                                                          rest_of_text.StartsWith("secs")
                                                          || rest_of_text.StartsWith("patients") ||
                                                          rest_of_text.StartsWith("min")
                                                          || rest_of_text.StartsWith("days") ||
                                                          rest_of_text.StartsWith("days"))
                        {
                            genuine = false;
                        }

                        if (i > 0)
                        {
                            string prev_line = crLines[i - 1].text;
                            if (prev_line[^1] == '=' || prev_line[^1] == '>' || prev_line[^1] == '<')
                            {
                                genuine = false;
                            }
                            else
                            {
                                if (leader.Trim('.', ' ') != "101" && leader.Trim('.', ' ') != "201")
                                {
                                    bool prevln1same = false, prevln2same = false, nextlinesame = false;
                                    string prevldr1 = crLines[i - 1].leader!.Trim('.', ' ');
                                    if (Regex.Match(prevldr1, @"^(1|2)\d{1,2}").Success)
                                    {
                                        prevln1same = true;
                                    }

                                    if (i > 1)
                                    {
                                        string prevldr2 = crLines[i - 2].leader!.Trim('.', ' ');
                                        if (Regex.Match(prevldr2, @"^(1|2)\d{1,2}").Success)
                                        {
                                            prevln2same = true;
                                        }
                                    }

                                    if (i < crLines.Count - 1)
                                    {
                                        if (Regex.Match(crLines[i + 1].text, @"^(1|2)\d{1,2}\.?\s?").Success)
                                        {
                                            nextlinesame = true;
                                        }
                                    }

                                    if (prevln1same || prevln2same || nextlinesame)
                                    {
                                        // line is probably genuine
                                    }
                                    else
                                    {
                                        // seems to be isolated in the sequence
                                        genuine = false;
                                    }
                                }
                            }
                        }

                        if (!genuine)
                        {
                            ldrName = "none"; // not really a match for anything - make a 'header'
                            leader = "";
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
                            ldrName = "none"; // not really a match for anything
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

                        bool genuine = true; // as the starting point
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
                            // May be recrit 1, starting with a numeric value.
                            // But may be a numeric X.Y value followed by a time or weight uit
                            
                            string rest_of_text = this_line[leader.Length..].Trim().ToLower();
                            if (rest_of_text.StartsWith("secs")
                                || rest_of_text.StartsWith("second")
                                || rest_of_text.StartsWith("mins")
                                || rest_of_text.StartsWith("minute")
                                || rest_of_text.StartsWith("hour")
                                || rest_of_text.StartsWith("day")
                                || rest_of_text.StartsWith("week")
                                || rest_of_text.StartsWith("month")
                                || rest_of_text.StartsWith("year")
                                || rest_of_text.StartsWith("mg")
                                || rest_of_text.StartsWith("ml")
                                || rest_of_text.StartsWith("kg")
                                || rest_of_text.StartsWith("g/")
                                || rest_of_text.StartsWith("cm")
                                || rest_of_text.StartsWith("patient")
                               )
                            {
                                ldrName = "none"; // not really a match for anything
                                leader = "";
                            }
                            else
                            {
                                // change the found pattern to include only the first number and point
                                ldrName = "recrit1";
                                leader = Regex.Match(this_line, @"^\d{1,2}\.").Value;
                            }
                        }
                    }
                    
                    
                    if (ldrName == "restar")
                    {
                        // asterisk  -  more likely to be a final supplementary line, if the 
                        // line before does not have an asterisk leader
                        
                        if (i == crLines.Count - 1)
                        {
                            if (crLines.Count > 1 && crLines[i - 1].leader != "*")
                            {
                                crLines[i].leader = "Spp";
                                crLines[i].split_type = "cr";
                                crLines[i].indent_level = crLines[i - 1].indent_level;
                                crLines[i].indent_seq_num = ++levels[level].levelNum; // increment before applying
                                crLines[i].type = tv.post_crit;
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
                    level = IECH.GetLevel(ldrName, levels);

                    // if level = 1, (and not the first) have 'returned to a 'top level' leader
                    // the levels array therefore needs to be cleared so that identification of
                    // lower level leaders is kept 'local' to an individual top level element, and 
                    // built up as necessary for each top level element

                    if (level == 1 && levels.Count != 1)
                    {
                        levels.RemoveRange(2, levels.Count - 2);
                    }
                }
                
                // Change the properties of the iec_line object

                if (crLines[i].leader != "Spp")   // may have already been set above, e.g. with '*'
                {
                    crLines[i].leader = leader;
                    crLines[i].indent_level = level;
                    crLines[i].indent_seq_num = ++levels[level].levelNum; // increment before applying
                    crLines[i].text = this_line[leader.Length..].Trim();
                }
            }
            else
            {
                num_no_leader++; // keep a tally as ALL the lines may be without a leader

                if (i == crLines.Count - 1)
                {
                    // initially at least, make this final line without any 'leader' character
                    // a supplement (at the same indent level as the previous criteria).

                    crLines[i].leader = "Spp";
                    crLines[i].indent_level = level;
                    crLines[i].indent_seq_num = ++levels[level].levelNum; // increment before applying
                    crLines[i].type = tv.post_crit;
                }
                else
                {
                    // Otherwise, by default, add a line without any 'header' character as a sub-header
                    // in the list (at the same indent level as the previous criteria) 

                    crLines[i].leader = "Hdr";
                    crLines[i].indent_level = level;
                    crLines[i].indent_seq_num = ++levels[level].levelNum; // increment before applying
                    crLines[i].type = tv.grp_hdr;
                }
            }
            oldLdrName = ldrName;
        }

        // check all lines have a length of at least 1, i.e. are not empty, before proceeding further
        // Empty lines may occur - very rarely - if a line is 'all leader'
        // (though most of these should have been eliminated at the beginning)
        // or if the original split in CTG left a leader before the 'Exclusion Criteria' statement

        crLines.RemoveAll(ln => ln.text.Length == 0);

        // check the 'all without a leader' possibility - allowing a single exception

        if ((crLines.Count > 4 && num_no_leader >= crLines.Count - 1) ||
            (crLines.Count > 2 && num_no_leader == crLines.Count))
        {
            // none of the lines had a leader character. If they (or most of them) had proper 
            // termination, or consistent line starting, then it is possible that they are
            // simply differentiated by the CRs alone...

            bool assume_crs_only = IECH.CheckIfAllLinesEndConsistently(crLines, 1)
                                   || IECH.CheckIfAllLinesStartWithCaps(crLines, 1)
                                   || IECH.CheckIfAllLinesStartWithLowerCase(crLines, 0);

            // otherwise check for a consistent bullet type character

            string use_as_header = "";
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
            string thisText = crLines[i].text;

            // Remove (i.e. don't transfer) simple lines or headings with no information

            string lowtext = thisText.ToLower();
            if (lowtext is "inclusion:" or "inclusion criteria" or "inclusion criteria:" or "included:"
                or "exclusion:" or "exclusion criteria" or "exclusion criteria:" or "excluded:")
            {
                transfer_crit = false;
            }

            if (crLines[i].type == tv.grp_hdr)
            {
                if (lowtext.Contains("key criteria") || lowtext.Contains("key inclusion criteria")
                                                     || lowtext.Contains("inclusion criteria include") ||
                                                     lowtext.Contains("key exclusion criteria")
                                                     || lowtext.Contains("exclusion criteria include"))
                {
                    transfer_crit = false;
                }
            }

            if (!string.IsNullOrEmpty(thisText))
            {
                // Try and identify spurious 'headers', i.e. lines without leaders, caused by spurious CRs.

                try
                {
                    if (crLines[i].type == tv.grp_hdr && i < crLines.Count - 1 && i > 0)
                    {
                        // If line starts with 'Note' very likely to be a 'header' giving supp. information.
                        // Also do not try to merge upward if preceding line ends with ':'
                        // because headers assumed to normally end with ':', but other checks made in addition
                        // (N.B. Initial and last entries are not checked).

                        if (!thisText.ToLower().StartsWith("note") && !crLines[i - 1].text.EndsWith(':'))
                        {
                            char initChar = thisText[0];
                            if (!thisText.EndsWith(':'))
                            {
                                // Does the entry following the header have an indentation level greater than the header?,
                                // as would be expected with a 'true' header.
                                // If not, add it to the preceding entry as it is 
                                // likely to be a spurious \n in the original string rather than a genuine header.

                                // Also if starts with a lower case letter or digit, and
                                // previous line does not add in a full stop.

                                if (crLines[i].indent_level >= crLines[i + 1].indent_level || 
                                    (!crLines[i - 1].text.EndsWith('.') 
                                        && (char.ToLower(initChar) == initChar || char.IsDigit(initChar)))
                                   )
                                {
                                    // Almost certainly a spurious \n in the
                                    // original string rather than a genuine header.

                                    crLines[i - 1].text += " " + thisText;
                                    crLines[i - 1].text = crLines[i - 1].text.Replace("  ", " ");
                                    transfer_crit = false;
                                    
                                    // Difficulty is that some spurious \n are mid-word...and some
                                    // are between words - no easy way to distinguish
                                }
                            }

                            if (thisText.EndsWith(':')
                                && (initChar.ToString() == initChar.ToString().ToLower() || char.IsDigit(initChar)))
                            {
                                // Header line that has a colon but also starts with a lower case letter or digit
                                // Likely to be a 'split header'. merge it 'upwards' to the line before

                                string prev_line = crLines[i - 1].text;
                                char prev_last_char = prev_line[^1];
                                if (prev_last_char is not ('.' or ';' or ':'))
                                {
                                    crLines[i - 1].text = (prev_line + " " + thisText).Replace("  ", " ");
                                    crLines[i - 1].type = tv.grp_hdr;
                                    transfer_crit = false;
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }

                // check to see if a last line 'supplement' is better characterised as a normal criterion

                if (crLines[i].type == tv.post_crit && i > 0
                                                    && !thisText.EndsWith(':') && !thisText.StartsWith('*')
                                                    && !thisText.ToLower().StartsWith("note") &&
                                                    !thisText.ToLower().StartsWith("other ")
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
                        crLines[i].indent_level = crLines[i - 1].indent_level;
                        crLines[i].indent_seq_num = crLines[i - 1].indent_seq_num + 1;
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
        
        if (revised_lines.Count == 2 && revised_lines[0].type == tv.grp_hdr)
        {
            string top_text = revised_lines[0].text;
            string bottom_text = revised_lines[1].text;
            if (top_text.EndsWith(":") && top_text.ToLower().Contains("criteria"))
            {
                // Probably a genuine header (unusual). Make the second line a criterion
                
                revised_lines[1].type = tv.type;
                revised_lines[1].leader = "-1-";
                revised_lines[1].indent_level = 1;
                revised_lines[1].indent_seq_num = 1;
            }
            else
            {
                if (IECH.CheckIfAllLinesEndConsistently(crLines, 0)
                    || IECH.CheckIfAllLinesStartWithCaps(crLines, 0))
                {
                    // More likely that these are a pair of criteria statements (or multiple criteria statements)
                   
                    revised_lines[0].seq_num = 1;
                    revised_lines[1].seq_num = 2;
                    revised_lines[0].split_type = "cr pair";
                    revised_lines[1].split_type = "cr pair";
                   
                    revised_lines[0].type = tv.type;
                    revised_lines[0].leader = "-1-";
                    revised_lines[0].indent_level = 1;
                    revised_lines[0].indent_seq_num = 1;
                   
                    revised_lines[1].type = tv.type;
                    revised_lines[1].leader = "-2-";
                    revised_lines[1].indent_level = 1;
                    revised_lines[1].indent_seq_num = 2;
                   
                }
                else if ((top_text.EndsWith(' ') || top_text.EndsWith(','))
                         && bottom_text[0].ToString() != bottom_text[0].ToString().ToUpper())
                    
                {
                   // More likely they are a single statement split for some reason

                   revised_lines[0].text = revised_lines[0].text + " " + revised_lines[1].text;
                   revised_lines[0].text = revised_lines[0].text.Replace("  ", " ");
                   
                   revised_lines[0].seq_num = 1;
                   revised_lines[0].split_type = "none";
                   revised_lines[0].type = tv.no_sep;
                   revised_lines[0].leader = "All";
                   revised_lines[0].indent_level = 0;
                   revised_lines[0].indent_seq_num = 1;
                   revised_lines[0].sequence_string = tv.getSequenceStart() + "0A";

                   revised_lines.Remove(revised_lines[1]);
                }
                else
                {
                    // leave as a hdr / spp pair...
                }
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
                        ? sequence_start + "00" + level_pos[0]
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
        List<Splitter> splitters = FindSplittersInString(iecLine.text, iecLine.leader);

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
                ? IECH.SplitUsingSequence(iecLine, sp.f_start!, sp.f_end!, sp.check_char!, loop_depth,
                    tv) // split on sequence
                : IECH.SplitOnSeperator(iecLine, sp.string_splitter!, loop_depth, tv); // split on a separator

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


    private static List<Splitter> FindSplittersInString(string input_string, string? leader)
    {
        List<Splitter> splitters = new();

        if (leader is not null && leader.Length == 1)
        {
            // for bullet type leaders, put the leader back in front of the line, as it may be 
            // used inside the line as a separator as well. In which case the starting position should 
            // be identified correctly as 0. otherwise ordering of splitter application may be wrong.
            
            input_string = leader + input_string;
        }

        // Try typical separators

        int semicolon_count = (input_string.Length - input_string.Replace("; ", "").Length) / 2;
        if (semicolon_count > 2)
        {
            // additional checks here - ensure 3 rather than 2 and reasonable distance apart
            int pos1 = input_string.IndexOf(';', 0);
            int pos2 = input_string.IndexOf(';', pos1 + 1);
            int pos3 = input_string.IndexOf(';', pos1 + 2);
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

                int pos1 = IECH.FetchNextButCheckForFollowingDigit(input_string, 0, "1.");
                if (pos1 > -1)
                {
                    int pos2 = IECH.FetchNextButCheckForFollowingDigit(input_string, pos1 + 3, "2.");
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

                int pos1 = IECH.FetchNextButCheckForPrecedingDigit(input_string, 0, "1)");
                if (pos1 > -1)
                {
                    int pos2 = IECH.FetchNextButCheckForPrecedingDigit(input_string, pos1 + 3, "2)");
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

                int pos1 = IECH.FetchNextButCheckForFollowingDigit(input_string, 0, "1/");
                if (pos1 > -1)
                {
                    int pos2 = IECH.FetchNextButCheckForFollowingDigit(input_string, pos1 + 3, "2/");
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
                int pos1 = IECH.FetchNextButCheckForFollowingDigit(input_string, 0, "1-");
                if (pos1 > -1)
                {
                    int pos2 = IECH.FetchNextButCheckForFollowingDigit(input_string, pos1 + 3, "2-");
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

                int pos1 = IECH.FetchNextButCheckSeparatedFromPreceding(input_string, 0, "1 ");
                if (pos1 > -1)
                {
                    int pos2 = IECH.FetchNextButCheckSeparatedFromPreceding(input_string, pos1 + 3, "2 ");
                    if (pos2 > -1)
                    {
                        int pos3 = IECH.FetchNextButCheckSeparatedFromPreceding(input_string, pos2 + 3, "3 ");
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
                splitters.Add(new Splitter(1, pos1, GetStringToFind, GetNextStringToFind, "a."));
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

            int pos1 = IECH.FetchNextButCheckNotHyphen(input_string, 0, "-");
            if (pos1 > -1)
            {
                int pos2 = IECH.FetchNextButCheckNotHyphen(input_string, pos1 + 2, "-");
                if (pos2 > -1)
                {
                    int pos3 = IECH.FetchNextButCheckNotHyphen(input_string, pos2 + 2, "-");
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

            int pos1 = IECH.FetchNextButCheckNotHyphen(input_string, 0, "¿");
            if (pos1 > -1)
            {
                int pos2 = IECH.FetchNextButCheckNotHyphen(input_string, pos1 + 2, "¿");
                if (pos2 > -1)
                {
                    int pos3 = IECH.FetchNextButCheckNotHyphen(input_string, pos2 + 2, "¿");
                    if (pos3 - pos2 > 4 && pos2 - pos1 > 4)
                    {
                        if (!input_string.Trim().StartsWith("¿"))
                        {
                            input_string = "¿" + input_string; // ensure all lines treated the same
                        }

                        string GetStringToFind(int i) => "¿";
                        string GetNextStringToFind(int i) => "¿";
                        splitters.Add(new Splitter(1, pos1, GetStringToFind, GetNextStringToFind, "¿"));
                    }
                }
            }
        }

        // Test 20

        int singlestar_count = input_string.Count(c => c == '*');
        int doublestar_count = (input_string.Length - input_string.Replace("**", "").Length);
        if (singlestar_count - doublestar_count > 2)
        {
            // * also used to indicate multiplication within formulae, as well as '**' doubles
            // Therefore check for numbers both sides of the *

            int pos1 = IECH.FetchNextButCheckNotMultip(input_string, 0, "*");
            if (pos1 > -1)
            {
                int pos2 = IECH.FetchNextButCheckNotMultip(input_string, pos1 + 2, "*");
                if (pos2 > -1 && pos2 - pos1 > 4)
                {
                    if (!input_string.Trim().StartsWith("*"))
                    {
                        input_string = "*" + input_string; // ensure all lines treated the same
                    }

                    string GetStringToFind(int i) => "*";
                    string GetNextStringToFind(int i) => "*";
                    splitters.Add(new Splitter(1, pos1, GetStringToFind, GetNextStringToFind, "¿"));
                }
            }
        }

        return splitters;
    }
}

