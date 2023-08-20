
using System.Globalization;

namespace MDR_Harvester.Extensions;

public static class StringHelpers
{
    public static string? FullClean(this string? input_string)
    {
        // Used for all types of text, but especially larger descriptive fields
        
        if (string.IsNullOrEmpty(input_string))
        {
            return null;
        }

        string? output_string = input_string.TrimEnds();
        output_string = output_string.ReplaceCodes();        
        output_string = output_string.ReplaceTags();
        output_string = output_string.ReplaceApos();
        output_string = output_string.RegulariseStringEndings();
        return output_string.CompressSpaces();
    }
    
    public static string? LineClean(this string? input_string)
    {
        // Used for single lines or values, e.g. organisation names
        
        if (string.IsNullOrEmpty(input_string))
        {
            return null;
        }
        
        string? output_string = input_string.TrimEnds();
        output_string = output_string.ReplaceCodes();        
        return output_string.ReplaceApos();
    }
    
    public static string? TrimEnds(this string? input_string)
    {
        // removes beginning or trailing carriage returns, tabs and spaces.
        
        return string.IsNullOrEmpty(input_string) 
            ? null 
            : input_string.Trim('\r', '\n', '\t', ' ');
    }


    public static string? ReplaceCodes(this string? input_string)
    {
        // Necessary for a few sources in particular but a 
        // useful first step before further processing, e.g. of apostrophes
        
        if (string.IsNullOrEmpty(input_string))
        {
            return null;
        }
        string output_string = input_string;
        
        // decimal coded unicode characters
         
        if (output_string.Contains('&'))
        {
            output_string = input_string.Replace("&#44;", ",");
            output_string = output_string.Replace("&#45;", "-");
            output_string = output_string.Replace("&#38;", "&");
            output_string = output_string.Replace("&#39;", "'");
            output_string = output_string.Replace("&#8217;", "’");


            // replace escaped html / xml characters

            output_string = output_string.Replace("&lt;", "<");
            output_string = output_string.Replace("&gt;", ">");
            output_string = output_string.Replace("&amp;", "&");
            output_string = output_string.Replace("&nbsp;", " ");
        }

        // unicode equivalents of non-breaking spaces

        output_string = output_string.Replace('\u00A0', ' ');
        output_string = output_string.Replace('\u2000', ' ').Replace('\u2001', ' ');
        output_string = output_string.Replace('\u2002', ' ').Replace('\u2003', ' ');
        output_string = output_string.Replace('\u2007', ' ').Replace('\u2008', ' ');
        output_string = output_string.Replace('\u2009', ' ').Replace('\u200A', ' ');
        
        // may be in as explicit unicode codes

        if (output_string.Contains("\\u"))
        {
            output_string = output_string.Replace("\u00A0", " ");
            output_string = output_string.Replace("\u2000", " ").Replace("\u2001", " ");
            output_string = output_string.Replace("\u2002", " ").Replace("\u2003", " ");
            output_string = output_string.Replace("\u2007", " ").Replace("\u2008", " ");
            output_string = output_string.Replace("\u2009", " ").Replace("\u200A", " ");
        }

        // replace combination sometimes used to denote an 'unprintable character'
        
        output_string = output_string.Replace("â??", "");
        
        // replace 'double escape' sequence now found in some CTG text
        
        output_string = output_string.Replace("\\", "");
        
        // Drop any registration mark ot trademark symbols
        
        output_string = output_string.Replace(((char)174).ToString(), ""); 
        output_string = output_string.Replace('\u00AE'.ToString(), ""); 
        output_string = output_string.Replace('\u2122'.ToString(), "");
        output_string = output_string.Replace("\u00AE", "");  // if already expanded explicitly
        output_string = output_string.Replace("\u2122", ""); 
        
        
        return output_string;
    }


