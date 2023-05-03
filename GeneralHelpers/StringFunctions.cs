
namespace MDR_Harvester.Extensions;

public static class StringHelpers
{
    public static string? TrimPlus(this string? input_string)
    {
        // removes beginning or trailing carriage returns, tabs and spaces.
        
        return string.IsNullOrEmpty(input_string) 
            ? null 
            : input_string.Trim('\r', '\n', '\t', ' ');
    }


    public static string? ReplaceApos(this string? apos_name)
    {
        if (string.IsNullOrEmpty(apos_name))
        {
            return null;
        }

        string aName = apos_name.Replace("&#44;", ","); // unusual but it occurs

        while (aName.Contains('\''))
        {
            int apos_pos = aName.IndexOf("'", StringComparison.Ordinal);
            int alen = aName.Length;
            if (apos_pos == 0)
            {
                aName = "‘" + aName[1..];
            }
            else if (apos_pos == alen - 1)
            {
                aName = aName[..^1] + "’";
            }
            else
            {
                if (aName[apos_pos - 1] == ' ' || aName[apos_pos - 1] == '(')
                {
                    aName = aName[..apos_pos] + "‘" + aName[(apos_pos + 1)..];
                }
                else
                {
                    aName = aName[..apos_pos] + "’" + aName[(apos_pos + 1)..];
                }
            }
        }

        return aName;
    }
    

    public static string? ReplaceTags(this string? input_string)
    {
        if (string.IsNullOrEmpty(input_string))
        {
            return null;
        }

        // needs to have opening and closing tags for further processing
        // except in a few cases commas may be in a string as "&#44;"
        // and hyphens as "&#44;" (largely thai registry)

        string output_string = input_string.Replace("&#44;", ",");
        output_string = output_string.Replace("&#45;", "-");
        
        if (!(output_string.Contains('<') && output_string.Contains('>')))
        {
            return output_string;
        }

        // The commonest case.

        output_string = output_string
            .Replace("<br>", "\n")
            .Replace("<br/>", "\n")
            .Replace("<br />", "\n")
            .Replace("<br/ >", "\n")
            .Replace("< br / >", "\n");

        // Check need to continue.

        if (!(output_string.Contains('<') && output_string.Contains('>')))
        {
            return output_string;
        }

        output_string = RemoveTab(output_string, "p", "\n");
        output_string = RemoveTab(output_string, "li", "\n\u2022 ");
        output_string = RemoveTab(output_string, "ul", "");
        output_string = RemoveTab(output_string, "ol", "");

        if (!(output_string.Contains('<') && output_string.Contains('>')))          // check need to continue
        {
            return output_string;
        }
        
        output_string = RemoveTab(output_string, "div", "");
        output_string = RemoveTab(output_string, "span", "");        
        output_string = RemoveTab(output_string, "a", "");     
        
        // Assume these will be simple tags, without classes.
        
        output_string = output_string.Replace("<b>", "").Replace("</b>", "").Replace("<i>", "").Replace("</i>", "");
        output_string = output_string.Replace("<em>", "").Replace("</em>", "").Replace("<u>", "").Replace("</u>", "");
        output_string = output_string.Replace("<strong>", "").Replace("</strong>", "");

        // try and replace any sub and super scripts.

        while (output_string.Contains("<sub>"))
        {
            int start_pos = output_string.IndexOf("<sub>", StringComparison.Ordinal);
            int end_string = output_string.IndexOf("</sub>", start_pos, StringComparison.Ordinal);
            if (end_string != -1) // would indicate a non matched sub entry
            {
                int end_pos = end_string + 5;
                string string_to_change = output_string[(start_pos + 5)..end_string];
                string new_string = "";
                for (int i = 0; i < string_to_change.Length; i++)
                {
                    new_string += string_to_change[i].ChangeToSubUnicode();
                }

                if (end_pos > output_string.Length - 1)
                {
                    output_string = output_string[..start_pos] + new_string;
                }
                else
                {
                    output_string = output_string[..start_pos] + new_string + output_string[(end_pos + 1)..];
                }
            }
            else
            {
                // drop any that are left (to get out of the loop)
                output_string = output_string.Replace("</sub>", "");
                output_string = output_string.Replace("<sub>", "");
            }
        }

        while (output_string.Contains("<sup>"))
        {
            int start_pos = output_string.IndexOf("<sup>", StringComparison.Ordinal);
            int end_string = output_string.IndexOf("</sup>", start_pos, StringComparison.Ordinal);
            if (end_string != -1) // would indicate a non matched sup entry
            {
                int end_pos = end_string + 5;
                string string_to_change = output_string[(start_pos + 5)..end_string];
                string new_string = "";
                for (int i = 0; i < string_to_change.Length; i++)
                {
                    new_string += string_to_change[i].ChangeToSupUnicode();
                }

                if (end_pos > output_string.Length - 1)
                {
                    output_string = output_string[..start_pos] + new_string;
                }
                else
                {
                    output_string = output_string[..start_pos] + new_string + output_string[(end_pos + 1)..];
                }
            }
            else
            {
                // drop any that are left (to ensure getting out of the loop)
                output_string = output_string.Replace("</sup>", "");
                output_string = output_string.Replace("<sup>", "");
            }
        }

        return output_string;
    }

