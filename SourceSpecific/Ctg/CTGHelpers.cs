using System.Text.RegularExpressions;

namespace MDR_Harvester.Ctg;

internal class CTGHelpers
{
    public IdentifierDetails ProcessCTGIdentifier(StudyIdentifier si)
    {
        // Use initial values to create id details object
        // then examine id_org and (mostly) id_type to provide
        // identifier details where possible (sometimes provides org as well)
        
        string id_value = si.identifier_value!;
        int? id_type = si.identifier_type_id;
        
        string id_org = si.source!;        
        string org_lower = id_org.ToLower();
        
        IdentifierDetails idd = new IdentifierDetails(null, si.identifier_type, null, si.source, si.identifier_value);
        
        // if source has no alpha characters then probably source and 
        // value should be inverted (if not the same). Rare but happens.

        if (!Regex.Match(id_org, @"[A-Za-z]").Success)
        {
            if (id_org != id_value)
            {
                (id_value, id_org) = (id_org, id_value);
            }
        }

        // Where possible use regex matches with the id value as the prime method of matching or
        // over-riding identifier types, as the given type and org names may not be consistently applied.
        // Otherwise, or in addition, use the org name. 

        if (Regex.Match(id_value, @"20(0|1|2)[0-9]-[0-9]{6}-[0-9]{2}").Success)
        {
            idd.id_value = Regex.Match(id_value, @"20(0|1|2)[0-9]-[0-9]{6}-[0-9]{2}").Value;
            idd.id_org_id = 100123;      // Eudract number
            idd.id_org = "EU Clinical Trials Register";
            idd.id_type_id = 11;
            idd.id_type = "Trial Registry ID";
            idd.changed = true;
        }
        else if (Regex.Match(id_value, @"1111-[0-9]{4}-[0-9]{4}").Success)
        {
            idd.id_value = "U" + Regex.Match(id_value, @"1111-\d{4}-\d{4}").Value;
            idd.id_org_id = 100115;       // WHO universal trail number
            idd.id_org = "International Clinical Trials Registry Platform";
            idd.id_type_id = 11;
            idd.id_type = "Trial Registry ID";
            idd.changed = true;
        }
        else if (Regex.Match(id_value, @"^CDR\d{10}").Success)
        {
            idd.id_value = Regex.Match(id_value, @"^CDR\d{10}").Value;
            idd.id_type_id = 49;         // CDR number - NCI PDQ ID
            idd.id_type = "NCI PDQ ID";
            idd.id_org_id = 100162;
            idd.id_org = "National Cancer Institute";
            idd.changed = true;
        }
        else if (Regex.Match(id_value, @"NCI-20(0|1|2)[0-9]-[0-9]{5}").Success)
        {
            idd.id_value = Regex.Match(id_value, @"NCI-20(0|1|2)[0-9]-[0-9]{5}").Value;
            idd.id_type_id = 39;          // CTRP number - NCI CTRP ID
            idd.id_type = "NCI CTRP ID";
            idd.id_org_id = 100162;
            idd.id_org = "National Cancer Institute";
            idd.changed = true;
        }
        else if (Regex.Match(id_value, @"^NCI-").Success)
        {
            idd.id_type_id = 13;   // Remaining NCI- ids appear to be NCI grant identifiers
            idd.id_type = "Funder / Contract ID";   // though take care not a non-US NCI!
            idd.id_org_id = 100162;
            idd.id_org = "National Cancer Institute";
            idd.changed = true;
        }
        else if (Regex.Match(id_value, @"^\d{4}-A\d{5}-\d{2}$").Success)
        {
            idd.id_org_id = 101408;    // French asnsm number
            idd.id_org = "Agence Nationale de Sécurité du Médicament";
            idd.id_type_id = 41;
            idd.id_type = "Regulatory Body ID";
            idd.changed = true;
        }
        else if (org_lower.Contains("ansm") || org_lower.Contains("agence") || org_lower.Contains("rcb")
                         || org_lower.Contains("afssaps"))
        {
            if (Regex.Match(id_value, @"\d{4}-A\d{5}-\d{2}").Success)
            {
                // Value often embedded in a longer string
                idd.id_value = Regex.Match(id_value, @"\d{4}-A\d{5}-\d{2}").Value;
            }
            idd.id_org_id = 101408;    // French asnsm number
            idd.id_org = "Agence Nationale de Sécurité du Médicament";
            idd.id_type_id = 41;
            idd.id_type = "Regulatory Body ID";
            idd.changed = true;
        }
        else if (org_lower.Contains("cnil"))
        {
            idd.id_org_id = 109732;    // French CNIL number
            idd.id_org = "CNIL";
            idd.id_type_id = 41;
            idd.id_type = "Regulatory Body ID";
            idd.changed = true;
        }          
        else if (Regex.Match(id_value, @"^NL ?[0-9]{5}\.[0-9]{3}\.(0|1|2)[0-9]").Success)
        {
            idd.id_value = Regex.Match(id_value, @"^NL ?[0-9]{5}\.[0-9]{3}\.(0|1|2)[0-9]").Value;
            idd.id_org_id = 109113;    // Dutch CCMO number (type 1)
            idd.id_org = "CCMO";
            idd.id_type_id = 12;
            idd.id_type = "Ethics Review ID";
            idd.changed = true;
        }
        else if (Regex.Match(id_value, @"^NL ?[0-9]{8}(0|1|2)[0-9]").Success)
        {
            idd.id_value = Regex.Match(id_value, @"^NL ?[0-9]{8}(0|1|2)[0-9]").Value;
            idd.id_org_id = 109113;    // Dutch CCMO number (type 2)
            idd.id_org = "CCMO";
            idd.id_type_id = 12;
            idd.id_type = "Ethics Review ID";
            idd.changed = true;
        }
        else if (id_value.StartsWith("NL") || id_value.StartsWith("NTR")
                 && (org_lower.Contains("nederlands") || org_lower.Contains("netherlands") 
                 || org_lower.Contains("dutch") || org_lower == "ntr"))   
        {
            idd.id_type_id = 11;  // remaining NLs Dutch Trial Registry Ids, if they meet criteria above
            idd.id_type = "Trial Registry ID";
            idd.id_org_id = 100132;
            idd.id_org = "The Netherlands National Trial Register";
            idd.changed = true;
        }
        else if (id_type == 11 && org_lower.Contains("isrctn")
                 || id_value.ToLower().Contains("isrctn"))
        {
            if (id_value.Length == 9)
            {
                id_value = id_value[1..];  // rare but take last 8 characters
            }
            if (Regex.Match(id_value, @"[0-9]{8}").Success) // Try and regularise the value
            {
                id_value = "ISRCTN" + Regex.Match(id_value, @"[0-9]{8}").Value;
            }
            idd.id_value = id_value;
            idd.id_type_id = 11;   
            idd.id_type = "Trial Registry ID";
            idd.id_org_id = 100126;  // isrctn registry
            idd.id_org = "ISRCTN";
            idd.changed = true;
        }
        else if ((id_type == 11 && (org_lower.Contains("drks") || org_lower.Contains("german") 
                 || org_lower.Contains("deutsch")) || id_value.Contains("DRKS")))
        {
            if (Regex.Match(id_value, @"[0-9]{8}").Success) // Try and regularise the value
            {
                id_value = "DRKS" + Regex.Match(id_value, @"[0-9]{8}").Value;
            }
            idd.id_value = id_value;
            idd.id_type_id = 11;
            idd.id_type = "Trial Registry ID";
            idd.id_org_id = 100124;  // German registry
            idd.id_org = "Deutschen Register Klinischer Studien";
            idd.changed = true;
        }
        else if ((id_type == 11 && (org_lower.Contains("india") || org_lower.Contains("ctri"))
                 || id_value.Contains("CTRI-") || id_value.Contains("CTRI/")))
        {
            idd.id_value = id_value.Replace("/", "-"); // slashes in id causes problems for derived paths
            idd.id_type_id = 11;
            idd.id_type = "Trial Registry ID";
            idd.id_org_id = 100121;  // indian registry
            idd.id_org = "Clinical Trials Registry - India";
            idd.changed = true;
        }
        else if ((id_type == 11 && (org_lower.Contains("anzctr") || org_lower.Contains("australian"))
                  || id_value.Contains("ACTRN")))
        {
            id_value = id_value.Replace("ACTRNO", "ACTRN0"); // just 1 occurence
            if (Regex.Match(id_value, @"[0-9]{14}").Success) // Try and regularise the value
            {
                id_value = "ACTRN" + Regex.Match(id_value, @"[0-9]{14}").Value;
            }
            idd.id_value = id_value;
            idd.id_type_id = 11;
            idd.id_type = "Trial Registry ID";
            idd.id_org_id = 100116;  // Australian registry
            idd.id_org = "Australian New Zealand Clinical Trials Registry";
            idd.changed = true;
        }
        else if (id_value.ToLower().StartsWith("rbr-"))
        {
            idd.id_type_id = 11;
            idd.id_type = "Trial Registry ID";
            idd.id_org_id = 100117;    // CRIS Korea
            idd.id_org = "Registro Brasileiro de Ensaios Clínicos";
            idd.changed = true;
        }
        else if (Regex.Match(id_value, @"^KCT[0-9]{7}").Success)
        {
            idd.id_value = Regex.Match(id_value, @"^KCT[0-9]{7}").Value;
            idd.id_type_id = 11;
            idd.id_type = "Trial Registry ID";
            idd.id_org_id = 100119;    // CRIS Korea
            idd.id_org = "Clinical Research Information Service";
            idd.changed = true;
        }
        else if (id_value.ToLower().StartsWith("chictr"))
        {
            idd.id_type_id = 11;
            idd.id_type = "Trial Registry ID";
            idd.id_org_id = 100118;   // Chinese registry
            idd.id_org = "Chinese Clinical Trial Register";
            idd.changed = true;
        }
        else if (id_value.ToLower().StartsWith("tctr"))
        {
            // thai registry
            idd.id_type_id = 11;
            idd.id_type = "Trial Registry ID";
            idd.id_org_id = 100131;
            idd.id_org = "Thai Clinical Trials Register";
            idd.changed = true;
        }   
        else if (org_lower.StartsWith("japan registry") || org_lower.StartsWith("jrct") || id_value.ToLower().StartsWith("jrct") )
        {
            if (!id_value.ToLower().StartsWith("jrct"))
            {
                id_value = "jRCT" + id_value;
            }
            idd.id_value = id_value;
            idd.id_type_id = 11;
            idd.id_type = "Trial Registry ID";
            idd.id_org_id = 104547;     // japanese register f clinical trialsregistry
            idd.id_org = "";
            idd.changed = true;
        }
        else if (org_lower.StartsWith("japic") || id_value.ToLower().StartsWith("japic") )
        {
            if (id_value.ToLower().StartsWith("CTI"))
            {
                id_value = "Japic" + id_value;
            }
            if (!id_value.ToLower().StartsWith("Japic"))
            {
                id_value = "JapicCTI-" + id_value;
            }
            idd.id_value = id_value;           
            idd.id_type_id = 11;
            idd.id_type = "Trial Registry ID";
            idd.id_org_id = 100157;    // japanese registry
            idd.id_org = "Japan Pharmaceutical Information Center";
            idd.changed = true;
        }
        else if (id_org.Contains("UMIN") || (id_value.ToLower().StartsWith("umin") 
                  && !id_value.ToLower().StartsWith("uminho") &&  !id_value.ToLower().StartsWith("uminne"))  )
        {
            if (Regex.Match(id_org, @"[0-9]{9}").Success) // in 2 cases value is in the wrong field !
            {
                idd.id_value = "UMIN" + Regex.Match(id_org, @"[0-9]{9}").Value;
            }
            if (Regex.Match(id_value, @"[0-9]{9}").Success) // Try and regularise the value
            {
                idd.id_value = "UMIN" + Regex.Match(id_value, @"[0-9]{9}").Value;
            }
            idd.id_type_id = 11;
            idd.id_type = "Trial Registry ID";
            idd.id_org_id = 100156;   // japanese UMIN registry
            idd.id_org = "University Hospital Medical Information Network CTR";
            idd.changed = true;
        }
        else if (Regex.Match(id_value, @"^EUPAS[0-9]").Success) 
        {
            idd.id_value = "EUPAS " + id_value.Replace("EUPAS", "").Trim();
            idd.id_type_id = 11;
            idd.id_type = "Trial Registry ID";
            idd.id_org_id = 109753;    // EU PAS Register
            idd.id_org = "EU PAS Register";
            idd.changed = true;
        }
        else if (id_value.StartsWith("ENCEPP/SDPP/"))
        {
            idd.id_value = "EUPAS " + id_value.Replace("ENCEPP/SDPP/", "").Trim();
            idd.id_type_id = 11;
            idd.id_type = "Trial Registry ID";
            idd.id_org_id = 109753;    // EU PAS Register
            idd.id_org = "EU PAS Register";
            idd.changed = true;
        }
        else if (id_value.Contains("CIV-"))
        {
            idd.id_value = id_value.Replace("EUDAMED", "").Replace("EUDRAMED", "").Trim();
            idd.id_type_id = 41;
            idd.id_type = "Regulatory Body ID";    
            idd.id_org_id = 1023011;    // EUDAMED EU Medical Device approval 
            idd.id_org = "European Database on Medical Devices";
            idd.changed = true;
        }
        else if (id_value.Contains("MHRA"))
        {
            idd.id_type_id = 41;
            idd.id_type = "Regulatory Body ID";    
            idd.id_org_id = 100161;    // UK Medicines agency approval
            idd.id_org = "Medicines and Healthcare Products Regulatory Agency";
            idd.changed = true;
        }
        else if (id_org.Contains("IRAS") || id_org.Contains("HRA"))
        {
            idd.id_type_id = 41;
            idd.id_type = "Regulatory Body ID";            
            idd.id_org_id = 109755;    // UK IRAS number
            idd.id_org = "Integrated Research Application System";
            idd.changed = true;
        }
        else if (org_lower is "independent review board" or "institutional review board" or "irb" or "ethics committee")
        {
            idd.id_type_id = 12;
            idd.id_type = "Ethics Review ID";
            idd.id_org_id = 14;
            idd.id_org = "Unspecified IRB / Ethics Review Board";
            idd.changed = true;
        }
        else if (org_lower is "nres")
        {
            idd.id_type_id = 12;
            idd.id_type = "Ethics Review ID";
            idd.id_org_id = 109754;   // UK Ethics Service
            idd.id_org = "National Research Ethics Service";
            idd.changed = true;
        }
        else if ((org_lower.Contains(" irb") || org_lower.StartsWith("irb")) && id_value.Contains("DUMC"))
        {
            idd.id_type_id = 12;
            idd.id_type = "Ethics Review ID";
            idd.id_org_id = 109744;  
            idd.id_org = "Duke University IRB";
            idd.changed = true;
        }
        else if (org_lower.Contains(" irb") || org_lower.StartsWith("irb") || org_lower.Contains("ethics") 
                || org_lower.Contains("review board"))
        {
            idd.id_type_id = 12;
            idd.id_type = "Ethics Review ID";
            idd.changed = true;
        }
        else if (org_lower is "ctep" or "nci/ctep" )
        {
            idd.id_org_id = 101412; // CTEP
            idd.id_org = "Cancer Therapy Evaluation Program";
            idd.id_type_id = 46;
            idd.id_type = "CTEP ID";
            idd.changed = true;
        }
        else if (org_lower.Contains("daids"))
        {
            idd.id_org_id = 100168; // NAID programme / registry Id
            idd.id_org = "National Institute of Allergy and Infectious Diseases";
            idd.id_type_id = 40;
            idd.id_type = "DAIDS-ES registry ID";
            idd.changed = true;
        }
        else if (org_lower.Contains("niaid") && org_lower.Contains("crms"))
        {
            idd.id_org_id = 100168; // NAID programme / registry Id
            idd.id_org = "National Institute of Allergy and Infectious Diseases";
            idd.id_type_id = 43;
            idd.id_type = "CTMS ID";
            idd.changed = true;
        }
        else if (org_lower.Contains("niaid"))
        {
            idd.id_org_id = 100168; // NAID programme / registry Id
            idd.id_org = "National Institute of Allergy and Infectious Diseases";
        }
        else if (org_lower is "oncore" or "uf oncore")
        {
            idd.id_org_id = 102371; 
            idd.id_org = "OnCore";  // OnCore system
            idd.id_type_id = 43;
            idd.id_type = "CTMS ID";
            idd.changed = true;
        } 
        else if ( org_lower.EndsWith("oncology group") || org_lower.EndsWith("cancer group") 
                                   || id_org is "Breast International Group" or "ECOG" or "SWOG")
        {
            idd.id_type_id = 50;
            idd.id_type = "Research Collaboration ID";
            idd.changed = true;
        }

        return idd;
    }
}

public class IdentifierDetails
{
    public bool changed { get; set; }
    public int? id_type_id { get; set; }
    public string? id_type { get; set; }
    public int? id_org_id { get; set; }
    public string? id_org { get; set; }
    public string? id_value { get; set; }

    public IdentifierDetails(int? _id_type_id, string? _id_type, int? _id_org_id, string? _id_org,
        string? _id_value)
    {
        changed = false;
        id_type_id = _id_type_id;
        id_type = _id_type;
        id_org_id = _id_org_id;
        id_org = _id_org;
        id_value = _id_value;
    }
}