    public static string? ReplaceApos(this string? apos_name)
    {
        string? aName = apos_name.ReplaceCodes();
        if (string.IsNullOrEmpty(aName))
        {
            return null;
        }
        
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
        
        if (!(input_string.Contains('<') && input_string.Contains('>')))
        {
            return input_string;
        }

        // The commonest case...

        string output_string = input_string
            .Replace("<br>", "\n")
            .Replace("<br/>", "\n")
            .Replace("<br />", "\n")
            .Replace("<br/ >", "\n")
            .Replace("< br / >", "\n");

        if (!(output_string.Contains('<') && output_string.Contains('>')))
        {
            return output_string;
        }
        
        // Check need to continue.
        
        output_string = RemoveTag(output_string, "p", "\n");
        output_string = RemoveTag(output_string, "li", "\n\u2022 ");
        output_string = RemoveTag(output_string, "ul", "");
        output_string = RemoveTag(output_string, "ol", "");

        if (!(output_string.Contains('<') && output_string.Contains('>')))  
        {
            return output_string;
        }
        
        // check need to continue
        
        output_string = RemoveTag(output_string, "div", "");
        output_string = RemoveTag(output_string, "span", "");        
        output_string = RemoveTag(output_string, "a", "");     
        
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

    private static string RemoveTag(string input_string, string tag, string substitute)
    {
        string start_tag = "<" + tag;
        string end_tag = "</" + tag + ">";
        int search_start = 0, start_of_tag = 0;
        while (input_string.Contains(start_tag) && start_of_tag != -1)
        {
            // Although the while statement guarantees the presence of the start tag
            // a search for it may fail if the test below is failed, i.e. if it is not 
            // a 'true' html tag - ongoing search could therefore 'run off the end'
            
            start_of_tag = input_string.IndexOf(start_tag, search_start, StringComparison.Ordinal);
            if (start_of_tag != -1)        
            {
                // check no immediately following non-space character
                // and if that is the case look for the ending of the start tag
                // remove the text, between and including the tags, replacing it if necessary.

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
        
        // Then remove all corresponding end tags.
        
        input_string = input_string.Replace(end_tag, "");
        return input_string;
    }

    
    public static string? RegulariseStringEndings(this string? input_string)
    {
        if (string.IsNullOrEmpty(input_string))
        {
            return null;
        }
        string output_string = input_string.Replace("\r\n", "\n");
        return output_string.Replace("\r", "\n");
    }


    public static string? CompressSpaces(this string? input_string)
    {
        if (string.IsNullOrEmpty(input_string))
        {
            return null;
        }

        string output_string = input_string.Replace("\n ", "\n");            
        output_string = output_string.Replace("\r\n ", "\n");
        
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

    public static string? Capitalised(this string? input, TextInfo TI )
    {
        if (string.IsNullOrEmpty(input))
        {
            return null;
        }
        string output_string = TI.ToTitleCase(input.Trim().ToLower());
        
        // At present normally expecting a single word rather than a phrase
        // But some small words may be included in the string (e.g. in a keyword phrase)

        if (output_string.Contains(' '))
        {
            output_string = output_string.Replace(" And ", " and ").Replace(" The ", " the ");
            output_string = output_string.Replace(" Of", " of ").Replace(" To ", " to ");
            output_string = output_string.Replace(" In ", " in ").Replace(" On ", " on ");
            output_string = output_string.Replace(" For ", " for ");
        }
        return output_string.Replace("_", " ");  
    }

    public static string? CapFirstLetter(this string? input)
    {
        return string.IsNullOrEmpty(input) ? null : char.ToUpper(input[0]) + input[1..];
    }

    public static string? TidyORCIDId(this string? input_identifier)
    {
        if (string.IsNullOrEmpty(input_identifier))
        {
            return null;
        }

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

    
    public static string? TidyOrgName(this string? in_name, string sid)
    {
        string? name = in_name.LineClean();
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

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

        if (string.IsNullOrEmpty(name))
        {
            return null;
        }
        
        // A few org names (usually affiliation strings) have CRs, spaced hyphens etc. in them
        
        name = name.Replace("\n", ", ").Replace(" - ", ", ");
        name = name.Replace("<br>", ", ").Replace("<br/>", ", ");
        name = name.Replace(" - ", ", ");
        name = name.Replace(" ,", ", ");  // can be created by the line above
        name = name.Replace("  ", " ");   // likewise..., try and correct
        
        // Trim any 'odd' characters and remove any 'The ' prefix    
        
        name = name.Trim(',', '-', '*', ';', ' ');
        if (name.ToLower().StartsWith("the "))
        {
            if (name.Length > 4 && name.Split().Length > 2)
            {
                name = name[4..];
            }
        }
        
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
        
        string? name1 = in_name.LineClean();
        string? pName = name1?.Replace(".", "");
        
        if (string.IsNullOrEmpty(pName) || pName == "-")
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
 
        string low_string = in_string.ToLower().Trim();
        
        if (low_string.Length < 3)
        {
            return false;
        }
        if (low_string is "n.a." or "na" or "n.a" or "n/a" or "n/a." 
                             or "no" or "nil" or "nill" or "non")
        {
            return false;
        }
        if (low_string is "none" or "nd" or "not done" or "same as above" or "in preparation" or "non fornito")
        {
            return false;
        }  
        if (low_string is "not stated" or "nothing" or "other" or "not yet" or "pending")
        {
            return false;
        }   
        if (low_string.StartsWith("no ") || low_string == "not applicable" || low_string.StartsWith("not prov"))
        {
            return false;
        }
        if (low_string.StartsWith("non fund") || low_string.StartsWith("non spon")
                || low_string.StartsWith("nonfun") || low_string.StartsWith("noneno")
                || low_string.StartsWith("organisation name "))
        {
            return false;
        }
        if (low_string.StartsWith("not ") || low_string.StartsWith("to be ")
               || low_string.StartsWith("not-") || low_string.StartsWith("not_")
               || low_string.StartsWith("notapplic") || low_string.StartsWith("non applic") 
               || low_string.StartsWith("non aplic") || low_string.StartsWith("no applic")
               || low_string.StartsWith("no aplic"))
        {
            return false;
        }
        if (low_string.StartsWith("notavail") || low_string.StartsWith("tobealloc") 
                || low_string.StartsWith("tobeapp") || low_string.StartsWith("see ") 
                || low_string.StartsWith("not avail") || low_string.StartsWith("non dispo") 
                || low_string.Contains(" none."))
        {
            return false;
        }    

        return true;
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
            low_name.Contains("clinical") || low_name.Contains("advisor") ||
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
            low_name.Contains("associat") || low_name.Contains("univers") || 
            low_name.Contains("thesis"))
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
        
        // In some cases components of a organisation's name indicate (as above) that
        // the name is that of a person rather than an organisation.
        // However it may be that the name is a composite of both a person's
        // and an organisation's name. This therefore needs to be checked. If the 
        // name has organisation elements (i.e. fails the genuine person test)
        // return true instead.
        
        if (!result && !in_name.AppearsGenuinePersonName())
        {
            result = true;
        }
        return result;
    }

    
    public static string? ExtractOrganisation(this string affiliation, string sid)
    {
        string? affil = affiliation.LineClean();        
        if (string.IsNullOrEmpty(affil))
        {
            return null;
        }
        string? affil_organisation;
        string aff = affil.ToLower();
        
        if (aff.Contains("univers"))
        {
            affil_organisation = FindSubPhrase(affil, "univers");
        }
        else if (aff.Contains("hospit"))
        {
            affil_organisation = FindSubPhrase(affil, "hospit");
        }
        else if (aff.Contains("klinik"))
        {
            affil_organisation = FindSubPhrase(affil, "klinik");
        }
        else if (aff.Contains("instit"))
        {
            affil_organisation = FindSubPhrase(affil, "instit");
        }
        else if (aff.Contains("hôpital"))
        {
            affil_organisation = FindSubPhrase(affil, "hôpital");
        }
        else if (aff.Contains("clinic"))
        {
            affil_organisation = FindSubPhrase(affil, "clinic");
        }
        else if (aff.Contains("infirmary"))
        {
            affil_organisation = FindSubPhrase(affil, "infirmary");
        }
        else if (aff.Contains("medical center"))
        {
            affil_organisation = FindSubPhrase(affil, "medical center");
        }
        else if (aff.Contains("medical centre"))
        {
            affil_organisation = FindSubPhrase(affil, "medical centre");
        }
        else if (aff.Contains("college"))
        {
            affil_organisation = FindSubPhrase(affil, "college");
        }
        else if (aff.Contains("school"))
        {
            affil_organisation = FindSubPhrase(affil, "school");
        }
        else if (aff.Contains("école"))
        {
            affil_organisation = FindSubPhrase(affil, "école");
        }
        else if (aff.Contains("academy"))
        {
            affil_organisation = FindSubPhrase(affil, "academy");
        }
        else if (aff.Contains("nation"))
        {
            affil_organisation = FindSubPhrase(affil, "nation");
        }
        else if (aff.Contains(" inc."))
        {
            affil_organisation = FindSubPhrase(affil, aff.Contains(", inc.") ? ", inc." : " inc.");
        }
        else if (aff.Contains(" inc,"))
        {
            affil_organisation = FindSubPhrase(affil, aff.Contains(", inc,") ? ", inc," : " inc,");
        }
        else if (aff.Contains(" ltd"))
        {
            affil_organisation = FindSubPhrase(affil, aff.Contains(", ltd") ? ", ltd" : " ltd");
        }
        else if (aff.Contains("centre"))
        {
            affil_organisation = FindSubPhrase(affil, "centre");
        }
        else if (aff.Contains("center"))
        {
            affil_organisation = FindSubPhrase(affil, "center");
        }
        else if (aff.Contains("foundation"))
        {
            affil_organisation = FindSubPhrase(affil, "foundation");
        }
        else if (aff.Contains(" nhs"))
        {
            affil_organisation = FindSubPhrase(affil, " nhs");
        }
        else if (aff.Contains(" mc "))
        {
            affil_organisation = FindSubPhrase(affil, " mc ");
        }
        else if (aff.Contains(" chu "))
        {
            affil_organisation = FindSubPhrase(affil, " chu ");
        }
        else if (aff.Contains(" mc,"))
        {
            affil_organisation = FindSubPhrase(affil, " mc,");
        }
        else if (aff.Contains(" chu,"))
        {
            affil_organisation = FindSubPhrase(affil, " chu,");
        }
        else if (aff.Contains("unit"))
        {
            affil_organisation = FindSubPhrase(affil, "unit");
        }
        else
        {
            affil_organisation = affil;
        }
        return TidyOrgName(affil_organisation, sid);   // will return null if an empty string
    }


    public static string? FindSubPhrase(this string? phrase, string target)
    {
        if (string.IsNullOrEmpty(phrase))
        {
            return null;
        }

        string p = phrase.ToLower();
        string t = target.ToLower();

        // Ignore trailing commas after some base names. They require further elaboration....
        // Comma will be re-introduced in returned value
        
        p = p.Replace("california,", "california*");
        p = p.Replace("wisconsin,", "wisconsin*");
        p = p.Replace("mayo clinic,", "mayo clinic*");
        p = p.Replace("institute of psychiatry,", "institute of psychiatry*");
        p = p.Replace("centre hospitalier universitaire,", "centre hospitalier universitaire*");
        p = p.Replace("hospital clínico universitario,", "hospital clínico universitario*");
        p = p.Replace("medical university,", "medical university*");
        p = p.Replace("university children’s hospital,", "university children’s hospital*");
        p = p.Replace("hospital for tropical diseases,", "hospital for tropical diseases*");
        p = p.Replace("prince of wales hospital,", "prince of wales hospital*");
        p = p.Replace("princess alexandra hospital,", "princess alexandra hospital*");
        p = p.Replace("queen elizabeth hospital,", "queen elizabeth hospital*");
        p = p.Replace("queen’s hospital,", "queen’s hospital*");
        p = p.Replace("texas health science center,", "texas health science center*");
        
        // Find target in phrase if possible, and the position of the preceding comma,
        // and the comma after the target (if any). 
        // if no preceding comma make start the beginning of the string.
        // if no following comma make end the end of the string
        
        try
        {
            int startPos = p.IndexOf(t, StringComparison.Ordinal);
            if (startPos == -1)
            {
                return phrase;  // target was not found (should not happen but...)
            }
        
            // if the target starts with a comma (e.g. ', inc', ', ltd') do the last index search
            // for the preceding comma at the position just before that comma.

            int searchStartPos = t.StartsWith(",") ? startPos - 1 : startPos;
            
            // if commaPos1 is -1 (no preceding comma) adding 1 below
            // makes it 0, the start of the string, as required.
            
            int commaPos1 = p.LastIndexOf(',', searchStartPos); 
            int commaPos2 = p.IndexOf(',', startPos + target.Length - 1);
            if (commaPos2 == -1)
            {
                commaPos2 = p.Length;
            }

            string org_name = phrase[(commaPos1 + 1)..commaPos2].Trim();
            
            org_name = org_name.Replace("*", ",");  // in case this happened above
            return org_name;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return null;
        }
        
    }


    public static List<string>? SplitStringWithMinWordSize(this string? input_string, char separator, int min_width)
    {
        if (string.IsNullOrEmpty(input_string))
        {
            return null;
        }
        string[] split_strings = input_string.Split(separator, StringSplitOptions.RemoveEmptyEntries);
        if (split_strings.Length == 0)
        {
            return null;
        }
        
        // try and avoid spurious split string results, with small qualifiers / additions
        
        int last_valid = 0;
        for (int j = 0; j < split_strings.Length; j++)
        {
            if (split_strings[j].Length < min_width)
            {
                if (j == 0 && split_strings.Length > 1)
                {
                    split_strings[1] = split_strings[0] + "," + split_strings[1];
                    split_strings[0] = "";
                    last_valid = 1;
                }
                else
                {
                    if (j > 0)
                    {
                        split_strings[last_valid] = split_strings[last_valid] + "," + split_strings[j];
                        split_strings[j] = "";
                    }
                }
            }
            else
            {
                last_valid = j;
            }
        }

        List<string> strings = new();
        if (split_strings.Length > 0)
        {
            strings.AddRange(split_strings.Where(t => t != ""));
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