    private static string RemoveTab(string input_string, string tag, string substitute)
    {
        string start_tag = "<" + tag;
        string end_tag = "</" + tag + ">";
        int search_start = 0, start_of_tag = 0;
        while (input_string.Contains(start_tag) && start_of_tag != -1)
        {
            // Although the while statement guarantees the presence of the tag
            // a search for it may fail if the test below is failed, i.e. if it is not 
            // a 'true' tag - ongoing search could therefore 'run off the end'
            
            start_of_tag = input_string.IndexOf(start_tag, search_start, StringComparison.Ordinal);
            if (start_of_tag != -1)        
            {
                // check no immediately following non-space character
                // and if that is the case look for the ending of the start tag
                // remove the text, between and including the tags, replacing it if necessary.
                // Then remove all corresponding end tags.

                if (input_string[start_of_tag + start_tag.Length] == '>'
                                        || input_string[start_of_tag + start_tag.Length] == ' ')
                {
                    int e_pos = input_string.IndexOf(">", start_of_tag, StringComparison.Ordinal);
                    if (e_pos != -1)
                    {
                        input_string = input_string[..start_of_tag] + substitute + input_string[(e_pos + 1)..];
                    }
                }
                search_start = start_of_tag + start_tag.Length;
            }
        }
        input_string = input_string.Replace(end_tag, "");
        return input_string;
    }

    
    public static string? RegulariseStringEndings(this string? input_string)
    {
        if (string.IsNullOrEmpty(input_string))
        {
            return null;
        }

        string output_string = input_string.Replace("\r\n", "|@@|");
        output_string = output_string.Replace("\r", "\n");
        return output_string.Replace("|@@|", "\r\n");
 }


    public static string? CompressSpaces(this string? input_string)
    {
        if (string.IsNullOrEmpty(input_string))
        {
            return null;
        }

        string output_string = input_string.Replace("\n ", "\n");            
        
        while (output_string.Contains("  "))
        {
            output_string = output_string.Replace("  ", " ");
        }
        while (output_string.Contains("\n\n"))
        {
            output_string = output_string.Replace("\n\n", "\n");
        }

        return output_string.Trim();

    }


    public static string? ReplaceNonBreakingSpaces(this string? input_string)
    {
        // Simple extension that returns null for null values and
        // text based 'NULL equivalents', and otherwise trims the string
        
        if (string.IsNullOrEmpty(input_string))
        {
            return null;
        }

        string output_string = input_string.Replace('\u00A0', ' ');
        output_string = output_string.Replace('\u2000', ' ').Replace('\u2001', ' ');
        output_string = output_string.Replace('\u2002', ' ').Replace('\u2003', ' ');
        output_string = output_string.Replace('\u2007', ' ').Replace('\u2008', ' ');
        output_string = output_string.Replace('\u2009', ' ').Replace('\u200A', ' ');

        return output_string;
    }


    public static string? StringClean(this string? input_string)
    {
        if (string.IsNullOrEmpty(input_string))
        {
            return null;
        }

        string? output_string = input_string.TrimPlus();
        output_string = output_string.ReplaceTags();
        output_string = output_string.ReplaceApos();
        output_string = output_string.ReplaceNonBreakingSpaces();
        return output_string.RegulariseStringEndings();
    }


