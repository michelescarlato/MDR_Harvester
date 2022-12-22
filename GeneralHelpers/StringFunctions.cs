using System;

namespace DataHarvester
{
    public class StringHelpers
    {
        private readonly LoggingHelper _logger;

        public StringHelpers(LoggingHelper logger)
        {
            _logger = logger;
        }

        public string TidyPersonName(string in_name)
        {
            // Remove periods and dashes

            string name = in_name.Replace(".", "");

            // Check for professional titles

            string low_name = name.ToLower();

            if (low_name.StartsWith("professor "))
            {
                name = name.Substring(10);
            }
            else if (low_name.StartsWith("associate professor "))
            {
                name = name.Substring(20);
            }
            else if (low_name.StartsWith("prof "))
            {
                name = name.Substring(5);
            }
            else if (low_name.StartsWith("dr med "))
            {
                name = name.Substring(7);
            }
            else if (low_name.StartsWith("dr ") || low_name.StartsWith("mr ")
                || low_name.StartsWith("ms "))
            { 
                name = name.Substring(3); 
            }
            else if (low_name.StartsWith("dr") && low_name.Length > 2
                && name[2].ToString() == low_name[2].ToString().ToUpper())
            {
                name = name.Substring(2);
            }
            else if (low_name =="dr" || low_name =="mr"
                || low_name =="ms")
            {
                name = "";
            }

            // Remove trailing qualifications

            int comma_pos = name.IndexOf(',');
            if (comma_pos > -1) 
            { 
                name = name.Substring(0, comma_pos); 
            }

            string low_name2 = name.ToLower();
            if (low_name2.EndsWith(" phd") || low_name2.EndsWith(" msc"))
            {
                name = name.Substring(0, name.Length - 3);
            }
            else if (low_name2.EndsWith(" ms"))
            {
                name = name.Substring(0, name.Length - 2);
            }
            else if (low_name2.EndsWith(" ms(ophthal)"))
            {
                name = name.Substring(0, name.Length - 12);
            }

            return name.Trim(' ', '-');
        }


        public bool CheckPersonName(string in_name)
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


        public string ReplaceApos(string apos_name)
        {
            try
            {

                if (apos_name == null)
                {
                    return null;
                }

                apos_name = apos_name.Replace("&#44;", ",");

                while (apos_name.Contains("'"))
                {
                    int apos_pos = apos_name.IndexOf("'");
                    int alen = apos_name.Length;
                    if (apos_pos == 0)
                    {
                        apos_name = "‘" + apos_name.Substring(1);
                    }
                    else if (apos_pos == alen - 1)
                    {
                        apos_name = apos_name.Substring(0, alen - 1) + "’";
                    }
                    else
                    {
                        if (apos_name[apos_pos - 1] == ' ' || apos_name[apos_pos - 1] == '(')
                        {
                            apos_name = apos_name.Substring(0, apos_pos) + "‘" + apos_name.Substring(apos_pos + 1, alen - apos_pos - 1);

                        }
                        else
                        {
                            apos_name = apos_name.Substring(0, apos_pos) + "’" + apos_name.Substring(apos_pos + 1, alen - apos_pos - 1);
                        }
                    }
                }

                return apos_name;
            }
            catch (Exception e)
            {
                _logger.LogError("In ReplaceApos: " + e.Message + " (input was '" + apos_name + "')");
                return apos_name;
            }

        }


