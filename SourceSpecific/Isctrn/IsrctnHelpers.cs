using System.Text.RegularExpressions;

namespace MDR_Harvester.Isrctn;

internal class IsrctnHelpers
{
    internal IsrctnIdentifierDetails GetISRCTNIdentifierProps(string id_value, string study_sponsor)
    {
        // Use given values to create an id details object, by default as a 
        // sponsor serial number, which is how it is presented in the source. 
        // First set of tests effectively discards useless or invalid ids.
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


    internal static bool IsAnIndividual(this string? orgname)
    {
        if (string.IsNullOrEmpty(orgname))
        {
            return false;
        }
        
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


    internal static bool IsAnOrganisation(this string? fullname)
    {
        if (string.IsNullOrEmpty(fullname))
        {
            return false;
        }

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


    internal static int CheckObjectName(this string? object_display_title, List<ObjectTitle> titles)
    {
        if (string.IsNullOrEmpty(object_display_title))
        {
            return 0;
        }
        
        int num_of_this_type = 0;
        if (titles.Count > 0)
        {
            for (int j = 0; j < titles.Count; j++)
            {
                if (titles[j].title_text?.Contains(object_display_title) is true)
                {
                    num_of_this_type++;
                }
            }
        }
        return num_of_this_type;
    }
}