    private static char ChangeToSupUnicode(this char a)
    {
        return a switch
        {
            '0' => '\u2070',
            '1' => '\u0B09',
            '2' => '\u0B02',
            '3' => '\u0B03',
            '4' => '\u2074',
            '5' => '\u2075',
            '6' => '\u2076',
            '7' => '\u2077',
            '8' => '\u2078',
            '9' => '\u2079',
            'i' => '\u2071',
            '+' => '\u207A',
            '-' => '\u207B',
            '=' => '\u207C',
            '(' => '\u207D',
            ')' => '\u207E',
            'n' => '\u207F',
            _ => a
        };
    }

    private static char ChangeToSubUnicode(this char a)
    {
        return a switch
        {
            '0' => '\u2080',
            '1' => '\u2081',
            '2' => '\u2082',
            '3' => '\u2083',
            '4' => '\u2084',
            '5' => '\u2085',
            '6' => '\u2086',
            '7' => '\u2087',
            '8' => '\u2088',
            '9' => '\u2089',
            '+' => '\u208A',
            '-' => '\u208B',
            '=' => '\u208C',
            '(' => '\u208D',
            ')' => '\u208E',
            'a' => '\u2090',
            'e' => '\u2091',
            'o' => '\u2092',
            'x' => '\u2093',
            'h' => '\u2095',
            'k' => '\u2096',
            'l' => '\u2097',
            'm' => '\u2098',
            'n' => '\u2099',
            'p' => '\u209A',
            's' => '\u209B',
            't' => '\u209C',
            _ => a
        };

    }


    public static string? TidyORCIDId(this string? input_identifier)
    {
        if (string.IsNullOrEmpty(input_identifier))
        {
            return null;
        }
        else
        {
            string identifier = input_identifier.Replace("https://orcid.org/", "");
            identifier = identifier.Replace("http://orcid.org/", "");
            identifier = identifier.Replace("/", "-");
            identifier = identifier.Replace(" ", "-");

            if (identifier.Length == 16)
            {
                identifier = identifier.Substring(0, 4) + "-" + identifier.Substring(4, 4) +
                             "-" + identifier.Substring(8, 4) + "-" + identifier.Substring(12, 4);
            }

            if (identifier.Length == 15) identifier = "0000" + identifier;
            if (identifier.Length == 14) identifier = "0000-" + identifier;

            return identifier;
        }
    }

    
    public static string? TidyOrgName(this string? in_name, string sid)
    {
        if (string.IsNullOrEmpty(in_name))
        {
            return null;
        }

        string? name = in_name;

        if (name.Contains('.'))
        {
            // protect these exceptions to the remove full stop rule
            name = name.Replace(".com", "|com");
            name = name.Replace(".gov", "|gov");
            name = name.Replace(".org", "|org");

            name = name.Replace(".", "");

            name = name.Replace("|com", ".com");
            name = name.Replace("|gov", ".gov");
            name = name.Replace("|org", ".org");
        }

        // Replace any apostrophes

        name = name.ReplaceApos();

        if (string.IsNullOrEmpty(name))
        {
            return null;
        }
        
        // Trim any odd' characters
        name = name.Trim(',', '-', '*', ';', ' ');

        // try and deal with possible ambiguities (organisations with genuinely the same name)

        string nLower = name.ToLower();
        if (nLower.Contains("newcastle") && nLower.Contains("university")
                                         && !nLower.Contains("hospital"))
        {
            if (nLower.Contains("nsw") || nLower.Contains("australia"))
            {
                name = "University of Newcastle (Australia)";
            }
            else if (nLower.Contains("uk") || nLower.Contains("tyne"))
            {
                name = "University of Newcastle (UK)";
            }
            else if (sid.StartsWith("ACTRN"))
            {
                name = "University of Newcastle (Australia)";
            }
            else
            {
                name = "University of Newcastle (UK)";
            }
        }

        if (nLower.Contains("china medical") && nLower.Contains("university"))
        {
            if (nLower.Contains("taiwan") || nLower.Contains("taichung"))
            {
                name = "China Medical University, Taiwan";
            }
            else if (nLower.Contains("Shenyang") || nLower.Contains("prc"))
            {
                name = "China Medical University";
            }
            else if (sid.StartsWith("Chi"))
            {
                name = "China Medical University";
            }
        }

        if (nLower.Contains("national") && nLower.Contains("cancer center"))
        {
            if (sid.StartsWith("KCT"))
            {
                name = "National Cancer Center, Korea";
            }
            else if (sid.StartsWith("JPRN"))
            {
                name = "National Cancer Center, Japan";
            }
        }

        return name;
    }


