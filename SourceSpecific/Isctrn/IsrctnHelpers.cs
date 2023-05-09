using System.Text.RegularExpressions;
namespace MDR_Harvester.Isrctn;

internal class IsrctnHelpers
{
    internal List<IsrctnIdentifierDetails> GetISRCTNIdentifierProps(string id_value, 
                                                                    string? study_sponsor, int in_uk_only)
    {
        // Often ids are give with type unknown. The format of the id can be examined to
        // see if the real type can be identified. 
        // First set of tests effectively discards useless or invalid ids. It returns an empty list.
        
        // Does the identifier contain two Dutch registry ids? (About 100 values do)
        // If so turn them into an array of values. If not create an array with a single value.

        string[]? poss_ids;
        
        if (id_value.Contains("NTR") && id_value.Contains("NL"))
        {
            // good chance it is both the dutch ids, separated by a comma or brackets

            id_value = id_value.Replace("(", ",");
            id_value = id_value.Replace(")", string.Empty);
            poss_ids = id_value.Split(',', StringSplitOptions.TrimEntries);
        }
        else if (id_value.Contains(','))
        {
            // if an 'ordinary' comma joined string split it anyway
            
            poss_ids = id_value.Split(',', StringSplitOptions.TrimEntries);
        }
        else
        {
            poss_ids = new[] { id_value };
        }
        
        // Establish a list of identifiers to receive the processed id information and examine
        // each id - usually only one
        
        List<IsrctnIdentifierDetails> identifiers = new();

        foreach (string s in poss_ids)
        {
            string id_low = s.Trim().ToLower();
            bool usable = true; // by default 

            if (s.Length < 3)
            {
                usable = false; // too small 
            }
            else if (s.Length <= 4 && Regex.Match(s, @"^(\d{1}\.\d{1}|\d{1}\.\d{2})$").Success)
            {
                usable = false; // probably just a protocol version number
            }
            else if (Regex.Match(id_low, @"^version ?([0-9]|[0-9]\.[0-9]|[0-9]\.[0-9][0-9])").Success)
            {
                usable = false; // probably just a protocol version number
            }
            else if (Regex.Match(id_low, @"^v ?([0-9]|[0-9]\.[0-9]|[0-9]\.[0-9][0-9])").Success)
            {
                usable = false; // probably just a protocol version number
            }
            else if (Regex.Match(s, @"^0+$").Success)
            {
                usable = false; // all zeroes!
            }
            else if (Regex.Match(id_value, @"^0+").Success)
            {
                string val2 = s.TrimStart('0');
                if (s.Length <= 4 && Regex.Match(val2, @"^(\d{1}|\d{1}\.\d{1}|\d{1}\.\d{2})$").Success)
                {
                    usable = false; // starts with zeroes, then a few numbers
                }
            }
            else if (Regex.Match(id_low, @"^http://").Success || Regex.Match(id_low, @"^https://").Success)
            {
                usable = false; // urls rather than identifiers
            }
            
            // Onl;y proceed if id is potentially 'usable'. The second set uses possible key words / acronyms
            // in the value to distinguish particular id types and organisational sources. 
            // An id details object is created. If no match then by the id is interpreted by default as a
            // sponsor serial number, which is how it is described in the source. 

            if (usable)
            {
                bool determined = false;

                // is it a Dutch registry id? 
                
                if (Regex.Match(s, @"^NL\d{2,4}$").Success)
                {
                    identifiers.Add(new IsrctnIdentifierDetails(11, "Trial Registry ID", 100132,
                        "The Netherlands National Trial Register", s));
                    determined = true;
                }

                // is it an obsolete type Dutch registry id? 
                
                if (Regex.Match(s, @"^NTR\d{2,4}$").Success)
                {
                    identifiers.Add(new IsrctnIdentifierDetails(45, "Obsolete NTR number", 100132,
                        "The Netherlands National Trial Register", s));
                    determined = true;
                }
                
                // An Australian CTR number?

                if (Regex.Match(s, @"^ACTRN\d{14}$").Success)
                {
                    identifiers.Add(new IsrctnIdentifierDetails(11, "Trial Registry ID", 100116,
                        "Australian / New Zealand Clinical Trials Registry", s));
                    determined = true;
                }
                
                // A German CTR number?

                if (Regex.Match(s, @"^DRKS\d{8}$").Success)
                {
                    identifiers.Add(new IsrctnIdentifierDetails(11, "Trial Registry ID", 100124,
                        "Deutschen Register Klinischer Studien", s));
                    determined = true;
                }
                
                // a Eudract number?
                
                if (Regex.Match(s, @"[0-9]{4}-[0-9]{6}-[0-9]{2}").Success)
                {
                    string id = Regex.Match(s, @"[0-9]{4}-[0-9]{6}-[0-9]{2}").Value;
                    identifiers.Add(new IsrctnIdentifierDetails(11, "Trial Registry ID", 100123,
                        "EU Clinical Trials Register", id));
                    determined = true;
                }

                // An IRAS reference?

                if (id_low.Contains("iras") || id_low.Contains("hra"))
                {
                    if (Regex.Match(id_low, @"([0-9]{6})").Success) 
                    {
                        string id = Regex.Match(id_low, @"[0-9]{6}").Value;
                        identifiers.Add(new IsrctnIdentifierDetails(41, "Regulatory Body ID", 101409,
                            "Health Research Authority", id));
                        determined = true;
                    }
                    else if (Regex.Match(id_low, @"([0-9]{5})").Success)
                    {
                        // uk IRAS number
                        string id = Regex.Match(id_low, @"[0-9]{5}").Value;
                        identifiers.Add(new IsrctnIdentifierDetails(41, "Regulatory Body ID", 101409,
                            "Health Research Authority", id));
                        determined = true;
                    }
                }
                
                // A CPMS reference?
                
                if (id_low.Contains("cpms") && Regex.Match(id_low, @"[0-9]{5}").Success)
                {
                    string id = Regex.Match(id_value, @"[0-9]{5}").Value;
                    identifiers.Add(new IsrctnIdentifierDetails(41, "Regulatory Body ID", 102002,
                        "Central Portfolio Management System", id));
                    determined = true;
                }

                // if simply 5 digits and a study entirely run in the Uk
                // Almost certainly a CPMS number as well
                
                if (Regex.Match(s, @"^\d{5}$").Success && in_uk_only == 2)
                {
                    identifiers.Add(new IsrctnIdentifierDetails(41, "Regulatory Body ID", 102002,
                        "Central Portfolio Management System", s));
                    determined = true;
                }
                
                // An HTA reference?
                
                if (id_low.Contains("hta") && Regex.Match(id_value, @"\d{2}/(\d{3}|\d{2})/\d{2}").Success)
                {
                    string id = Regex.Match(id_value, @"\d{2}/(\d{3}|\d{2})/\d{2}").Value;
                    identifiers.Add(new IsrctnIdentifierDetails(13, "Funder Id", 102003,
                        "Health Technology Assessment programme", id));
                    determined = true;
                }
                
                // CCMO number?
               
                if (s.StartsWith("CCMO", StringComparison.OrdinalIgnoreCase))
                {
                    string s1 = s.Replace("CCMO", "").Trim();
                    s1 = s1.Replace("nr", "", StringComparison.OrdinalIgnoreCase);
                    s1 = s1.Replace("form", "", StringComparison.OrdinalIgnoreCase);
                    s1 = s1.Trim(' ', ':', '-', '#');
                    identifiers.Add(new IsrctnIdentifierDetails(41, "Regulatory Body ID", 109113, "CCMO", s1));
                    determined = true;
                }
                
                if (s.StartsWith("ABR", StringComparison.OrdinalIgnoreCase))
                {
                    string s1 = s.Replace("ABR", "").Trim();
                    s1 = s1.Replace("nr", "", StringComparison.OrdinalIgnoreCase);
                    s1 = s1.Replace("form", "", StringComparison.OrdinalIgnoreCase);
                    s1 = s1.Trim(' ', ':', '-', '#');
                    identifiers.Add(new IsrctnIdentifierDetails(41, "Regulatory Body ID", 109113, "CCMO", s1));
                    determined = true;
                }
                
                // ZonMw number?
                
                if (id_low.Contains("zonmw", StringComparison.OrdinalIgnoreCase))
                {
                    string s1 = s.Replace("ZonMw", "", StringComparison.OrdinalIgnoreCase).Trim();
                    s1 = s1.Replace("nr", "", StringComparison.OrdinalIgnoreCase);
                    s1 = s1.Replace("number", "", StringComparison.OrdinalIgnoreCase);
                    s1 = s1.Replace("no.", "", StringComparison.OrdinalIgnoreCase);
                    s1 = s1.Replace("no", "", StringComparison.OrdinalIgnoreCase);
                    s1 = s1.Replace("reference", "", StringComparison.OrdinalIgnoreCase);
                    s1 = s1.Replace("file number", "", StringComparison.OrdinalIgnoreCase);
                    s1 = s1.Trim(' ', ':', '-', '#');
                    identifiers.Add(new IsrctnIdentifierDetails(13, "Funder’s ID", 109113, "ZonMw", s1));
                    determined = true;
                }
                
                // MRC G number?
                
                if (Regex.Match(s, @"^G\d{7}$").Success)
                {
                    identifiers.Add(new IsrctnIdentifierDetails(13, "Funder’s ID", 100456,
                        "Medical Research Council", s));
                    determined = true;
                }

                
                // NHS / NIHR support funding grant?
                
                if (s.Contains("NIHR", StringComparison.OrdinalIgnoreCase))
                {
                    string s1 = s.Replace("Grant Codes", "", StringComparison.OrdinalIgnoreCase).Trim();
                    s1 = s1.Replace("grant", "", StringComparison.OrdinalIgnoreCase);
                    s1 = s1.Replace("Award", "", StringComparison.OrdinalIgnoreCase);
                    s1 = s1.Replace("id", "", StringComparison.OrdinalIgnoreCase);
                    s1 = s1.Replace("HTA", "", StringComparison.OrdinalIgnoreCase);
                    s1 = s1.Replace("PHR", "", StringComparison.OrdinalIgnoreCase);
                    s1 = s1.Trim(' ', ':', '-', '(', ')');
                    identifiers.Add(new IsrctnIdentifierDetails(13, "Funder’s ID", 100442, 
                                        "National Institute for Health Research", s1));
                    determined = true;
                }
                
                // EORTC number?
                
                if (s.StartsWith("EORTC", StringComparison.OrdinalIgnoreCase))
                {
                    string s1 = s.Replace("EORTC", "").Trim();
                    s1 = s1.Replace("protocol", "", StringComparison.OrdinalIgnoreCase);
                    s1 = s1.Trim(' ', ':', '-', '#');

                    if (study_sponsor is not null && 
                        study_sponsor.Contains("EORTC", StringComparison.OrdinalIgnoreCase))
                    {
                        identifiers.Add(new IsrctnIdentifierDetails(14, "Sponsor ID", 100010, "EORTC", s));
                    }
                    else
                    {
                        identifiers.Add(new IsrctnIdentifierDetails(50, "Research Collaboration ID", 
                                                                    100010, "EORTC", s1));
                    }
                    determined = true;
                }

                // when all else fails - use the default sponsor's protocol number as the best remaining approximation

                if (!determined)
                {
                    identifiers.Add(new IsrctnIdentifierDetails(14, "Sponsor ID", null, study_sponsor, s));
                }
            }
        }

        return identifiers;
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
