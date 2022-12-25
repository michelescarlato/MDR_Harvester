using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MDR_Harvester
{
    public class IdentifierHelpers
    {
        // Two check routines that scan previously extracted Identifiers or Dates, to 
        // indicate if the input Id / Date type has already beenm extracted.

        public bool IdNotPresent(List<ObjectIdentifier> ids, int id_type, string id_value)
        {
            bool to_add = true;
            if (ids.Count > 0)
            {
                foreach (ObjectIdentifier id in ids)
                {
                    if (id.identifier_type_id == id_type && id.identifier_value == id_value)
                    {
                        to_add = false;
                        break;
                    }
                }
            }
            return to_add;
        }

        public bool DateNotPresent(List<ObjectDate> dates, int datetype_id, int? year, int? month, int? day)
        {
            bool to_add = true;
            if (dates.Count > 0)
            {
                foreach (ObjectDate d in dates)
                {
                    if (d.date_type_id == datetype_id
                        && d.start_year == year && d.start_month == month && d.start_day == day)
                    {
                        to_add = false;
                        break;
                    }
                }
            }
            return to_add;
        }
        

        // A helper function called from the loop that goes through the CTG secondary Id data
        // It tries to make the data as complete as possible, depending on the typem of 
        // secondary id that is being processed.

        public IdentifierDetails GetIdentifierProps(string? id_type, string? id_org, string id_value)
        {
            // use initial values
            // to create id details object

            IdentifierDetails id = new IdentifierDetails(id_type, id_org, id_value); 
            
            if (id_org == null || id_org == "Other" || id_org == "Alias Study Number")
            {
                id.id_org_id = 12;
                id.id_org = "No organisation name provided in source data";
            }

            if (id_type == null)
            {
                id.id_type_id = 1;
                id.id_type = "No type given in source data";
            }
            
            else if (id_type == "Other Identifier")
            {
                id.id_type_id = 90;
                id.id_type = "Other";
            }

            else if (id_type == "U.S. NIH Grant/Contract")
            {
                id.id_org_id = 100134;
                id.id_org = "National Institutes of Health";
                id.id_type_id = 13;
                id.id_type = "Funder ID";
            }

            else if (id_type == "Other Grant/Funding Number")
            {
                id.id_type_id = 13;
                id.id_type = "Funder ID";
            }

            else if (id_type == "EudraCT Number")
            {
                id.id_org_id = 100123;
                id.id_org = "EU Clinical Trials Register";
                id.id_type_id = 11;
                id.id_type = "Trial Registry ID";
            }

            else if (id_type == "Registry Identifier")
            {
                id.id_type_id = 11;
                id.id_type = "Trial Registry ID";

                string idorg = id_org.ToLower();

                if (idorg.Contains("who") || idorg.Contains("utn") 
                    || idorg.Contains("ictrp") || idorg.Contains("universal"))
                {
                    // UTN number - check for ictrp before checking ctrp
                    id.id_org_id = 100115;
                    id.id_org = "International Clinical Trials Registry Platform";
                }

                else if (idorg.Contains("ctrp") || idorg.Contains("pdq") || idorg.Contains("nci"))
                {
                    // NCI CTRP programme
                    id.id_org_id = 100162;
                    id.id_org = "National Cancer Institute";
                    id.id_type_id = 39;
                    id.id_type = "NIH CTRP ID";
                }

                else if (idorg.Contains("daids"))
                {
                    // NAID programme
                    id.id_org_id = 100168;
                    id.id_org = "National Institute of Allergy and Infectious Diseases";
                    id.id_type_id = 40;
                    id.id_type = "DAIDS ID";
                }

                
                else if (idorg.Contains("japic") || idorg.Contains("cti"))
                {
                    // japanese registry
                    id.id_org_id = 100157;
                    id.id_org = "Japan Pharmaceutical Information Center";
                }

                else if (idorg.Contains("umin"))
                {
                    // japanese registry
                    id.id_org_id = 100156;
                    id_org = "University Hospital Medical Information Network CTR";
                }

                else if (idorg.Contains("isrctn"))
                {
                    // isrctn registry
                    id.id_org_id = 100126;
                    id.id_org = "ISRCTN";
                }

                else if (idorg.Contains("india") || id_org.Contains("ctri"))
                {
                    // indian registry
                    id.id_org_id = 100121;
                    id.id_org = "Clinical Trials Registry - India";
                    id.id_value = id.id_value.Replace("/", "-"); // slashes in id causes problems for derived paths
                }

                else if (idorg.Contains("eudract"))
                {
                    // EU CTR
                    id.id_org_id = 100123;
                    id.id_org = "EU Clinical Trials Register";
                    
                }

                else if (idorg.Contains("drks") || idorg.Contains("german") || idorg.Contains("deutsch"))
                {
                    // German registry
                    id.id_org_id = 100124;
                    id.id_org = "Deutschen Register Klinischer Studien";
                }

                else if (idorg.Contains("nederlands") || idorg.Contains("dutch"))
                {
                    // Dutch registry
                    id.id_org_id = 100132;
                    id.id_org = "The Netherlands National Trial Register";
                }

                else if (idorg.Contains("ansm") || idorg.Contains("agence") || idorg.Contains("rcb"))
                {
                    // French asnsm number
                    id.id_org_id = 101408;
                    id.id_org = "Agence Nationale de Sécurité du Médicament";
                    id.id_type_id = 41;
                    id.id_type = "Regulatory Body ID";
                }


                else if (idorg.Contains("iras") || idorg.Contains("hra"))
                {
                    // uk IRAS number
                    id.id_org_id = 101409;
                    id.id_org = "Health Research Authority";
                    id.id_type_id = 41;
                    id.id_type = "Regulatory Body ID";
                }

                else if (idorg.Contains("anzctr") || idorg.Contains("australian"))
                {
                    // australian registry
                    id.id_org_id = 100116;
                    id.id_org = "Australian New Zealand Clinical Trials Registry";
                }

                else if (idorg.Contains("chinese"))
                {
                    // chinese registry
                    id.id_org_id = 100118;
                    id.id_org = "Chinese Clinical Trial Register";
                }

                else if (idorg.Contains("thai"))
                {
                    // thai registry
                    id.id_org_id = 100131;
                    id.id_org = "Thai Clinical Trials Register";
                }

                else if (idorg.Contains("utn"))
                {
                    // thai registry
                    id.id_org_id = 100131;
                    id.id_org = "Thai Clinical Trials Register";
                }

                if (idorg == "jhmirb" || idorg == "jhm irb")
                {
                    // ethics approval number
                    id.id_org_id = 100190;
                    id.id_org = "Johns Hopkins University";
                    id.id_type_id = 12;
                    id.id_type = "Ethics Review ID";
                }

                if (idorg.ToLower().Contains("ethics") || idorg == "Independent Review Board" || idorg.Contains("IRB"))
                {
                    // ethics approval number
                    id.id_type_id = 12;
                    id.id_type = "Ethics Review ID";
                }
            }

            if (id.id_type_id == 1 || id.id_type_id == 90)
            {
                if (id_org != null)
                {
                    if (id_org == "UTN")
                    {
                        // WHO universal trail number
                        id.id_org_id = 100115;
                        id.id_org = "International Clinical Trials Registry Platform";
                        id.id_type_id = 11;
                        id.id_type = "Trial Registry ID";
                    }

                    if (id_org.ToLower().Contains("ansm") || id_org.ToLower().Contains("rcb"))
                    {
                        // French ANSM number
                        id.id_org_id = 101408;
                        id.id_org = "Agence Nationale de Sécurité du Médicament";
                        id.id_type_id = 41;
                        id.id_type = "Regulatory Body ID";
                    }

                    if (id_org == "IRAS")
                    {
                        // uk IRAS number
                        id.id_org_id = 101409;
                        id.id_org = "Health Research Authority";
                        id.id_type_id = 41;
                        id.id_type = "Regulatory Body ID";
                    }

                    if (id_org == "JHMIRB" || id_org == "JHM IRB")
                    {
                        // ethics approval number
                        id.id_org_id = 100190;
                        id.id_org = "Johns Hopkins University";
                        id.id_type_id = 12;
                        id.id_type = "Ethics Review ID";
                    }

                    if (id_org.ToLower().Contains("ethics") || id_org == "Independent Review Board" ||
                        id_org == "Institutional Review Board" || id_org.Contains("IRB"))
                    {
                        // ethics approval number
                        id.id_type_id = 12;
                        id.id_type = "Ethics Review ID";
                        id.id_org_id = 102374;
                        id.id_org = "Unspecified IRB / Ethics Review Board";
                    }

                    if (id_org.ToLower() == "pdq")
                    {
                        // NCI Physician Database id
                        id.id_org_id = 100162;
                        id.id_org = "National Cancer Institute";
                    }
                }

                if (id_value.Length > 4 && id_value.Substring(0, 4) == "NCI-")
                {
                    // NCI id
                    id.id_org_id = 100162;
                    id.id_org = "National Cancer Institute";
                }
            }

            return id;
        }


        public IdentifierDetails GetISRCTNIdentifierProps(string id_value, string study_sponsor)
        {
            // use initial values to create id details object

            IdentifierDetails id = new IdentifierDetails(14, "Sponsor ID", study_sponsor, id_value);
            string id_val = id_value.Trim().ToLower();

            if (id_val.Length < 3)
            {
                // very unlikely to be a useful id!
                id.id_type = "Protocol version";
            }

            // is id_value a protocol version number?
            // These are usually numbers (e.g. 1, 1.0, 2, 3.1, 2.01)
            else if (id_val.Length <= 4 && Regex.Match(id_value, @"^(\d{1}\.\d{1}|\d{1}\.\d{2})$").Success)
            {
                // very unlikely to be a useful id!
                id.id_type = "Protocol version";
            }

            // contains 'version' and a number at the beginning
            else if (Regex.Match(id_val, @"^version ?([0-9]|[0-9]\.[0-9]|[0-9]\.[0-9][0-9])").Success)
            {
                
                // very unlikely to be a useful id!
                id.id_type = "Protocol version";
            }

            // contains 'v' and a number at the beginning
            else if (Regex.Match(id_val, @"^v ?([0-9]|[0-9]\.[0-9]|[0-9]\.[0-9][0-9])").Success)
            {
                // very unlikely to be a useful id!
                id.id_type = "Protocol version";
            }

            // all zeroes?
            else if (Regex.Match(id_value, @"^0+$").Success)
            {
                // very unlikely to be a useful id!
                id.id_type = "Protocol version";
            }

            //starts with zeroes
            else if (Regex.Match(id_value, @"^0+").Success)
            {
                string val2 = id_value.TrimStart('0');
                if (val2.Length <= 4 && Regex.Match(val2, @"^(\d{1}|\d{1}\.\d{1}|\d{1}\.\d{2})$").Success)
                {
                    // very unlikely to be a useful id!
                    id.id_type = "Protocol version";
                }
            }


            // is it a Dutch registry id?
            if (Regex.Match(id_value, @"^NTR\d{2}").Success)
            {
                id.id_org_id = 100132;
                id.id_org = "The Netherlands National Trial Register";
                id.id_type_id = 11;
                id.id_type = "Trial Registry ID";
                // can be a 4, 3 or 2 digit number
                if (Regex.Match(id_value, @"^NTR\d{4}$").Success)
                {
                    id.id_value = Regex.Match(id_value, @"^NTR\d{4}$").Value;
                }
                else if (Regex.Match(id_value, @"^NTR\d{3}$").Success)
                {
                    id.id_value = Regex.Match(id_value, @"^NTR\d{3}$").Value;
                }
                else if (Regex.Match(id_value, @"^NTR\d{2}$").Success)
                {
                    id.id_value = Regex.Match(id_value, @"^NTR\d{2}$").Value;
                }
            }
            
            
            // a Eudract number?
            if (Regex.Match(id_value, @"[0-9]{4}-[0-9]{6}-[0-9]{2}").Success)
            {
                id.id_org_id = 100123;
                id.id_org = "EU Clinical Trials Register";
                id.id_type_id = 11;
                id.id_type = "Trial Registry ID";
                id.id_value = Regex.Match(id_value, @"[0-9]{4}-[0-9]{6}-[0-9]{2}").Value;
            }


            // An IRAS reference?
            if (id_val.Contains("iras") || id_value.Contains("hra"))
            {
                if (Regex.Match(id_value, @"([0-9]{6})").Success)
                {
                    // uk IRAS number
                    id.id_org_id = 101409;
                    id.id_org = "Health Research Authority";
                    id.id_type_id = 41;
                    id.id_type = "Regulatory Body ID";
                    id.id_value = Regex.Match(id_value, @"[0-9]{6}").Value;
                }
                else if (Regex.Match(id_value, @"([0-9]{5})").Success)
                {
                    // uk IRAS number
                    id.id_org_id = 101409;
                    id.id_org = "Health Research Authority";
                    id.id_type_id = 41;
                    id.id_type = "Regulatory Body ID";
                    id.id_value = Regex.Match(id_value, @"[0-9]{5}").Value;
                }
                else
                {
                    id.id_type = "Protocol version";
                }
            }


            // A CPMS reference?
            if (id_val.Contains("cpms") && Regex.Match(id_value, @"[0-9]{5}").Success)
            {
                // uk CPMS number
                id.id_org_id = 102002;
                id.id_org = "Central Portfolio Management System";
                id.id_type_id = 13;
                id.id_type = "Funder Id";
                id.id_value = Regex.Match(id_value, @"[0-9]{5}").Value;
            }


            // An HTA reference?
            if (id_val.Contains("hta") && Regex.Match(id_value, @"\d{2}/(\d{3}|\d{2})/\d{2}").Success)
            {
                // uk hta number
                id.id_org_id = 102003;
                id.id_org = "Health Technology Assessment programme";
                id.id_type_id = 13;
                id.id_type = "Funder Id";
                id.id_value = Regex.Match(id_value, @"\d{2}/(\d{3}|\d{2})/\d{2}").Value;
            }

            return id;
        }


        public bool CheckIfIndividual(string orgname)
        {
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

            // some specific individuals...
            if (orgname == "seung-jung park" || orgname == "kang yan")
            {
                make_individual = true;
            }
            return make_individual;
        }


        public bool CheckIfOrganisation(string fullname)
        {
            bool make_org = false;

            if (fullname.Contains(" group") || fullname.StartsWith("group") ||
                fullname.Contains(" assoc") || fullname.Contains(" team") ||
                fullname.Contains("collab") || fullname.Contains("network"))
            {
                make_org = true;
            }

            return make_org;
        }
            

        // A helper function called from the WHO processor. Returns the source name
        // from the source id

        public string? get_source_name(int? source_id)
        {
            if (source_id.HasValue)
            {
                return source_id switch
                {
                    100116 => "Australian New Zealand Clinical Trials Registry",
                    100117 => "Registro Brasileiro de Ensaios Clínicos",
                    100118 => "Chinese Clinical Trial Register",
                    100119 => "Clinical Research Information Service (South Korea)",
                    100120 => "ClinicalTrials.gov",
                    100121 => "Clinical Trials Registry - India",
                    100122 => "Registro Público Cubano de Ensayos Clínicos",
                    100123 => "EU Clinical Trials Register",
                    100124 => "Deutschen Register Klinischer Studien",
                    100125 => "Iranian Registry of Clinical Trials",
                    100126 => "ISRCTN",
                    100127 => "Japan Primary Registries Network",
                    100128 => "Pan African Clinical Trial Registry",
                    100129 => "Registro Peruano de Ensayos Clínicos",
                    100130 => "Sri Lanka Clinical Trials Registry",
                    100131 => "Thai Clinical Trials Register",
                    100132 => "Netherlands National Trial Register",
                    101989 => "Lebanon Clinical Trials Registry",
                    _ => null
                };
            }
            else
            {
                return null;
            }
        }



        public string get_registry_prefix(int? source_id)
        {
            // Used for WHO registries only

            string prefix = "";
            switch (source_id)
            {
                case 100116: { prefix = "Australian / NZ "; break; }
                case 100117: { prefix = "Brazilian "; break; }
                case 100118: { prefix = "Chinese "; break; }
                case 100119: { prefix = "South Korean "; break; }
                case 100121: { prefix = "Indian "; break; }
                case 100122: { prefix = "Peruvian "; break; }
                case 100124: { prefix = "German "; break; }
                case 100125: { prefix = "Iranian "; break; }
                case 100127: { prefix = "Japanese "; break; }
                case 100128: { prefix = "Pan African "; break; }
                case 100129: { prefix = "Peruvian "; break; }
                case 100130: { prefix = "Sri Lankan "; break; }
                case 100131: { prefix = "Thai "; break; }
                case 100132: { prefix = "Dutch "; break; }
                case 101989: { prefix = "Lebanese "; break; }
            }
            return prefix;
        }

    }



    public class IdentifierDetails
    {
        public int? id_type_id { get; set; }
        public string id_type { get; set; }
        public int? id_org_id { get; set; }
        public string id_org { get; set; }
        public string id_value { get; set; }

        public IdentifierDetails(int? _id_type_id, string _id_type,
                                 int? _id_org_id, string _id_org)
        {
            id_type_id = _id_type_id;
            id_type = _id_type;
            id_org_id = _id_org_id;
            id_org = _id_org;
        }

        public IdentifierDetails(string? _id_type, string? _id_org, string _id_value)
        {
            id_type_id = null;
            id_type = _id_type;
            id_org_id = null;
            id_org = _id_org;
            id_value = _id_value;
        }

        public IdentifierDetails(int? _id_type_id, string _id_type, string _id_org, string _id_value)
        {
            id_type_id = _id_type_id;
            id_type = _id_type;
            id_org_id = null;
            id_org = _id_org;
            id_value = _id_value;
        }

    }


}