    public static string? TidyPersonName(this string? in_name)
    {
        // Replace apostrophes and remove periods.
        
        string? name1 = in_name.ReplaceApos();
        string? pName = name1?.Replace(".", "");
        
        if (string.IsNullOrEmpty(pName))
        {
            return null;
        }

        // Check for and remove professional titles

        string low_name = pName.ToLower();

        if (low_name.StartsWith("professor "))
        {
            pName = pName[10..];
        }
        else if (low_name.StartsWith("prof "))
        {
            pName = pName[5..];
        }
        else if (low_name.StartsWith("associate professor ") 
                 || low_name.StartsWith("assistant professor "))
        {
            pName = pName[20..];
        }
        else if (low_name.StartsWith("associate prof ") 
                 || low_name.StartsWith("assistant prof "))
        {
            pName = pName[15..];
        }
        else if (low_name.StartsWith("dr med ") 
                 || low_name.StartsWith("a/prof ") || low_name.StartsWith("a prof "))
        {
            pName = pName[7..];
        }
        else if (low_name.StartsWith("dr ") || low_name.StartsWith("mr ")
                                            || low_name.StartsWith("ms "))
        {
            pName = pName[3..];
        }
        else if (low_name.StartsWith("assoc prof "))
        {
            pName = pName[11..];
        }
        else if (low_name.StartsWith("a/ prof "))
        {
            pName = pName[8..];
        }
        else if (low_name.StartsWith("assocprof "))
        {
            pName = pName[10..];
        }
        else if (low_name.StartsWith("dr") && low_name.Length > 2
                                           && pName[2].ToString() == low_name[2].ToString().ToUpper())
        {
            pName = pName[2..];
        }
        else if (low_name is "dr" or "mr" or "ms")
        {
            pName = "";
        }
       
        if (pName == "")
        {
            return pName;
        }
        
        // remove some trailing qualifications

        int comma_pos = pName.IndexOf(',', StringComparison.Ordinal);
        if (comma_pos > -1)
        {
            pName = pName[..comma_pos];
        }

        string low_name2 = pName.ToLower();
        if (low_name2.EndsWith(" phd") || low_name2.EndsWith(" msc"))
        {
            pName = pName[..^3];
        }
        else if (low_name2.EndsWith(" ms") || low_name2.EndsWith(" md"))
        {
            pName = pName[..^2];
        }
        else if (low_name2.EndsWith(" ms(ophthal)"))
        {
            pName = pName[..^12];
        }
        else if (low_name2.EndsWith("phd candidate"))
        {
            pName = pName[..^13];
        }

        return pName.Trim(' ', '-', ',');
    }
    

