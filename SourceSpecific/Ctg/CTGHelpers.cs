
using System.Text.RegularExpressions;

namespace MDR_Harvester.Ctg;

internal class CTGHelpers
{
    public IdentifierDetails GetCTGIdentifierProps(string? id_type, string? id_org, string id_value)
    {
        // Use initial values to create id details object
        // then examine id_org and (mostly) id_type to provide
        // identifier details where possible (sometimes provides org as well)

        IdentifierDetails id = new IdentifierDetails(null, id_type, null, id_org, id_value);

        if (id_org is null or "Other" or "Alias Study Number")
        {
            id.id_org_id = 12;
            id.id_org = "No organisation name provided in source data";
        }

        if (id_type == null)
        {
            id.id_type_id = 1;
            id.id_type = "No type given in source data";
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

            if (id_org != null)
            {
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
                    id.id_value = id.id_value!.Replace("/", "-"); // slashes in id causes problems for derived paths
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

                else if (idorg == "jhmirb" || idorg == "jhm irb")
                {
                    // ethics approval number
                    id.id_org_id = 100190;
                    id.id_org = "Johns Hopkins University";
                    id.id_type_id = 12;
                    id.id_type = "Ethics Review ID";
                }

                else if (idorg.ToLower().Contains("ethics") || idorg == "Independent Review Board" 
                                                            || idorg.Contains("IRB"))
                {
                    // ethics approval number
                    id.id_type_id = 12;
                    id.id_type = "Ethics Review ID";
                }
            }
            
        }
        
        else if (id_type == "Other Identifier")
        {
             id.id_type_id = 90;
             id.id_type = "Other";
        }

        if (id.id_type_id is 1 or 90 && !string.IsNullOrEmpty(id_org))
        {
            // if source has no alpha characters then probably source and 
            // value should be inverted (if not the same).
            
            if (!Regex.Match(id_org, @"[A-Za-z]").Success)
            {
                if (id_org != id_value)
                {
                    (id_value, id_org) = (id_org, id_value);
                }
            }

            if (id_org == "UTN" || Regex.Match(id_value, @"^1111-").Success)
            {
                // WHO universal trail number
                id.id_org_id = 100115;
                id.id_org = "International Clinical Trials Registry Platform";
                id.id_type_id = 11;
                id.id_type = "Trial Registry ID";
            }

            else if (id_org.ToLower().Contains("ansm") || id_org.ToLower().Contains("rcb")
                    || id_org.ToLower().Contains("afssaps") || Regex.Match(id_value, @"^\d{4}-A\d{5}-\d{2}$").Success)
            {
                // French ANSM number
                id.id_org_id = 101408;
                id.id_org = "Agence Nationale de Sécurité du Médicament";
                id.id_type_id = 41;
                id.id_type = "Regulatory Body ID";
            }

            else if (id_org.ToLower().StartsWith("isrctn"))
            {
                id.id_org_id = 100126;
                id.id_org = "ISRCTN";
                id.id_type_id = 11;
                id.id_type = "Trial Registry ID";
            }
 
            else if (id_org == "IRAS" || id_org.StartsWith("IRAS"))
            {
                // uk IRAS number
                id.id_org_id = 101409;
                id.id_org = "Health Research Authority";
                id.id_type_id = 41;
                id.id_type = "Regulatory Body ID";
            }

            else if (id_org == "JHMIRB" || id_org == "JHM IRB")
            {
                // ethics approval number
                id.id_org_id = 100190;
                id.id_org = "Johns Hopkins University";
                id.id_type_id = 12;
                id.id_type = "Ethics Review ID";
            }

            else if (id_org.ToLower().Contains("ethics") || id_org == "Independent Review Board" ||
                id_org == "Institutional Review Board" || id_org.Contains("IRB"))
            {
                // ethics approval number
                id.id_type_id = 12;
                id.id_type = "Ethics Review ID";
                id.id_org_id = 102374;
                id.id_org = "Unspecified IRB / Ethics Review Board";
            }

            else if (Regex.Match(id_value, @"^CDR\d{10}$").Success)
            {
                // CDR number
                id.id_type_id = 49;
                id.id_type = "CDR number";
                id.id_org_id = 100162;
                id.id_org = "National Cancer Institute";
            }
            
            else if (id_org.ToLower() == "pdq")
            {
                // NCI Physician Database id
                id.id_org_id = 100162;
                id.id_org = "National Cancer Institute";
            }
            
            else if (id_org is "Breast International Group" or "Eastern Cooperative Oncology Group" 
                     or "Gynecologic Oncology Group" or "Pediatric Oncology Group" 
                     or "Radiation Therapy Oncology Group" or "South Weest Oncology Group" 
                     or "Children’s Oncology Group" or "Children’s Cancer Group" or "ECOG" or "SWOG" )
            {
                id.id_type_id = 50;
                id.id_type = "Research Collaboration ID";
            }
            
            else if (id_org.ToLower().StartsWith("eu ct") || id_org.ToLower().StartsWith("eu trial") ||
                     id_org.ToLower().StartsWith("eudract") || id_org.ToLower().StartsWith("ema") ||
                     id_org.ToLower().StartsWith("eu clinical trials") ||
                     id_org.ToLower().StartsWith("european medicines"))

            {
                if (!id_value.Contains("p/") && !id_value.Contains("pip") && !id_value.Contains("eupa")
                    && !id_value.Contains("irb") && !id_value.Contains("med"))
                {
                    id.id_org_id = 100123;
                    id.id_org = "EU Clinical Trials Register";
                    id.id_type_id = 11;
                    id.id_type = "Trial Registry ID";
                }
            }
        }

        if (id_value.Length > 4 && id_value[..4] == "NCI-")
        {
            // NCI id
            id.id_org_id = 100162;
            id.id_org = "National Cancer Institute";
        }

        return id;
    }
    
    
    // check name...
    internal int CheckObjectName(List<ObjectTitle> titles, string object_display_title)
    {
        int num_of_this_type = 0;
        if (titles.Count > 0)
        {
            for (int j = 0; j < titles.Count; j++)
            {
                string? title_to_test = titles[j].title_text;
                if (title_to_test is not null)
                {
                    if (title_to_test.Contains(object_display_title))
                    {
                        num_of_this_type++;
                    }
                }
            }
        }
        return num_of_this_type;
    }
}

public class IdentifierDetails
{
    public int? id_type_id { get; set; }
    public string? id_type { get; set; }
    public int? id_org_id { get; set; }
    public string? id_org { get; set; }
    public string? id_value { get; set; }

    public IdentifierDetails(int? _id_type_id, string? _id_type, int? _id_org_id, string? _id_org,
        string? _id_value)
    {
        id_type_id = _id_type_id;
        id_type = _id_type;
        id_org_id = _id_org_id;
        id_org = _id_org;
        id_value = _id_value;
    }
}


