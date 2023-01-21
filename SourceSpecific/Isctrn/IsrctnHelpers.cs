using System.Text.RegularExpressions;
using MDR_Harvester.Extensions;
using Microsoft.VisualBasic;


namespace MDR_Harvester.Isctrn;

internal class IsrctnHelpers
{
    internal IsrctnIdentifierDetails GetISRCTNIdentifierProps(string id_value, string study_sponsor)
    {
        // Use given values to create an id details object, by default as a 
        // sponsor serial number, which is how it is presented in the source. 
        // First set of tests effctively discards useless or invalid ids.
        // Second set uses possible key words / acronyms in the provided value
        // to distinguish particular id types and organisational sources.

        IsrctnIdentifierDetails idd = new(14, "Sponsor ID", null, study_sponsor, id_value);
        string id_val = id_value.Trim().ToLower();

        if (id_val.Length < 3)
        {
            idd.id_type = "Not usable"; // too small 
        }
        else if (id_val.Length <= 4 && Regex.Match(id_value, @"^(\d{1}\.\d{1}|\d{1}\.\d{2})$").Success)
        {
            idd.id_type = "Not usable"; // probably just a protocol version number
        }
        else if (Regex.Match(id_val, @"^version ?([0-9]|[0-9]\.[0-9]|[0-9]\.[0-9][0-9])").Success)
        {
            idd.id_type = "Not usable"; // probably just a protocol version number
        }
        else if (Regex.Match(id_val, @"^v ?([0-9]|[0-9]\.[0-9]|[0-9]\.[0-9][0-9])").Success)
        {
            idd.id_type = "Not usable"; // probably just a protocol version number
        }
        else if (Regex.Match(id_value, @"^0+$").Success)
        {
            idd.id_type = "Not usable"; // all zeroes!
        }
        else if (Regex.Match(id_value, @"^0+").Success)
        {
            string val2 = id_value.TrimStart('0');
            if (val2.Length <= 4 && Regex.Match(val2, @"^(\d{1}|\d{1}\.\d{1}|\d{1}\.\d{2})$").Success)
            {
                idd.id_type = "Not usable"; // starts with zeroes, then a few numbers
            }
        }


        // is it a Dutch registry id?
        if (Regex.Match(id_value, @"^NTR\d{2}").Success)
        {
            idd.id_org_id = 100132;
            idd.id_org = "The Netherlands National Trial Register";
            idd.id_type_id = 11;
            idd.id_type = "Trial Registry ID";

            // can be a 4, 3 or 2 digit number.

            if (Regex.Match(id_value, @"^NTR\d{4}$").Success)
            {
                idd.id_value = Regex.Match(id_value, @"^NTR\d{4}$").Value;
            }
            else if (Regex.Match(id_value, @"^NTR\d{3}$").Success)
            {
                idd.id_value = Regex.Match(id_value, @"^NTR\d{3}$").Value;
            }
            else if (Regex.Match(id_value, @"^NTR\d{2}$").Success)
            {
                idd.id_value = Regex.Match(id_value, @"^NTR\d{2}$").Value;
            }
        }

        // a Eudract number?
        if (Regex.Match(id_value, @"[0-9]{4}-[0-9]{6}-[0-9]{2}").Success)
        {
            idd.id_org_id = 100123;
            idd.id_org = "EU Clinical Trials Register";
            idd.id_type_id = 11;
            idd.id_type = "Trial Registry ID";
            idd.id_value = Regex.Match(id_value, @"[0-9]{4}-[0-9]{6}-[0-9]{2}").Value;
        }

        // An IRAS reference?
        if (id_val.Contains("iras") || id_value.Contains("hra"))
        {
            if (Regex.Match(id_value, @"([0-9]{6})").Success)
            {
                // uk IRAS number
                idd.id_org_id = 101409;
                idd.id_org = "Health Research Authority";
                idd.id_type_id = 41;
                idd.id_type = "Regulatory Body ID";
                idd.id_value = Regex.Match(id_value, @"[0-9]{6}").Value;
            }
            else if (Regex.Match(id_value, @"([0-9]{5})").Success)
            {
                // uk IRAS number
                idd.id_org_id = 101409;
                idd.id_org = "Health Research Authority";
                idd.id_type_id = 41;
                idd.id_type = "Regulatory Body ID";
                idd.id_value = Regex.Match(id_value, @"[0-9]{5}").Value;
            }
            else
            {
                idd.id_type = "Not usable";
            }
        }

        // A CPMS reference?
        if (id_val.Contains("cpms") && Regex.Match(id_value, @"[0-9]{5}").Success)
        {
            // uk CPMS number
            idd.id_org_id = 102002;
            idd.id_org = "Central Portfolio Management System";
            idd.id_type_id = 13;
            idd.id_type = "Funder Id";
            idd.id_value = Regex.Match(id_value, @"[0-9]{5}").Value;
        }


        // An HTA reference?
        if (id_val.Contains("hta") && Regex.Match(id_value, @"\d{2}/(\d{3}|\d{2})/\d{2}").Success)
        {
            // uk hta number
            idd.id_org_id = 102003;
            idd.id_org = "Health Technology Assessment programme";
            idd.id_type_id = 13;
            idd.id_type = "Funder Id";
            idd.id_value = Regex.Match(id_value, @"\d{2}/(\d{3}|\d{2})/\d{2}").Value;
        }

        return idd;
    }