    public static bool IsNotPlaceHolder(this string? in_string)
    {
        if (string.IsNullOrEmpty(in_string))
        {
            return false;
        }
 
        bool result = true;  // default assumption
        string low_string = in_string.ToLower().Trim();
        
        if (low_string.Length < 3)
        {
            result = false;
        }
        else if (low_string is "n.a." or "n.a" or "n/a" or "nil" or "nill" or "non")
        {
            result = false;
        }
        else if (low_string is "none" or "not done" or "same as above" or "in preparation" or "non fornito")
        {
             result = false;
        }        
        else if (low_string.StartsWith("no ") || low_string == "not applicable" || low_string.StartsWith("not prov"))
        {
            result = false;
        }
        else if (low_string.StartsWith("non fund") || low_string.StartsWith("non spon")
                                                   || low_string.StartsWith("nonfun") || low_string.StartsWith("noneno")
                                                   || low_string.StartsWith("organisation name "))
        {
            result = false;
        }
        else if (low_string.StartsWith("not applic") || low_string.StartsWith("not aplic")
                || low_string.StartsWith("non applic") || low_string.StartsWith("non aplic")
                || low_string.StartsWith("no applic") || low_string.StartsWith("no aplic"))
        {
            result = false;
        }
        else if (low_string.StartsWith("see ") || low_string.StartsWith("not avail")
                || low_string.StartsWith("non dispo") || low_string.Contains(" none."))
        {
            result = false;
        }
        
        return result;
    }
    
    
    public static bool AppearsGenuinePersonName(this string? person_name)
    {
        if (string.IsNullOrEmpty(person_name))
        {
            return false;
        }

        bool result = true; // default assumption
        string in_name = person_name.ToLower();

        string low_name = in_name.ToLower();
        if (low_name.Contains("research") || low_name.Contains("development") ||
            low_name.Contains("trials") || low_name.Contains("pharma") ||
            low_name.Contains("national") || low_name.Contains("college") ||
            low_name.Contains("board") || low_name.Contains("council") ||
            low_name.Contains("ltd") || low_name.Contains("inc.")
           )
        {
            result = false;
        }

        if (low_name.Contains(" group") || low_name.StartsWith("group") ||
            low_name.Contains(" assoc") || low_name.Contains(" team") ||
            low_name.Contains("collab") || low_name.Contains("network"))
        {
            result = false;
        }
            
        if (low_name.Contains("labor") || low_name.Contains("labat") ||
            low_name.Contains("institu") || low_name.Contains("istitu") ||
            low_name.Contains("school") || low_name.Contains("founda") ||
            low_name.Contains("associat") || low_name.Contains("univers") )
        {
            result = false;
        }
       
        return result;
    }
    
    
    public static bool AppearsGenuineOrgName(this string? org_name)
    {
        if (string.IsNullOrEmpty(org_name))
        {
            return false;
        }

        bool result = true;   // default assumption
        string in_name = org_name.ToLower();

        if (in_name.StartsWith("investigator ") || in_name is "investigator" or "self")
        {
            return false;
        }
        if (in_name.Contains("thesis"))
        {
            return false;
        }
        if (in_name is "seung-jung park" or "kang yan")
        {
            return false;  // a few specific individuals...
        }
        
        if (in_name.Contains("professor") || in_name.Contains("prof ")
                  || in_name.Contains("prof. ") || in_name.Contains("associate prof")
                  || in_name.Contains("assistant prof"))
        {
            result = false;
        }
        else if (in_name.Contains("assoc prof") || in_name.Contains("a/prof ")
                   || in_name.Contains("a/ prof") || in_name.Contains("assocprof")
                   || in_name.Contains("a prof"))
        {
            result = false;
        }
        else if (in_name.StartsWith("dr med ") || in_name.StartsWith("dr ") 
                 || in_name.StartsWith("dr. ") || in_name.StartsWith("mr ")
                 || in_name.StartsWith("ms "))
        {
            result = false;
        }
        else if (in_name.StartsWith("dr")
                 && org_name[2].ToString() == in_name[2].ToString().ToUpper())
        {
            result = false;
        }
        else if (in_name.EndsWith(" md") || in_name.EndsWith(" phd") ||
                 in_name.Contains(" md,") || in_name.Contains(" md ") ||
                 in_name.Contains(" phd,") || in_name.Contains(" phd "))
        {
            result = false;
        }
        
        // In some cases components of a organisation's name indicate that
        // the name is that of a person rather than an organisation.
        // However it may be that the name is a composite of both a person's
        // and an organisation's name. This therefore needs to be checked.
        
        if (!result && !in_name.AppearsGenuinePersonName())
        {
            result = true;
        }
        return result;
    }

    
    public static string? ExtractOrganisation(this string affiliation, string sid)
    {
        if (string.IsNullOrEmpty(affiliation))
        {
            return null;
        }
        
        string? affil_organisation = "";
        string aff = affiliation.ToLower();
        aff = aff.Replace("&#44;", ",");

        if (!aff.Contains(','))
        {
            affil_organisation = affiliation;  // cannot do a lot without a separating comma!
        }
        else if (aff.Contains("univers"))
        {
            affil_organisation = FindSubPhrase(affiliation, "univers");
        }
        else if (aff.Contains("hospit"))
        {
            affil_organisation = FindSubPhrase(affiliation, "hospit");
        }
        else if (aff.Contains("klinik"))
        {
            affil_organisation = FindSubPhrase(affiliation, "klinik");
        }
        else if (aff.Contains("instit"))
        {
            affil_organisation = FindSubPhrase(affiliation, "instit");
        }
        else if (aff.Contains("nation"))
        {
            affil_organisation = FindSubPhrase(affiliation, "nation");
        }
        else if (aff.Contains(" inc."))
        {
            if (aff.Contains(", inc."))
            {
                affil_organisation = FindSubPhrase(affiliation, ", inc.");
            }
            else
            {
                affil_organisation = FindSubPhrase(affiliation, " inc.");
            }

        }
        else if (aff.Contains(" ltd"))
        {
            if (aff.Contains(", ltd"))
            {
                affil_organisation = FindSubPhrase(affiliation, ", ltd");
            }
            else
            {
                affil_organisation = FindSubPhrase(affiliation, " ltd");
            }
        }
        return TidyOrgName(affil_organisation, sid);
    }


