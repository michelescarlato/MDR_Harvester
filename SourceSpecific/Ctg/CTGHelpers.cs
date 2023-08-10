using System.Text.RegularExpressions;

namespace MDR_Harvester.Ctg;

internal static class CTGHelpers
{
    public static IdentifierDetails ProcessCTGIdentifier(this StudyIdentifier si)
    {
        // Use initial values to create id details object
        // then examine id_org and (mostly) id_type to provide
        // identifier details where possible (sometimes provides org as well)
        
        string id_value = si.identifier_value!;
        int? id_type = si.identifier_type_id;
        string id_org = si.source!;        
        string org_lower = id_org.ToLower();
        
        IdentifierDetails idd = new IdentifierDetails(si.identifier_type_id, si.identifier_type, 
            si.source_id, si.source, si.identifier_value);
        
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
        else if (id_value.StartsWith("CAN-NCIC") 
                 || org_lower.Contains("physician data query") || id_org.Contains("PDQ"))
        {
            idd.id_type_id = 49;         // NCI PDQ ID
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
        else if (id_value.ToLower().StartsWith("pactr"))
        {
            idd.id_type_id = 11;
            idd.id_type = "Trial Registry ID";
            idd.id_org_id = 100128;   // Chinese registry
            idd.id_org = "Pan African Trial Register";
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
            idd.id_org_id = 104547;     // japanese register of clinical trials
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
        else if (id_value.StartsWith("DUMC"))
        {
            idd.id_org_id = 105726;  
            idd.id_org = "Duke University Health System";
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
        else if (org_lower.Contains("aids clinical trials information") || id_org.Contains("ACTIS"))
        {
            idd.id_org_id = 101413; // NAID programme / registry Id
            idd.id_org = "National Institute of Allergy and Infectious Diseases";
            idd.id_type_id = 48;
            idd.id_type = "AIDS Clinical Trials Information Service";
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
        else if (org_lower.Contains("niaid") 
                 || id_value.StartsWith("ACTG ") || id_value.StartsWith("AVEG ")
                 || id_value.StartsWith("CPCRA "))
        {
            idd.id_org_id = 100168; // NAID programme / registry Id
            idd.id_org = "National Institute of Allergy and Infectious Diseases";
            if (id_type != 13)
            {
                idd.id_type_id = 48;
                idd.id_type = "Agency Reference ID";
            }
            idd.changed = true;
        }
        else if (Regex.Match(id_value, @"^IA[0-9]{4}$").Success) 
        {
            idd.id_org_id = 100228;    // National Institute on Aging
            idd.id_org = "National Institute on Aging";
            if (id_type != 13)
            {
               idd.id_type_id = 48;
               idd.id_type = "Agency Reference ID";
            }
            idd.changed = true;
        }
        else if (id_value.StartsWith("NCRR-"))
        {
            idd.id_type_id = 13;
            idd.id_type = "Funder / Contract ID";    
            idd.id_org_id = 100282;    // Old National Center for Research Resources
            idd.id_org = "National Center for Research Resources";
            idd.changed = true;
        }
        else if (id_value.StartsWith("NIDA-") || id_value.StartsWith("NIDA "))
        {
            idd.id_org_id = 100181; // NAID programme / registry Id
            idd.id_org = "National Institute on Drug Abuse";
            if (id_type != 13)
            {
                idd.id_type_id = 48;
                idd.id_type = "Agency Reference ID";
            }
            idd.changed = true;
        }
        else if (id_value.StartsWith("NEI-") && id_value.Length < 8)
        {
            idd.id_org_id = 100308; // NAID programme / registry Id
            idd.id_org = "National Eye Institute";
            if (id_type != 13)
            {
                idd.id_type_id = 48;
                idd.id_type = "Agency Reference ID";
            }
            idd.changed = true;
        }
        else if (id_org.Contains("HLBI") || org_lower.Contains("national heart, lung and blood"))
        {
            idd.id_org_id = 100167; // NAID programme / registry Id
            idd.id_org = "National Heart Lung and Blood Institute";
            if (id_type != 13)
            {
                idd.id_type_id = 48;
                idd.id_type = "Agency Reference IDD";
            }
            idd.changed = true;
        }
        else if (org_lower is "oncore" or "uf oncore")
        {
            idd.id_org_id = 102371; 
            idd.id_org = "OnCore";  // OnCore system
            idd.id_type_id = 43;
            idd.id_type = "CTMS ID";
            idd.changed = true;
        } 
        else if (id_value.Contains("PB-PG"))
        {
            idd.id_type_id = 13;
            idd.id_type = "Funder / Contract ID";    
            idd.id_org_id = 109757;    // NIHR RfPB
            idd.id_org = "NIHR Research for Patient Benefit";
            idd.changed = true;
        }
        else if (id_org.Contains("NIH"))
        {
            if (id_org.Contains("NIHR") && (id_org.Contains("HTA") || org_lower.Contains("technology")
                                            || id_value.StartsWith("HTA")))
            {
                idd.id_type_id = 13;
                idd.id_type = "Funder / Contract ID";    
                idd.id_org_id = 102003;    // NIHR RfPB
                idd.id_org = "NIHR HTA";
                idd.changed = true;
            }
            else if (id_org.Contains("NIHR"))
            {
                if (id_value == "NIHR")
                {
                    id_value = id_org;  // in effect swap them over
                }
                id_value = id_value.Replace("NIHR", "").Replace("CPMS", "").Trim();
                
                if (Regex.Match(id_value, @"^[0-9]{4}$").Success 
                    || Regex.Match(id_value, @"^[0-9]{5}$").Success) 
                {
                    idd.id_type_id = 41;
                    idd.id_type = "Regulatory Body ID";    
                    idd.id_org_id = 102002;    // NIHR RfPB
                    idd.id_org = "Central Portfolio Management System";
                    idd.changed = true;
                }
            }
            else if (id_org == "NIHCC" || id_org == "NIH CC" || id_org.StartsWith("NIH Clinical Center"))
            {
                idd.id_org_id = 100360;    // NIH Clinical Center
                idd.id_org = "National Institutes of Health Clinical Center";
                idd.changed = true;
            }
            else if (org_lower.Contains("grant") || org_lower.Contains("contract"))
            {
                idd.id_type_id = 13;
                idd.id_type = "Funder / Contract ID";    
                idd.id_org_id = 100134;    // NIH
                idd.id_org = "National Institutes of Health";
                idd.changed = true;
            }
            else if (id_org is "NIH" or "US NIH" || 
                     org_lower.StartsWith("nih protocol") || 
                     org_lower.StartsWith("nih office of protocol"))
            {
                idd.id_org_id = 100134;    // NIH
                idd.id_org = "National Institutes of Health";
                if (id_type is 1 or 90)
                {
                    idd.id_type_id = 48;
                    idd.id_type = "Agency Reference IDD";
                }
                idd.changed = true;
            }
        }
        else if (org_lower.Contains("zonmw"))
        {
            idd.id_type_id = 13;
            idd.id_type = "Funder / Contract ID";    
            idd.id_org_id = 100467;    // ZonMw
            idd.id_org = "ZonMw: The Netherlands Organisation for Health Research and Development";
            idd.changed = true;
        }  
        if (id_value.Contains("OG-"))
        {
            bool org_changed = false;
            if (id_value.Contains("SWOG-"))
            {
                idd.id_org_id = 100358;    
                idd.id_org = "South West Oncology Group";
                org_changed = true;
            }
            else if (id_value.Contains("RTOG-"))
            {
                idd.id_org_id = 100525;    
                idd.id_org = "Radiation Therapy Oncology Group";
                org_changed = true;
            }
            else if (id_value.Contains("ECOG-"))
            {
                idd.id_org_id = 100428;   
                idd.id_org = "Eastern Cooperative Oncology Group";
                org_changed = true;
            }
            else if (id_value.Contains("COG-"))
            {
                idd.id_org_id = 100332;   
                idd.id_org = "Children’s Oncology Group";
                org_changed = true;
            }
            else if (id_value.Contains("POG-"))
            {
                idd.id_org_id = 100332;    
                idd.id_org = "Children’s Oncology Group";
                org_changed = true;
            }
            else if (id_value.Contains("GOG-"))
            {
                idd.id_org_id = 100483;    
                idd.id_org = "Gynecologic Oncology Group";
                org_changed = true;
            }
            else if (id_value.Contains("JCOG"))
            {
                idd.id_org_id = 102049;    
                idd.id_org = "Japan Clinical Oncology Group";
                org_changed = true;
            }

            if (org_changed)
            {
                idd.id_type_id = 50;
                idd.id_type = "Research Collaboration ID";
                idd.changed = true;
            }
        }
        else if ( org_lower.EndsWith("oncology group") || org_lower.EndsWith("cancer group") 
                                   || id_org is "Breast International Group" or "ECOG" or "SWOG")
        {
            idd.id_type_id = 50;
            idd.id_type = "Research Collaboration ID";
            idd.changed = true;
        }
        else if (org_lower.EndsWith("trials.gov"))
        {
            // all NCT secondary ids should have been removed
            // Claims to be a CTG number should therefore be removed
            
            idd.id_type_id = 90;
            idd.id_type = "Other";
            idd.changed = true;
        }

        return idd;
    }
    
    public static bool IsNotBlankOrPlaceHolder(this string? idValue, string? orgValue)
    {
        if (string.IsNullOrEmpty(idValue))
        {
            return false;
        }
        string input_lc = idValue.ToLower();
        if (input_lc is "pending" or "nd" or "na" or "n/a" or "n.a."
            or "none" or "n/a." or "no" or "none" or "pending")
        {
            return false;
        }
        if (input_lc.StartsWith("not ") || input_lc.StartsWith("to be ")
            || input_lc.StartsWith("not-") || input_lc.StartsWith("not_")
            || input_lc.StartsWith("notapplic") || input_lc.StartsWith("notavail")
            || input_lc.StartsWith("tobealloc") || input_lc.StartsWith("tobeapp")
            || input_lc.StartsWith("nation"))
        {
            return false;
        }
        // Rarely, people put the same name in both source org and value fields...
        // i.e. they put the organisation name in as the identifier value, or vice versa -
        // as not easy to know which, better not add the potentially false identifier.

        if (orgValue is not null
            && String.Equals(idValue, orgValue, StringComparison.CurrentCultureIgnoreCase))
        {
            return false;
        }
        return true;
    }
 
    public static string? GetCTGStatusString(this string? study_status)
    {
        if (string.IsNullOrEmpty(study_status))
        {
            return null;
        }
        return study_status switch
        {
            "COMPLETED" => "Completed",
            "RECRUITING" => "Recruiting",
            "ACTIVE_NOT_RECRUITING" => "Active, not recruiting",
            "NOT_YET_RECRUITING" => "Not yet recruiting",
            "UNKNOWN" => "Unknown status",
            "WITHDRAWN" => "Withdrawn",
            "AVAILABLE" => "Available",
            "WITHHELD" => "Withheld",
            "NO_LONGER_AVAILABLE" => "No longer available",
            "SUSPENDED" => "Suspended",
            "TERMINATED" => "Terminated",
            "TEMPORARILY_NOT_AVAILABLE" => "Temporarily not available",
            "ENROLLING_BY_INVITATION" => "Enrolling by invitation",
            "APPROVED_FOR_MARKETING" => "Approved for marketing",
            _ => null,
        };
    }

    public static string? GetCTGTypeString(this string? study_type)
    {
        if (string.IsNullOrEmpty(study_type))
        {
            return null;
        }
        return study_type switch
        {
            "INTERVENTIONAL" => "Interventional",
            "OBSERVATIONAL" => "Observational",
            "EXPANDED_ACCESS" =>"Expanded access",
            _ => null
        };
    }
   	
    public static string? GetCTGPhaseString(this string? phase)
    {
        if (string.IsNullOrEmpty(phase))
        {
            return null;
        }
        return phase switch
        {
            "NA" => "Not applicable",
            "EARLY_PHASE1" => "Early phase 1",
            "PHASE1" => "Phase 1",
            "PHASE2" => "Phase 2",
            "PHASE3" => "Phase 3",
            "PHASE4" => "Phase 4",
            _ => null,
        };
    }

    
    public static string? GetCTGPrimaryPurposeString(this string primary_purpose)
    {
        if (string.IsNullOrEmpty(primary_purpose))
        {
            return null;
        }
        return primary_purpose switch
        {
            "TREATMENT" => "Treatment",
            "PREVENTION" => "Prevention",
            "DIAGNOSTIC" => "Diagnostic",
            "SUPPORTIVE_CARE" => "Supportive care",
            "SCREENING" => "Screening",
            "HEALTH_SERVICES_RESEARCH" => "Health services research",
            "BASIC_SCIENCE" => "Basic science",
            "DEVICE_FEASIBILITY" => "Device feasibility",
            "ECT" => "Educational/counseling/training",        
            "OTHER" => "Other",
            _ => null,
        };
    }
    
    public static string? GetCTGAllocationTypeString(this string allocation_type)
    {
        if (string.IsNullOrEmpty(allocation_type))
        {
            return null;
        }
        return allocation_type switch
        {
            "NA" => "Not applicable",
            "RANDOMIZED" => "Randomized",
            "NON_RANDOMIZED" => "Non-randomized",
            _ => null,
        };
    }
    
    public static string? GetCTGInterventionTypeString(this string design_type)
    {
        if (string.IsNullOrEmpty(design_type))
        {
            return null;
        }
        return design_type switch
        {
            "SINGLE_GROUP" => "Single group assignment",
            "PARALLEL" => "Parallel assignment",
            "CROSSOVER" => "Crossover assignment",
            "FACTORIAL" => "Factorial assignment",
            "SEQUENTIAL" => "Sequential assignment",
            _ => null,
        };
    }
    
    public static string? GetCTGMaskingTypeString(this string masking_type)
    {
        if (string.IsNullOrEmpty(masking_type))
        {
            return null;
        }
        return masking_type switch
        {
             "NONE" => "None (open label)",
             "SINGLE"=> "Single",
             "DOUBLE" => "Double",
             "TRIPLE"=> "Triple",
             "QUADRUPLE"=> "Quadruple",
            _ => null,
        };
    }

    public static string? GetCTGObsModelTypeString(this string obs_model_type)
    {
        if (string.IsNullOrEmpty(obs_model_type))
        {
            return null;
        }
        return obs_model_type switch
        {
            "COHORT" => "Cohort",
            "CASE_CONTROL" => "Case control",
            "CASE_ONLY" => "Case only",
            "CASE_CROSSOVER" => "Case crossover",
            "ECOLOGIC_OR_COMMUNITY" => "Ecologic or community",
            "FAMILY_BASED" => "Family based",
            "DEFINED_POPULATION" => "Defined population",
            "NATURAL_HISTORY" => "Natural history", 
            "OTHER" => "Other",
            _ => null,
        };
    }

    public static string? GetCTGTimePerspectiveString(this string time_perspective)
    {
        if (string.IsNullOrEmpty(time_perspective))
        {
            return null;
        }

        return time_perspective switch
        {
            "RETROSPECTIVE" => "Retrospective",
            "PROSPECTIVE" => "Prospective",
            "CROSS_SECTIONAL" => "Cross-sectional",
            "OTHER" => "Other",
            _ => null,
        };
    }

    public static string? GetCTGSpecimenRetentionString(this string specimen_retention)
    {
        if (string.IsNullOrEmpty(specimen_retention))
        {
            return null;
        }

        return specimen_retention switch
        {
            "NONE_RETAINED" => "None retained",
            "SAMPLES_WITH_DNA" => "Samples with DNA",
            "SAMPLES_WITHOUT_DNA" => "Samples without DNA",
            _ => null,
        };
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