    public string FindPossibleSeparator(string inputString)
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
                // Standard in ISRCTN is to use 'N. ' for numbering IEC,
                // with N.n. and N.n.n for sub-headings, occasionally
                // N.a., N.b. etc. It seems to be pretty consistent
                // Use Regex to see if the line begins with that pattern.

                // First do a 'pre-run' to put any sub-headings and sub-sub
                // headings in properly and remove null lines, after a few 
                // basic attempts at tidying up.

                Regex ressh = new Regex(@"^\d{1,2}\.\d{1,2}\.\d{1,2}\.");
                Regex resh = new Regex(@"^\d{1,2}\.\d{1,2}\.");
                Regex resh1 = new Regex(@"^\d{1,2}\.\d{1,2} ");
                Regex reha = new Regex(@"^[a-z]{1}\.");
                Regex rehab = new Regex(@"^[a-z]{1}\)");
                Regex renha = new Regex(@"^\d{1,2}[a-z]{1} ");
                Regex recrit = new Regex(@"^\d{1,2}\. ");

                int level = 0;
                int old_level = 0;
                int[] level_nums = { 1, 1, 1, 1, 1, 1};    // 5 levels!
                int current_level_num = 1;
                for (int i = 0; i < lines.Length; i++)
                {
                    string? this_line = lines[i].TrimPlus()!;
                    if (!string.IsNullOrEmpty(this_line)
                        && !this_line.Contains(new string('_', 4)))
                    {
                        this_line = this_line.Replace("..", ".");
                        this_line = this_line.Replace(",.", ".");
                        this_line = this_line.Replace("\n\n", "\n");

                        if (ressh.IsMatch(this_line)) // Numeric Sub-sub-heading.
                        {
                            level = 3;
                            level_nums[3] = level == old_level ? level_nums[3] + 1 : 1;
                            var leader = Regex.Match(this_line, ressh.ToString()).Value;
                            var clipped_line = Regex.Replace(this_line, ressh.ToString(), string.Empty).Trim();
                            cr.Add(new Criterion(i + 1, leader, level, level_nums[3], single_crit, single_type,
                                clipped_line));
                        }
                        else if (resh.IsMatch(this_line)) // Numeric Sub-heading.
                        {
                            level = 2;
                            level_nums[2] = level != old_level && old_level == 1 ? 1 : level_nums[2] + 1;
                            var leader = Regex.Match(this_line, resh.ToString()).Value;
                            var clipped_line = Regex.Replace(this_line, resh.ToString(), string.Empty).Trim();
                            cr.Add(new Criterion(i + 1, leader, level, level_nums[2], single_crit, single_type,
                                clipped_line));
                        }
                        else if (resh1.IsMatch(this_line)) // Numeric Sub-heading (without final period).
                        {
                            level = 2;
                            level_nums[2] = level != old_level && old_level == 1 ? 1 : level_nums[2] + 1;
                            var leader = Regex.Match(this_line, resh1.ToString()).Value;
                            var clipped_line = Regex.Replace(this_line, resh1.ToString(), string.Empty).Trim();
                            cr.Add(new Criterion(i + 1, leader, level, level_nums[2], single_crit, single_type,
                                clipped_line));
                        }
                        else if (renha.IsMatch(this_line)) // Number plus letter -
                                                           // e.g. '4a ', '5b '  etc (without final period).
                        {
                            level = 2;
                            level_nums[2] = level != old_level && old_level == 1 ? 1 : level_nums[2] + 1;
                            var leader = Regex.Match(this_line, renha.ToString()).Value;
                            var clipped_line = Regex.Replace(this_line, renha.ToString(), string.Empty).Trim();
                            cr.Add(new Criterion(i + 1, leader, level, level_nums[2], single_crit, single_type,
                                clipped_line));
                        }
                        else if (reha.IsMatch(this_line)) // Alpha heading.
                        {
                            // Can occur at any level, therefore increases it by 1 if the first one.

                            var leader = Regex.Match(this_line, reha.ToString()).Value;
                            level = leader is "a." or "A." ? level + 1 : level;
                            level_nums[level] = leader is "a." or "A." ? 1 : level_nums[level] + 1;
                            var clipped_line = Regex.Replace(this_line, reha.ToString(), string.Empty).Trim();
                            cr.Add(new Criterion(i + 1, leader, level, level_nums[level], single_crit, single_type,
                                clipped_line));
                        }
                        else if (rehab.IsMatch(this_line)) // Alpha heading.
                        {
                            // Can occur at any level, therefore increases it by 1 if the first one.

                            var leader = Regex.Match(this_line, rehab.ToString()).Value;
                            level = leader is "a)" or "A)" ? level + 1 : level;
                            level_nums[level] = leader is "a)" or "A)" ? 1 : level_nums[level] + 1;
                            var clipped_line = Regex.Replace(this_line, rehab.ToString(), string.Empty).Trim();
                            cr.Add(new Criterion(i + 1, leader, level, level_nums[level], single_crit, single_type,
                                clipped_line));
                        }
                        else
                        {
                            // Correct the odd occasion when there is no space after
                            // the first 1 (subheadings already dealt with above).

                            if (this_line.StartsWith("1.") && !this_line.StartsWith("1. "))
                            {
                                this_line = "1. " + this_line[2..];
                            }

                            if (recrit.IsMatch(this_line)) //   'standard' criterion heading
                            {
                                level = 1;
                                level_nums[1] = level != old_level && old_level == 0 ? 1 : level_nums[1] + 1;
                                var leader = Regex.Match(this_line, recrit.ToString()).Value;
                                var clipped_line = Regex.Replace(this_line, recrit.ToString(), string.Empty).Trim();
                                cr.Add(new Criterion(i + 1, leader, level, level_nums[1], single_crit, single_type,
                                    clipped_line));
                            }
                            else
                            {
                                // Does not appear to have any normal criteria or component
                                // starting point - may be an internal break
                                // level stays the same. Type depends on position.
                                level_nums[level]++;
                                if (i == lines.Length - 1)
                                {
                                    cr.Add(new Criterion(i + 1, "Spp", level, level_nums[level], post_crit,
                                        post_crit_type, this_line));
                                }
                                else
                                {
                                    cr.Add(new Criterion(i + 1, "Hdr", level, level_nums[level], grp_hdr, grp_hdr_type,
                                        this_line));
                                }
                            }
                        }
                    }

                    old_level = level;
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

                if (sid == "ISRCTN11273035")
                {
                    //break
                }
                
                for (int i = cr.Count - 1 ; i >= 0; i--)
                {
                    bool transfer_crit = true;
                    if (cr[i].CritTypeId == grp_hdr
                        && i < cr.Count - 1 && i > 0
                        && !cr[i].CritText.EndsWith(':'))
                    {
                        // Does the following leader end with 1.,
                        // as would be the case with a genuine header line
                        // (N.B. Initial cr[0] is not checked), nor is 
                        // the last cr entry.

                        if (!cr[i + 1].Leader.EndsWith("1."))
                        {
                            // Almost certainly a spurious \n in the
                            // original string rather than a genuine hder.

                            cr[i - 1].CritText += " " + cr[i].CritText;
                            cr[i - 1].CritText = cr[i - 1].CritText?.Replace("  ", " ");
                            transfer_crit = false;
                        }
                    }

                    if (cr[i].CritTypeId == post_crit && !cr[i].CritText.EndsWith(':')
                                                      && !cr[i].CritText.StartsWith('*')
                                                      && !cr[i].CritText.ToLower().StartsWith("note"))
                    {
                        // Is this a very small supplements text?
                        // Likely to be a spurious \n that has caused it.

                        if (cr[i].CritText.Length < 20 )
                        {
                            // Almost certainly a spurious \n in the
                            // original string rather than a genuine hder.

                            cr[i - 1].CritText += " " + cr[i].CritText;
                            cr[i - 1].CritText = cr[i - 1].CritText?.Replace("  ", " ");
                            transfer_crit = false;
                        }

                        if (cr[i].CritText.StartsWith("Target Gender:"))
                        {
                            // clearly a criterion rather than a supplementary statement
                            cr[i].CritTypeId = single_crit;
                            cr[i].CritType = single_type;
                            cr[i].Leader = cr[i-1].Leader + "*";
                        }

                    }
                    
                    
                    

                    if (transfer_crit)
                    {
                        cr2.Add(cr[i]);
                    }
                }
                
                return cr2.OrderBy( c=> c.SeqNum).ToList();
            }
        }
    }

    public void StoreSubCritLine(Regex regPattern, List<string> crits, string this_line, string prefix, int j)
    {
        var clipped_line = Regex.Replace(this_line, regPattern.ToString(), string.Empty).Trim();
        string revised_line = prefix + clipped_line;
        var heading = Regex.Match(this_line, regPattern.ToString()).Value;
        if (int.TryParse(heading[..(heading.IndexOf('.'))], out int current_num))
        {
            if (j == current_num - 1)
            {
                crits[j] += revised_line;
            }
            else
            {
                crits.Add(clipped_line);;
                j++;
            }
        }
        else
        {
            if (j != -1)
            {
                crits[j] += revised_line;
            }
            else
            {
                crits.Add(clipped_line);;
                j++;
            }
        }
        
    }
}