        public string ReplaceTags(string input_string)
        {
            try
            {
                if (input_string == null)
                {
                    return null;
                }

                // needs to have opening and closing tags for further processing
                // except in a few cases commas may be in a string as "&#44;"
                string output_string = input_string.Replace("&#44;", ",");

                if (!(output_string.Contains("<") && output_string.Contains(">")))
                {
                    return output_string;
                }


                // The commonest case

                output_string = input_string
                                .Replace("<br>", "\n")
                                .Replace("<br/>", "\n")
                                .Replace("<br />", "\n")
                                .Replace("<br/ >", "\n")
                                .Replace("< br / >", "\n");



                // Check need to continue

                if (!(output_string.Contains("<") && output_string.Contains(">")))
                {
                    return output_string;
                }

                // Look for paragraph tags

                while (output_string.Contains("<p"))
                {
                    // replace any p start tags with a carriage return

                    int start_pos = output_string.IndexOf("<p");
                    int end_pos = output_string.IndexOf(">", start_pos);
                    output_string = output_string.Substring(0, start_pos) + "\n" + output_string.Substring(end_pos + 1);
                }

                output_string = output_string.Replace("</p>", "");

                // Check for any list structures

                if (output_string.Contains("<li>"))
                {
                    while (output_string.Contains("<li"))
                    {
                        // replace any li start tags with a carriage return and bullet

                        int start_pos = output_string.IndexOf("<li");
                        int end_pos = output_string.IndexOf(">", start_pos);
                        output_string = output_string.Substring(0, start_pos) + "\n\u2022 " + output_string.Substring(end_pos + 1);
                    }

                    // remove any list start and end tags

                    while (output_string.Contains("<ul"))
                    {
                        int start_pos = output_string.IndexOf("<ul");
                        int end_pos = output_string.IndexOf(">", start_pos);
                        output_string = output_string.Substring(0, start_pos) + output_string.Substring(end_pos + 1);
                    }

                    while (output_string.Contains("<ol"))
                    {
                        int start_pos = output_string.IndexOf("<ol");
                        int end_pos = output_string.IndexOf(">", start_pos);
                        output_string = output_string.Substring(0, start_pos) + output_string.Substring(end_pos + 1);
                    }

                    output_string = output_string.Replace("</li>", "").Replace("</ul>", "").Replace("</ol>", "");
                }

                while (output_string.Contains("<div"))
                {
                    // remove any div start tags
                    int start_pos = output_string.IndexOf("<div");
                    int end_pos = output_string.IndexOf(">", start_pos);
                    output_string = output_string.Substring(0, start_pos) + output_string.Substring(end_pos + 1);
                }

                while (output_string.Contains("<span"))
                {
                    // remove any span start tags
                    int start_pos = output_string.IndexOf("<span");
                    int end_pos = output_string.IndexOf(">", start_pos);
                    output_string = output_string.Substring(0, start_pos) + output_string.Substring(end_pos + 1);
                }

                output_string = output_string.Replace("</span>", "").Replace("</div>", "");

                // check need to continue

                if (!(output_string.Contains("<") && output_string.Contains(">")))
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
                    int start_pos = output_string.IndexOf("<a");
                    int end_pos = output_string.IndexOf(">", start_pos);
                    output_string = output_string.Substring(0, start_pos) + output_string.Substring(end_pos + 1);
                }

                output_string = output_string.Replace("</a>", "");

                // try and replace sub and super scripts

                while (output_string.Contains("<sub>"))
                {
                    int start_pos = output_string.IndexOf("<sub>");
                    int start_string = start_pos + 5;
                    int end_string = output_string.IndexOf("</sub>", start_string);
                    if (end_string != -1) // would indicate a non matched sub entry
                    {
                        int end_pos = end_string + 5;
                        string string_to_change = output_string.Substring(start_string, end_string - start_string);
                        string new_string = "";
                        for (int i = 0; i < string_to_change.Length; i++)
                        {
                            new_string += ChangeToSubUnicode(string_to_change[i]);
                        }
                        if (end_pos > output_string.Length - 1)
                        {
                            output_string = output_string.Substring(0, start_pos) + new_string;
                        }
                        else
                        {
                            output_string = output_string.Substring(0, start_pos) + new_string + output_string.Substring(end_pos + 1);
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
                    int start_pos = output_string.IndexOf("<sup>");
                    int start_string = start_pos + 5;
                    int end_string = output_string.IndexOf("</sup>", start_string);
                    if (end_string != -1) // would indicate a non matched sup entry
                    {
                        int end_pos = end_string + 5;
                        string string_to_change = output_string.Substring(start_string, end_string - start_string);
                        string new_string = "";
                        for (int i = 0; i < string_to_change.Length; i++)
                        {
                            new_string += ChangeToSupUnicode(string_to_change[i]);
                        }
                        if (end_pos > output_string.Length - 1)
                        {
                            output_string = output_string.Substring(0, start_pos) + new_string;
                        }
                        else
                        {
                            output_string = output_string.Substring(0, start_pos) + new_string + output_string.Substring(end_pos + 1);
                        }
                    }
                    else
                    {
                        // drop any that are left  (to get out of the loop)
                        output_string = output_string.Replace("</sup>", "");
                        output_string = output_string.Replace("<sup>", "");
                    }
                }

                return output_string;
            }

            catch (Exception e)
            {
                _logger.LogError("In replace_tags: " + e.Message + " (Input was '" + input_string + "')");
                return null;
            }
        }

        private char ChangeToSupUnicode(char a)
        {
            char unicode = a;
            switch (a)
            {
                case '0': unicode = '\u2070'; break;
                case '1': unicode = '\u0B09'; break;
                case '2': unicode = '\u0B02'; break;
                case '3': unicode = '\u0B03'; break;
                case '4': unicode = '\u2074'; break;
                case '5': unicode = '\u2075'; break;
                case '6': unicode = '\u2076'; break;
                case '7': unicode = '\u2077'; break;
                case '8': unicode = '\u2078'; break;
                case '9': unicode = '\u2079'; break;
                case 'i': unicode = '\u2071'; break;
                case '+': unicode = '\u207A'; break;
                case '-': unicode = '\u207B'; break;
                case '=': unicode = '\u207C'; break;
                case '(': unicode = '\u207D'; break;
                case ')': unicode = '\u207E'; break;
                case 'n': unicode = '\u207F'; break;
            }
            return unicode;
        }

        private char ChangeToSubUnicode(char a)
        {
            char unicode = a;
            switch (a)
            {
                case '0': unicode = '\u2080'; break;
                case '1': unicode = '\u2081'; break;
                case '2': unicode = '\u2082'; break;
                case '3': unicode = '\u2083'; break;
                case '4': unicode = '\u2084'; break;
                case '5': unicode = '\u2085'; break;
                case '6': unicode = '\u2086'; break;
                case '7': unicode = '\u2087'; break;
                case '8': unicode = '\u2088'; break;
                case '9': unicode = '\u2089'; break;
                case '+': unicode = '\u208A'; break;
                case '-': unicode = '\u208B'; break;
                case '=': unicode = '\u208C'; break;
                case '(': unicode = '\u208D'; break;
                case ')': unicode = '\u208E'; break;
                case 'a': unicode = '\u2090'; break;
                case 'e': unicode = '\u2091'; break;
                case 'o': unicode = '\u2092'; break;
                case 'x': unicode = '\u2093'; break;
                case 'h': unicode = '\u2095'; break;
                case 'k': unicode = '\u2096'; break;
                case 'l': unicode = '\u2097'; break;
                case 'm': unicode = '\u2098'; break;
                case 'n': unicode = '\u2099'; break;
                case 'p': unicode = '\u209A'; break;
                case 's': unicode = '\u209B'; break;
                case 't': unicode = '\u209C'; break;

            }
            return unicode;
        }


        public string TrimString(string input_string)
        {
            // removes beginning or trailing carriage returns, tabs and spaces
            if (input_string == null)
            {
                return null;
            }
            else
            {
                return input_string.Trim('\r', '\n', '\t', ' ');
            }
        }


        public string RegulariseStringEndings(string input_string)
        {
            // removes beginning or trailing carriage returns, tabs and spaces
            if (input_string == null)
            {
                return null;
            }
            else
            {
                string output_string = input_string.Replace("\r\n", "|@@|");
                output_string = output_string.Replace("\r", "\n");
                return output_string.Replace("|@@|", "\r\n");
            }
        }


        public string StringClean(string input_string)
        {
            string output_string = ReplaceApos(input_string);
            output_string = ReplaceTags(output_string);
            output_string = TrimString(output_string);
            return RegulariseStringEndings(output_string);
        }


        public bool AppearsGenuineTitle(string in_title)
        {
            if (string.IsNullOrEmpty(in_title))
            {
                return false;
            }
            else
            {
                bool result = true;
                string lower_title = in_title.ToLower().Trim();

                if (lower_title == "n.a." || lower_title == "na" 
                        || lower_title == "n.a" || lower_title == "n/a")
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
                else if ( lower_title == "none" || lower_title == "not done" 
                        || lower_title == "same as above" || lower_title == "in preparation" 
                        || lower_title == "non fornito")
                {
                    result = false;
                }

                // out_title = ReplaceApos(in_title);
                return result;
            }
        }


        public string TidyOrgName(string in_name, string sid)
        {
            string name = in_name;
            if (name != null)
            {
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

                name = ReplaceApos(name);

                // Trim any odd' characters

                name = name.Trim(',', '-', '*', ';', ' ');

                // try and deal with possible ambiguities (organisations with genuinely the same name)

                string nlower = name.ToLower();
                if (nlower.Contains("newcastle") && nlower.Contains("university")
                    && !nlower.Contains("hospital"))
                {
                    if (nlower.Contains("nsw") || nlower.Contains("australia"))
                    {
                        name = "University of Newcastle (Australia)";
                    }
                    else if (nlower.Contains("uk") || nlower.Contains("tyne"))
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

                if (nlower.Contains("china medical") && nlower.Contains("university"))
                {
                    if (nlower.Contains("taiwan") || nlower.Contains("taichung"))
                    {
                        name = "China Medical University, Taiwan";
                    }
                    else if (nlower.Contains("Shenyang") || nlower.Contains("prc"))
                    {
                        name = "China Medical University";
                    }
                    else if (sid.StartsWith("Chi"))
                    {
                        name = "China Medical University";
                    }
                }

                if (nlower.Contains("national") && nlower.Contains("cancer center"))
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
            }

            return name;
        }


        public bool AppearsGenuineOrgName(string org_name)
        {
            if (org_name == null)
            {
                return false;
            }
            else
            {            
                bool result = true;
                string in_name = org_name.ToLower();

                if (in_name.Length < 3 )
                {
                    result = false;
                }
                else if (in_name == "n.a." || in_name == "n a" || in_name == "n/a" ||
                   in_name == "nil" || in_name == "nill" || in_name == "non")
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
                else if (in_name.StartsWith("investigator ") || in_name == "investigator" || in_name == "self"
                    || in_name.StartsWith("Organisation name "))
                {
                    result = false;
                }
                else if (in_name.Contains("thesis") || in_name.Contains(" none."))
                {
                    result = false;
                }
                else if (in_name.StartsWith("professor") || in_name.StartsWith("prof ")
                    || in_name.StartsWith("prof. ") || in_name.StartsWith("associate prof"))
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
        }


        public string ExtractOrganisation(string affiliation, string sid)
        {
            if (affiliation == null)
            {
                return null;
            }

            string affil_organisation = "";
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


        public string FindSubPhrase(string phrase, string target)
        {
            string phrase1 = phrase.Replace("&#44;", ",");
            string p = phrase1.ToLower();

            // ignore trailing commas after some states names
            p = p.Replace("california,", "california*");
            p = p.Replace("wisconsin,", "wisconsin*");

            int startpos = p.IndexOf(target);
            int commapos1 = p.LastIndexOf(",", startpos);  // if -1 becomes 0 
            int commapos2 = p.IndexOf(",", startpos + target.Length - 1);
            if (commapos2 == -1)
            {
                commapos2 = p.Length;
            }

            string org_name = phrase1.Substring(commapos1 + 1, commapos2 - commapos1 - 1).Trim();
            if (org_name.ToLower().StartsWith("the "))
            {
                org_name = org_name.Substring(4);
            }
            return org_name;
        }


        public string TidyORCIDId(string input_identifier)
        {
            string identifier = input_identifier.Replace("https://orcid.org/", "");
            identifier = identifier.Replace("http://orcid.org/", "");
            identifier = identifier.Replace("/", "-");
            identifier = identifier.Replace(" ", "-");
            return identifier;
        }


        public string TidyORCIDId2(string input_identifier)
        {
            string identifier = input_identifier;
            if (identifier.Length == 16)
            {
                identifier = identifier.Substring(0, 4) + "-" + identifier.Substring(4, 4) +
                            "-" + identifier.Substring(8, 4) + "-" + identifier.Substring(12, 4);
            }
            if (identifier.Length == 15) identifier = "0000" + identifier;
            if (identifier.Length == 14) identifier = "0000-" + identifier;

            return identifier;
        }


        public string lang_3_to_2(string input_lang_code)
        {
            string lang_code = "";
            switch (input_lang_code)
            {
                // covers most of the non English cases

                case "fre": lang_code = "fr"; break;
                case "ger": lang_code = "de"; break;
                case "spa": lang_code = "es"; break;
                case "ita": lang_code = "it"; break;
                case "por": lang_code = "pt"; break;
                case "rus": lang_code = "ru"; break;
                case "tur": lang_code = "tr"; break;
                case "hun": lang_code = "hu"; break;
                case "pol": lang_code = "pl"; break;
                case "swe": lang_code = "sv"; break;
                case "nor": lang_code = "no"; break;
                case "dan": lang_code = "da"; break;
                case "fin": lang_code = "fi"; break;
                default: lang_code = "??"; break;
            }
            return lang_code;
        }
    }
} 