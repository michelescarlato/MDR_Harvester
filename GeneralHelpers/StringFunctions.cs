
namespace MDR_Harvester.Extensions;

public static class StringHelpers
{
    public static string? TrimPlus(this string? input_string)
    {
        // removes beginning or trailing carriage returns, tabs and spaces
        if (string.IsNullOrEmpty(input_string))
        {
            return null;
        }
        else
        {
            return input_string.Trim('\r', '\n', '\t', ' ');
        }
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
        // except in a few cases commas may be in a string as "&#44;".

        string output_string = input_string.Replace("&#44;", ",");

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

        // Look for paragraph tags

        while (output_string.Contains("<p"))
        {
            // replace any p start tags with a carriage return

            int start_pos = output_string.IndexOf("<p", StringComparison.Ordinal);
            int end_pos = output_string.IndexOf(">", start_pos, StringComparison.Ordinal);
            output_string = output_string[..start_pos] + "\n" + output_string[(end_pos + 1)..];
        }

        output_string = output_string.Replace("</p>", "");

        // Check for any list structures

        if (output_string.Contains("<li"))
        {
            while (output_string.Contains("<li"))
            {
                // replace any li start tags with a carriage return and bullet

                int start_pos = output_string.IndexOf("<li", StringComparison.Ordinal);
                int end_pos = output_string.IndexOf(">", start_pos, StringComparison.Ordinal);
                output_string = output_string[..start_pos] + "\n\u2022 " + output_string[(end_pos + 1)..];
            }

            // remove any list start and end tags

            while (output_string.Contains("<ul"))
            {
                int start_pos = output_string.IndexOf("<ul", StringComparison.Ordinal);
                int end_pos = output_string.IndexOf(">", start_pos, StringComparison.Ordinal);
                output_string = output_string[..start_pos] + output_string[(end_pos + 1)..];
            }

            while (output_string.Contains("<ol"))
            {
                int start_pos = output_string.IndexOf("<ol", StringComparison.Ordinal);
                int end_pos = output_string.IndexOf(">", start_pos, StringComparison.Ordinal);
                output_string = output_string[..start_pos] + output_string[(end_pos + 1)..];
            }

            output_string = output_string.Replace("</li>", "").Replace("</ul>", "").Replace("</ol>", "");
        }

        while (output_string.Contains("<div"))
        {
            // remove any div start tags
            int start_pos = output_string.IndexOf("<div", StringComparison.Ordinal);
            int end_pos = output_string.IndexOf(">", start_pos, StringComparison.Ordinal);
            output_string = output_string[..start_pos] + output_string[(end_pos + 1)..];
        }

        while (output_string.Contains("<span"))
        {
            // remove any span start tags
            int start_pos = output_string.IndexOf("<span", StringComparison.Ordinal);
            int end_pos = output_string.IndexOf(">", start_pos, StringComparison.Ordinal);
            output_string = output_string[..start_pos] + output_string[(end_pos + 1)..];
        }

        output_string = output_string.Replace("</span>", "").Replace("</div>", "");

        // check need to continue

        if (!(output_string.Contains('<') && output_string.Contains('>')))
        {
            return output_string;
        }

        // Assume these will be simple tags, without classes
        output_string = output_string.Replace("<b>", "").Replace("</b>", "").Replace("<i>", "").Replace("</i>", "");
        output_string = output_string.Replace("<em>", "").Replace("</em>", "").Replace("<u>", "").Replace("</u>", "");
        output_string = output_string.Replace("<strong>", "").Replace("</strong>", "");

        while (output_string.Contains("<a"))
        {
            // remove any link start tags - appears to be very rare
            int start_pos = output_string.IndexOf("<a", StringComparison.Ordinal);
            int end_pos = output_string.IndexOf(">", start_pos, StringComparison.Ordinal);
            output_string = output_string[..start_pos] + output_string[(end_pos + 1)..];
        }

        output_string = output_string.Replace("</a>", "");

        // try and replace sub and super scripts

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


    public static string? ReplacNBSpaces(this string? input_string)
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


    public static string lang_3_to_2(this string input_lang_code)
    {
        return input_lang_code switch
        {
            "fre" => "fr",
            "ger" => "de",
            "spa" => "es",
            "ita" => "it",
            "por" => "pt",
            "rus" => "ru",
            "tur" => "tr",
            "hun" => "hu",
            "pol" => "pl",
            "swe" => "sv",
            "nor" => "no",
            "dan" => "da",
            "fin" => "fi",
            _ => "??"
        };
    }


    public static string? TidyOrgName(this string? in_name, string sid)
    {
        if (string.IsNullOrEmpty(in_name))
        {
            return null;
        }

        string? name = in_name;

        if (name.Contains("."))
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

        // Trim any odd' characters

        name = name!.Trim(',', '-', '*', ';', ' ');

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

        // Check for professional titles

        string low_name = pName.ToLower();

        if (low_name.StartsWith("professor "))
        {
            pName = pName[10..];
        }
        else if (low_name.StartsWith("associate professor "))
        {
            pName = pName[20..];
        }
        else if (low_name.StartsWith("prof "))
        {
            pName = pName[5..];
        }
        else if (low_name.StartsWith("dr med "))
        {
            pName = pName[7..];
        }
        else if (low_name.StartsWith("dr ") || low_name.StartsWith("mr ")
                                            || low_name.StartsWith("ms "))
        {
            pName = pName[3..];
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
        else if (low_name2.EndsWith(" ms"))
        {
            pName = pName[..^2];
        }
        else if (low_name2.EndsWith(" ms(ophthal)"))
        {
            pName = pName[..^12];
        }

        return pName.Trim(' ', '-');
    }


    public static bool CheckPersonName(this string? in_name)
    {
        if (string.IsNullOrEmpty(in_name))
        {
            return false;
        }
        else
        {
            bool result = true;
            string low_name = in_name.ToLower();
            if (low_name.Contains("research") ||
                low_name.Contains("development") ||
                low_name.Contains("trials") ||
                low_name.Contains("pharma") ||
                low_name.Contains("ltd") ||
                low_name.Contains("inc.")
               )
            {
                result = false;
            }

            return result;
        }
    }


    public static bool AppearsGenuineTitle(this string? in_title)
    {
        if (string.IsNullOrEmpty(in_title))
        {
            return false;
        }

        bool result = true;
        string lower_title = in_title.ToLower().Trim();

        if (lower_title is "n.a." or "na" or "n.a" or "n/a")
        {
            result = false;
        }
        else if (lower_title is "none" or "not done" or "same as above" or "in preparation" or "non fornito")
        {
             result = false;
        }
        else if (lower_title.StartsWith("not applic") || lower_title.StartsWith("not aplic")
                || lower_title.StartsWith("non applic") || lower_title.StartsWith("non aplic")
                || lower_title.StartsWith("no applic") || lower_title.StartsWith("no aplic"))
        {
            result = false;
        }
        else if (lower_title.StartsWith("see ") || lower_title.StartsWith("not avail")
                                                || lower_title.StartsWith("non dispo"))
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

        bool result = true;
        string in_name = org_name.ToLower();

        if (in_name.Length < 3)
        {
            result = false;
        }
        else if (in_name is "n.a." or "n a" or "n/a" or "nil" or "nill" or "non")
        {
            result = false;
        }
        else if (in_name.StartsWith("no ") || in_name == "not applicable" || in_name.StartsWith("not prov"))
        {
            result = false;
        }
        else if (in_name == "none" || in_name.StartsWith("non fund") || in_name.StartsWith("non spon")
                 || in_name.StartsWith("nonfun") || in_name.StartsWith("noneno"))
        {
            result = false;
        }
        else if (in_name.StartsWith("investigator ") || in_name is "investigator" or "self" 
                                                     || in_name.StartsWith("Organisation name "))
        {
            result = false;
        }
        else if (in_name.Contains("thesis") || in_name.Contains(" none."))
        {
            result = false;
        }
        else if (in_name.StartsWith("professor") || in_name.StartsWith("prof ")
                                                 || in_name.StartsWith("prof. ") ||
                                                 in_name.StartsWith("associate prof"))
        {
            result = false;
        }
        else if (in_name.StartsWith("dr med ") || in_name.StartsWith("dr ") || in_name.StartsWith("mr ")
                 || in_name.StartsWith("ms "))
        {
            result = false;
        }
        else if (in_name.StartsWith("dr")
                 && org_name[2].ToString() == in_name[2].ToString().ToUpper())
        {
            result = false;
        }

        return result;
    }


    public static bool CheckIfIndividual(this string? orgname)
    {
        if (string.IsNullOrEmpty(orgname))
        {
            return false;
        }

        bool make_individual = false;

        // if looks like an individual's name
        if (orgname.EndsWith(" md") || orgname.EndsWith(" phd") ||
            orgname.Contains(" md,") || orgname.Contains(" md ") ||
            orgname.Contains(" phd,") || orgname.Contains(" phd ") ||
            orgname.Contains("dr ") || orgname.Contains("dr.") ||
            orgname.Contains("prof ") || orgname.Contains("prof.") ||
            orgname.Contains("professor"))
        {
            make_individual = true;
            // but if part of a organisation reference...
            if (orgname.Contains("hosp") || orgname.Contains("univer") ||
                orgname.Contains("labor") || orgname.Contains("labat") ||
                orgname.Contains("institu") || orgname.Contains("istitu") ||
                orgname.Contains("school") || orgname.Contains("founda") ||
                orgname.Contains("associat"))

            {
                make_individual = false;
            }
        }

        // A few specific individuals...

        if (orgname == "seung-jung park" || orgname == "kang yan")
        {
            make_individual = true;
        }

        return make_individual;
    }


    public static string? ExtractOrganisation(this string affiliation, string sid)
    {
        if (string.IsNullOrEmpty(affiliation))
        {
            return null;
        }
        
        string? affil_organisation = "";
        string aff = affiliation.ToLower();

        if (!aff.Contains(","))
        {
            affil_organisation = affiliation;
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
            affil_organisation = FindSubPhrase(affiliation, " inc.");
        }
        else if (aff.Contains(" ltd"))
        {
            affil_organisation = FindSubPhrase(affiliation, " ltd");
        }

        return TidyOrgName(affil_organisation, sid);
    }


    public static string? FindSubPhrase(this string? phrase, string target)
    {
        if (string.IsNullOrEmpty(phrase))
        {
            return null;
        }

        string phrase1 = phrase.Replace("&#44;", ",");
        string p = phrase1.ToLower();
        string t = target.ToLower();

        // ignore trailing commas after some states names.
        p = p.Replace("california,", "california*");
        p = p.Replace("wisconsin,", "wisconsin*");

        // Find target in phrase if possible, and the position
        // of the preceding comma, and the comma after the target (if any)
        // if no preceding comma make start the beginning of the string.
        // if no following comma make end the end of the string
                    
        int startPos = p.IndexOf(t, StringComparison.Ordinal);
        if (startPos == -1)
        {
            return phrase1;
        }

        int commaPos1 = p.LastIndexOf(",", startPos, StringComparison.Ordinal); 
        if (commaPos1 == -1)
        {
            commaPos1 = 0;
        }
        int commaPos2 = p.IndexOf(",", startPos + target.Length - 1, StringComparison.Ordinal);
        if (commaPos2 == -1)
        {
            commaPos2 = p.Length;
        }

        string org_name = phrase1[(commaPos1 + 1)..commaPos2].Trim();

        if (org_name.ToLower().StartsWith("the "))
        {
            org_name = org_name[4..];
        }

        return org_name;
    }


    public static List<string>? SplitStringWithMinWordSize(this string? input_string, char separator, int min_width)
    {
        if (!string.IsNullOrEmpty(input_string))
        {
            return null;
        }
        else
        {
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
    }


    public static List<string> GetNumberedStrings(this string input_string, string number_suffix, int max_number)
    {
        List<string> split_strings = new();
        for (int i = max_number; i > 0; i--)
        {
            string string_number = i.ToString() + number_suffix;
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


/*

public static string FindPossibleSeparator(this string inputString)
    {
        // Look for numbered lists and try to
        // identify the symbol following the number.

        string numInd = "";
        if (inputString.Contains("1. ") && inputString.Contains("2. "))
        {
            numInd = ". ";
        }
        else if (inputString.Contains("1) ") && inputString.Contains("2) "))
        {
            numInd = ") ";
        }
        else if (inputString.Contains("1 -") && inputString.Contains("2 -"))
        {
            numInd = " -";
        }
        else if (inputString.Contains("1.") && inputString.Contains("2."))
        {
            numInd = ".";
        }

        return numInd;
    }

}
*/
/*

public static List<Criterion>? GetNumberedCritera(this string input_string, string type, int max_number)
{
    if (string.IsNullOrEmpty(input_string))
    {
        return null;
    }
    else
    {
        // Establish criteria list to receive results,
        // save the original input for later comparison,
        // and set up criterion type codes to be used.
        
        List<Criterion> cr = new();
        string original_input = input_string;
        
        int single_crit = type == "inclusion" ? 1 : 2; 
        int all_crit = single_crit + 10;
        int pre_crit = single_crit + 100;
        int post_crit = single_crit + 200;
        int grp_hdr = single_crit + 300;
        int no_sep = single_crit + 1000;
        
        string single_type = type + " criterion";
        string all_crit_type = type + " criteria (as one statement)";
        string pre_crit_type = type + " criteria prefix statement";
        string post_crit_type = type + " criteria supplementary sttaement";
        string grp_hdr_type = type + " criteria group heading";
        string no_sep_type = type + " with no separator";
        
        int n = 0;

        while (input_string != "")
        {
            // Look for numbered lists and try to
            // identify the symbol following the number.

            string num_ind = input_string.FindPossibleSeparator();
            if (num_ind == "")
            {
                // No separators left (if there ever were any). Add the criterion
                // to the list and ensure the input text = "" to exit the loop.
                // What is returned depends on whether this is the whole of the
                // original input text, or what is left after a run back
                // through the numbered criteria. Ways of identifying group 
                // headers to be added later.

                n++; 
                if (input_string == original_input)
                {
                    cr.Add(new Criterion(n, no_sep, no_sep_type, input_string)); // no common separator found
                }
                else
                {
                    cr.Add(new Criterion(n, pre_crit, pre_crit_type, input_string)); 
                }
                input_string = "";
            }
            else
            {
                // Split list on the identified separator, 
                // working backwards from the end of the string.

                // Note i is the number in the text being processed
                // n is the overall sequence number, which may span
                // more than one sublist, and m is the sequence number
                // within the current (sub)list.

                int m = 0;       
                for (int i = max_number; i > 0; i--)
                {
                    string string_number = i + num_ind;
                    int number_pos = input_string.LastIndexOf(string_number);
                    if (number_pos != -1)
                    {
                        // But is this a genuine numered criterion or the presence of the 
                        // sought characters within a larger number , e.g 7.45 rather than 7.4.

                        // Remove the number and any separator from the found string.
                        // TrimPlus removes any trailing carriage returns.

                        m++;
                        string string_to_store = input_string[(number_pos + string_number.Length)..].TrimPlus();

                        // But is it the final string in the list, i.e. the first to be found?
                        // If so an internal carriage return is likely to indicate a supplementary statement.
                        // TrimPlus will remove any trailing carriage return.
                        
                        if (m == 1)
                        {
                            int cr_pos = string_to_store.LastIndexOf('\n');
                            if (cr_pos != -1)
                            {
                                // Split this 'final string' and store it as two
                                // separate statements, the last one being a supplement.

                                string criterion_to_store = string_to_store[..cr_pos];
                                string supp_statement = string_to_store[(cr_pos + 1)..];
                                n++;
                                cr.Add(new Criterion(n, post_crit, post_crit_type, supp_statement.TrimPlus()));
                                n++;
                                cr.Add(new Criterion(n, single_crit, single_type, criterion_to_store.TrimPlus()));
                            }
                        }
                        else
                        {
                            n++;
                            cr.Add(new Criterion(n, single_crit, single_type, string_to_store.TrimPlus()));
                        }
                        
                        // Either way truncate the string.
                        // At the end may be an empty string - 
                        // If not it will go through the While loop again.
                                                
                        input_string = input_string[..number_pos];
                    }
                }
            }
        }

        // reverse order before returning.

        return cr.OrderByDescending(c => c.SeqNum).ToList();
    }
}
}
*/
/*
public class Criterion
{
    public int? SeqNum { get; set; }
    public int? CritTypeId { get; set; }
    public string? CritType { get; set; }
    public string? CritText { get; set; }

    public Criterion(int? seqNum, int? critTypeId, 
                     string? critType, string? critText)
    {
        SeqNum = seqNum;
        CritTypeId = critTypeId;
        CritType = critType;
        CritText = critText;
    }
}

*/