internal class IsrctnIdentifierDetails
{
    public int? id_type_id { get; set; }
    public string? id_type { get; set; }
    public int? id_org_id { get; set; }
    public string? id_org { get; set; }
    public string? id_value { get; set; }

    internal IsrctnIdentifierDetails(int? _id_type_id, string? _id_type, int? _id_org_id, string? _id_org, string? _id_value)
    {
        id_type_id = _id_type_id;
        id_type = _id_type;
        id_org_id = _id_org_id;
        id_org = _id_org;
        id_value = _id_value;
    }
}


internal static class IsrctnExtensions
{

    internal static bool IsNewToList(this string? ident_value, List<StudyIdentifier> identifiers)
    {
        if (string.IsNullOrEmpty(ident_value))
        {
            return false;
        }
        else
        {
            bool res = true;
            if (identifiers.Count > 0)
            {
                foreach (StudyIdentifier i in identifiers)
                {
                    if (ident_value == i.identifier_value)
                    {
                        res = false;
                        break;
                    }
                }
            }
            return res;
        }
    }


    internal static bool IsAnIndividual(this string? orgname)
    {
        if (string.IsNullOrEmpty(orgname))
        {
            return false;
        }
        else
        {
            bool is_individual = false;

            // If looks like an individual's name...

            if (orgname.EndsWith(" md") || orgname.EndsWith(" phd") ||
                orgname.Contains(" md,") || orgname.Contains(" md ") ||
                orgname.Contains(" phd,") || orgname.Contains(" phd ") ||
                orgname.Contains("dr ") || orgname.Contains("dr.") ||
                orgname.Contains("prof ") || orgname.Contains("prof.") ||
                orgname.Contains("professor"))
            {
                is_individual = true;

                // Unless part of a organisation reference...

                if (orgname.Contains("hosp") || orgname.Contains("univer") ||
                    orgname.Contains("labor") || orgname.Contains("labat") ||
                    orgname.Contains("institu") || orgname.Contains("istitu") ||
                    orgname.Contains("school") || orgname.Contains("founda") ||
                    orgname.Contains("associat"))
                {
                    is_individual = false;
                }
            }

            // some specific individuals...
            if (orgname == "seung-jung park" || orgname == "kang yan")
            {
                is_individual = true;
            }
            return is_individual;
        }
    }


    internal static bool IsAnOrganisation(this string? fullname)
    {
        if (string.IsNullOrEmpty(fullname))
        {
            return false;
        }
        else
        {
            bool is_org = false;
            string fname = fullname.ToLower();
            if (fname.Contains(" group") || fname.StartsWith("group") ||
                fname.Contains(" assoc") || fname.Contains(" team") ||
                fname.Contains("collab") || fname.Contains("network"))
            {
                is_org = true;
            }

            return is_org;
        }
    }


    internal static int CheckObjectName(this string? object_display_title, List<ObjectTitle> titles)
    {
        if (string.IsNullOrEmpty(object_display_title))
        {
            return 0;
        }
        else
        {
            int num_of_this_type = 0;
            if (titles.Count > 0)
            {
                for (int j = 0; j < titles.Count; j++)
                {
                    if (titles[j].title_text?.Contains(object_display_title) == true)
                    {
                        num_of_this_type++;
                    }
                }
            }
            return num_of_this_type;
        }
    }

}