    public static string? FindSubPhrase(this string? phrase, string target)
    {
        if (string.IsNullOrEmpty(phrase))
        {
            return null;
        }

        string p = phrase.ToLower();
        string t = target.ToLower();

        // Ignore trailing commas after some 'university of' states names.
        
        p = p.Replace("california,", "california*");
        p = p.Replace("wisconsin,", "wisconsin*");

        // Find target in phrase if possible, and the position of the preceding comma,
        // and the comma after the target (if any). 
        // if no preceding comma make start the beginning of the string.
        // if no following comma make end the end of the string
                    
        int startPos = p.IndexOf(t, StringComparison.Ordinal);
        if (startPos == -1)
        {
            return phrase;
        }

        // if commaPos1 is -1 (no preceding comma) adding 1 below
        // makes it 0, the start of the string, as required.
        
        int commaPos1 = p.LastIndexOf(",", startPos, StringComparison.Ordinal); 
        int commaPos2 = p.IndexOf(",", startPos + target.Length - 1, StringComparison.Ordinal);
        if (commaPos2 == -1)
        {
            commaPos2 = p.Length;
        }

        string org_name = phrase[(commaPos1 + 1)..commaPos2].Trim();
        
        org_name = org_name.Replace("*", "'");  // in case this happened above
        
        return org_name;
    }


    public static List<string>? SplitStringWithMinWordSize(this string? input_string, char separator, int min_width)
    {
        if (!string.IsNullOrEmpty(input_string))
        {
            return null;
        }

        // try and avoid spurious split string results
        string[] split_strings = input_string!.Split(separator);
        for (int j = 0; j < split_strings.Length; j++)
        {
            if (split_strings[j].Length < min_width)
            {
                if (j == 0)
                {
                    split_strings[1] = split_strings[0] + "," + split_strings[1];
                }
                else
                {
                    split_strings[j - 1] = split_strings[j - 1] + "," + split_strings[j];
                }
            }
        }

        List<string> strings = new();
        foreach (string ss in split_strings)
        {
            if (ss.Length >= min_width)
            {
                strings.Add(ss);
            }
        }

        return strings;
    }


    public static List<string> GetNumberedStrings(this string input_string, string number_suffix, int max_number)
    {
        List<string> split_strings = new();
        for (int i = max_number; i > 0; i--)
        {
            string string_number = i + number_suffix;
            int number_pos = input_string.LastIndexOf(string_number, StringComparison.Ordinal);
            if (number_pos != -1)
            {
                string string_to_store = input_string[(number_pos + string_number.Length)..].Trim();
                split_strings.Add(string_to_store);
                input_string = input_string[..number_pos];
            }
        }

        // Anything left at the front of the string?
        if (input_string != "")
        {
            split_strings.Add(input_string);
        }

        // reverse order before returning
        List<string> reversed_strings = new();
        for (int j = split_strings.Count - 1; j >= 0; j--)
        {
            reversed_strings.Add(split_strings[j]);
        }

        return reversed_strings;
    }

}

