using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MDR_Harvester.Who;

internal class WhoHelpers
{
    internal string? GetSourceName(int? source_id)
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


    internal string? GetRegistryPrefix(int? source_id)
    {

        // from the source id
        // Used for WHO registries only

        if (source_id.HasValue)
        {
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
        else
        {
            return null;
        }
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


    internal StudyIdentifier GetANZIdentifier(string sid, string processed_id, bool? sponsor_is_org, string sponsor_name)
    {
        // australian nz identifiers
        if (processed_id.StartsWith("ADHB"))
        {
            return new StudyIdentifier(sid, processed_id, 41, "Regulatory body ID", 104531, "Aukland District Health Board");
        }

        else if (processed_id.StartsWith("Auckland District Health Board"))
        {
            processed_id = processed_id.Replace("Auckland District Health Board", "");
            processed_id = processed_id.Replace("registration", "").Replace("research", "");
            processed_id = processed_id.Replace("number", "").Replace(":", "").Trim();
            return new StudyIdentifier(sid, processed_id, 41, "Regulatory body ID", 104531, "Aukland District Health Board");
        }

        else if (processed_id.StartsWith("AG01") || processed_id.StartsWith("AG02") || processed_id.StartsWith("AG03")
            || processed_id.StartsWith("AG101") || processed_id.StartsWith("AGITG"))
        {
            return new StudyIdentifier(sid, processed_id, 43, "Collaborative Group ID", 104532, "Australian Gastrointestinal Trials Group");
        }

        else if (processed_id.Contains("Australasian Gastro-Intestinal Trials Group: "))
        {
            processed_id = processed_id.Replace("Australasian Gastro-Intestinal Trials Group:", "");
            processed_id = processed_id.Replace("The", "").Replace("(AGITG)", "");
            processed_id = processed_id.Replace("Protocol No:", "").Trim();
            return new StudyIdentifier(sid, processed_id, 43, "Collaborative Group ID", 104532, "Australian Gastrointestinal Trials Group");
        }

        else if (processed_id.StartsWith("ALLG") || processed_id.StartsWith("AMLM"))
        {
            return new StudyIdentifier(sid, processed_id, 43, "Collaborative Group ID", 103289, "Australasian Leukaemia and Lymphoma Group");
        }

        else if (processed_id.Contains("Australasian Leukaemia and Lymphoma Group"))
        {
            processed_id = processed_id.Replace("Australasian Leukaemia and Lymphoma Group", "");
            processed_id = processed_id.Replace("The", "").Replace("(ALLG):", "");
            processed_id = processed_id.Replace(":", "").Replace("-", "").Trim();
            return new StudyIdentifier(sid, processed_id, 43, "Collaborative Group ID", 103289, "Australasian Leukaemia and Lymphoma Group");
        }

        else if (processed_id.StartsWith("ANZGOG"))
        {
            return new StudyIdentifier(sid, processed_id, 43, "Collaborative Group ID", 104533, "Australia New Zealand Gynaecological Oncology Group");
        }

        else if (processed_id.StartsWith("Australia and New Zealand Gynecological Oncology Group: "))
        {
            processed_id = processed_id.Replace("Australia and New Zealand Gynecological Oncology Group:", "").Trim();
            return new StudyIdentifier(sid, processed_id, 43, "Collaborative Group ID", 104533, "Australia New Zealand Gynaecological Oncology Group");
        }

        else if (processed_id.StartsWith("Australasian Sarcoma Study Group Number"))
        {
            processed_id = processed_id.Replace("Australasian Sarcoma Study Group Number", "").Trim();
            return new StudyIdentifier(sid, processed_id, 43, "Collaborative Group ID", 104534, "Australasian Sarcoma Study Group");
        }

        else if (processed_id.StartsWith("ANZMTG"))
        {
            return new StudyIdentifier(sid, processed_id, 43, "Collaborative Group ID", 104535, "Australia and New Zealand Melanoma Trials Group");
        }

        else if (processed_id.StartsWith("ANZUP"))
        {
            return new StudyIdentifier(sid, processed_id, 43, "Collaborative Group ID", 104536, "Australian and New Zealand Urogenital and Prostate Cancer Trials Group");
        }

        else if (processed_id.StartsWith("APP") || processed_id.StartsWith("GNT"))
        {
            return new StudyIdentifier(sid, processed_id, 13, "Funder’s ID", 100690, "National Health and Medical Research Council, Australia");
        }

        else if (processed_id.StartsWith("Australian NH&MRC"))
        {
            processed_id = processed_id.Replace("Australian NH&MRC", "");
            processed_id = processed_id.Replace("Project", "").Replace("Grant", "");
            processed_id = processed_id.Replace("Targeted Call for Research", "").Trim();

            return new StudyIdentifier(sid, processed_id, 13, "Funder’s ID", 100690, "National Health and Medical Research Council, Australia");
        }

        else if (processed_id.StartsWith("National Health and Medical Research Council") ||
                    processed_id.StartsWith("National Health & Medical Research Council"))
        {
            processed_id = processed_id.Replace("National Health", "").Replace("Medical Research Council", "");
            processed_id = processed_id.Replace("and", "").Replace("&", "").Replace("NHMRC", "");
            processed_id = processed_id.Replace("application", "", StringComparison.CurrentCultureIgnoreCase);
            processed_id = processed_id.Replace("grant", "", StringComparison.CurrentCultureIgnoreCase).Replace("ID", "");
            processed_id = processed_id.Replace("number", "", StringComparison.CurrentCultureIgnoreCase).Replace("No:", "");
            processed_id = processed_id.Replace("project", "", StringComparison.CurrentCultureIgnoreCase);
            processed_id = processed_id.Replace("(", "").Replace(")", "").Replace("funding", "").Replace("body", "");
            processed_id = processed_id.Replace("of Australia", "").Replace("Postgraduate Scholarship", "");
            processed_id = processed_id.Replace("protocol", "", StringComparison.CurrentCultureIgnoreCase);
            processed_id = processed_id.Replace("Global Alliance for Chronic Diseases (GACD) initiative", "");
            processed_id = processed_id.Replace(":", "").Replace(",", "").Replace("#", "").Trim();

            return new StudyIdentifier(sid, processed_id, 13, "Funder’s ID", 100690, "National Health and Medical Research Council, Australia");
        }

        else if (processed_id.StartsWith("NHMRCC"))
        {
            processed_id = processed_id.Replace("NHMRC", "");
            processed_id = processed_id.Replace("grant", "", StringComparison.CurrentCultureIgnoreCase);
            processed_id = processed_id.Replace("project", "", StringComparison.CurrentCultureIgnoreCase);
            processed_id = processed_id.Replace("No:", "", StringComparison.CurrentCultureIgnoreCase).Replace("ID", "");
            processed_id = processed_id.Replace("application", "", StringComparison.CurrentCultureIgnoreCase);
            processed_id = processed_id.Replace("and FHF Centre for Research Excellence (CRE) in Diabetic Retinopathy App", "");
            processed_id = processed_id.Replace("CIA_Campbell.", "");
            processed_id = processed_id.Replace("partnership", "", StringComparison.CurrentCultureIgnoreCase);
            processed_id = processed_id.Replace(":", "").Replace(",", "").Replace("#", "").Replace("_", "").Replace(".", "").Trim();

            return new StudyIdentifier(sid, processed_id, 13, "Funder’s ID", 100690, "National Health and Medical Research Council, Australia");
        }

        else if (processed_id.StartsWith("Australian Research Council"))
        {
            processed_id = processed_id.Replace("Australian Research Council", "");
            processed_id = processed_id.Replace("(", "").Replace(")", "").Trim();

            return new StudyIdentifier(sid, processed_id, 13, "Funder’s ID", 104537, "Australian Research Council");
        }

        else if (processed_id.StartsWith("Australian Therapeutic Goods Administration"))
        {
            processed_id = processed_id.Replace("Australian Therapeutic Goods Administration", "");
            processed_id = processed_id.Replace("Clinical Trial", "").Replace("TGA", "");
            processed_id = processed_id.Replace("CTN", "").Replace("(", "").Replace(")", "");
            processed_id = processed_id.Replace("number", "", StringComparison.CurrentCultureIgnoreCase);
            processed_id = processed_id.Replace("-", "").Replace(".", "").Replace("Notification", "").Trim();

            return new StudyIdentifier(sid, processed_id, 41, "Regulatory body ID", 104538, "Australian Therapeutic Goods Administration CTN");
        }

        else if (processed_id.Contains("Clinical Trial") && processed_id.Contains("CTN"))
        {
            processed_id = processed_id.Replace("Clinical Trial", "").Replace("TGA", "");
            processed_id = processed_id.Replace("CTN", "").Replace("(", "").Replace(")", "").Replace(":", "");
            processed_id = processed_id.Replace("Network", "").Replace("Notification", "").Trim();

            return new StudyIdentifier(sid, processed_id, 41, "Regulatory body ID", 104538, "Australian Therapeutic Goods Administration CTN");
        }

        else if (processed_id.StartsWith("TGA"))
        {
            processed_id = processed_id.Replace("Clinical", "").Replace("TGA", "").Replace("CTN", "");
            processed_id = processed_id.Replace("trial", "", StringComparison.CurrentCultureIgnoreCase)
                                        .Replace("trials", "", StringComparison.CurrentCultureIgnoreCase);
            processed_id = processed_id.Replace("number", "", StringComparison.CurrentCultureIgnoreCase);
            processed_id = processed_id.Replace(":", "").Replace("Notification", "").Trim();

            return new StudyIdentifier(sid, processed_id, 41, "Regulatory body ID", 104538, "Australian Therapeutic Goods Administration CTN");
        }

        else if (processed_id.StartsWith("Therapeutic good administration") ||
                    processed_id.StartsWith("Therapeutic Goods Administration") ||
                    processed_id.StartsWith("Therapeutic Goods Association"))
        {
            processed_id = processed_id.Replace("Therapeutic", "").Replace("goods", "").Replace("Goods", "");
            processed_id = processed_id.Replace("TGA", "").Replace("CTN", "");
            processed_id = processed_id.Replace("administration", "", StringComparison.CurrentCultureIgnoreCase);
            processed_id = processed_id.Replace("association", "", StringComparison.CurrentCultureIgnoreCase);
            processed_id = processed_id.Replace("clinical", "", StringComparison.CurrentCultureIgnoreCase);
            processed_id = processed_id.Replace("trial", "", StringComparison.CurrentCultureIgnoreCase);
            processed_id = processed_id.Replace("notification", "", StringComparison.CurrentCultureIgnoreCase);
            processed_id = processed_id.Replace("number", "", StringComparison.CurrentCultureIgnoreCase);
            processed_id = processed_id.Replace("protocol", "", StringComparison.CurrentCultureIgnoreCase);
            processed_id = processed_id.Replace("reference", "", StringComparison.CurrentCultureIgnoreCase);
            processed_id = processed_id.Replace("no.", "", StringComparison.CurrentCultureIgnoreCase);
            processed_id = processed_id.Replace("Australian Govt, Dept of Health -", "");
            processed_id = processed_id.Replace("scheme", "", StringComparison.CurrentCultureIgnoreCase);
            processed_id = processed_id.Replace(":", "").Replace("(", "").Replace(")", "").Replace(",", "").Trim();
            if (processed_id.StartsWith("-")) processed_id = processed_id.Substring(1).Trim();

            return new StudyIdentifier(sid, processed_id, 41, "Regulatory body ID", 104538, "Australian Therapeutic Goods Administration CTN");
        }

        else if (processed_id.StartsWith("CRG") || processed_id.StartsWith("Cochrane Renal Group"))
        {
            processed_id = processed_id.Replace("Cochrane Renal Group", "").Replace("CRG", "");
            processed_id = processed_id.Replace("(", "").Replace(")", "").Replace("-", "").Replace(".", "").Replace(":", "").Trim();

            return new StudyIdentifier(sid, "CRG" + processed_id, 43, "Collaborative Group ID", 104539, "Cochrane Renal Group");
        }

        else if (processed_id.StartsWith("Commonwealth Scientific and Industrial Research Organisation"))
        {
            processed_id = processed_id.Replace("Commonwealth Scientific and Industrial Research Organisation", "");
            processed_id = processed_id.Replace("(CSIRO)", "").Replace(":", "").Replace("and", "").Trim();

            return new StudyIdentifier(sid, processed_id, 13, "Funder’s ID", 104540, "Commonwealth Scientific and Industrial Research Organisation");
        }

        else if (processed_id.StartsWith("Health Research Council") && !processed_id.Contains("Funding"))
        {
            processed_id = processed_id.Replace("Health Research Council", "");
            processed_id = processed_id.Replace("of New Zealand", "");
            processed_id = processed_id.Replace("NZ", "").Replace("HRC", "");
            processed_id = processed_id.Replace("reference", "", StringComparison.CurrentCultureIgnoreCase).Replace("Ref.", "");
            processed_id = processed_id.Replace("number", "", StringComparison.CurrentCultureIgnoreCase).Replace("programme", "");
            processed_id = processed_id.Replace("grant", "").Trim();

            return new StudyIdentifier(sid, processed_id, 13, "Funder’s ID", 104541, "Health Research Council of New Zealand");
        }

        else if (processed_id.StartsWith("HRC"))
        {
            processed_id = processed_id.Replace("HRC", "");
            processed_id = processed_id.Replace("Emerging Research First Grant- ", "");
            processed_id = processed_id.Replace("Project Grant Number #", "");
            processed_id = processed_id.Replace("Ref:", "").Trim(); ;

            return new StudyIdentifier(sid, processed_id, 13, "Funder’s ID", 104541, "Health Research Council of New Zealand");
        }

        else if (processed_id.StartsWith("HREC"))
        {
            if (sponsor_is_org == true)
            {
                return new StudyIdentifier(sid, processed_id, 12, "Ethics review ID", null, sponsor_name);
            }
            else
            {
                return new StudyIdentifier(sid, processed_id, 12, "Ethics review ID", 12, "No organisation name provided in source data");
            }

        }

        else if (processed_id.StartsWith("Human Research Ethics Committee (HREC):"))
        {
            // ??? change type and keep sponsor
            processed_id = processed_id.Replace("Human Research Ethics Committee (HREC):", "");

            if (sponsor_is_org == true)
            {
                return new StudyIdentifier(sid, processed_id, 12, "Ethics review ID", null, sponsor_name);
            }
            else
            {
                return new StudyIdentifier(sid, processed_id, 12, "Ethics review ID", 12, "No organisation name provided in source data");
            }
        }

        else if (processed_id.StartsWith("MRINZ"))
        {
            return new StudyIdentifier(sid, processed_id, 43, "Collaborative Group ID", 103010, "Medical Research Institute of New Zealand");
        }

        else if (processed_id.StartsWith("National Clinical Trials Registry"))
        {
            processed_id = processed_id.Replace("National Clinical Trials Registry:", "").Trim();
            return Add(new StudyIdentifier(sid, processed_id, 11, "Trial registry ID", 104548, "National Clinical Trials Registry (Australia)");
        }
        else if (processed_id.StartsWith("Perinatal Trials Registry:"))
        {
            processed_id = processed_id.Replace("Perinatal Trials Registry:", "").Trim();
            return new StudyIdentifier(sid, processed_id, 11, "Trial registry ID", 104542, "Perinatal Trials Registry (Australia)");
        }

        else if (processed_id.StartsWith("TROG"))
        {
            return new StudyIdentifier(sid, processed_id, 43, "Collaborative Group ID", 104543, "Trans Tasman Radiation Oncology Group");
        }

        else
        {
            if (sponsor_is_org == true)
            {
                return new StudyIdentifier(sid, processed_id, 14, "Sponsor ID", null, sponsor_name);
            }
            else
            {
                return new StudyIdentifier(sid, processed_id, 14, "Sponsor ID", 12, "No organisation name provided in source data");
            }
        }
    }


    internal StudyIdentifier GetChineseIdentifier(string sid, string processed_id, bool? sponsor_is_org, string sponsor_name)
    {
        if (!processed_id.EndsWith("#32"))    // first ignore these (small sub group)
        {
            if (processed_id.StartsWith("AMCTR"))
            {
                return new StudyIdentifier(sid, processed_id, 11, "Trial Registry ID", 104544, "Acupuncture-Moxibustion Clinical Trial Registry");
            }
            else if (processed_id.StartsWith("ChiMCTR"))
            {
                return new StudyIdentifier(sid, processed_id, 11, "Trial Registry ID", 104545, "Chinese Medicine Clinical Trials Registry");
            }
            else if (processed_id.StartsWith("CUHK"))
            {
                return new StudyIdentifier(sid, processed_id, 11, "Trial Registry ID", 104546, "CUHK Clinical Research and Biostatistics Clinical Trials Registry");
            }
            else
            {
                if (sponsor_is_org == true)
                {
                    return new StudyIdentifier(sid, processed_id, 14, "Sponsor ID", null, sponsor_name);
                }
                else
                {
                    return new StudyIdentifier(sid, processed_id, 14, "Sponsor ID", 12, "No organisation name provided in source data");
                }
            }
        }
    }


    internal StudyIdentifier GetJapaneseIdentifier(string sid, string processed_id, bool? sponsor_is_org, string sponsor_name)
    {
        if (processed_id.StartsWith("JapicCTI"))
        {
            return new StudyIdentifier(sid, processed_id, 11, "Trial Registry ID", 100157, "Japan Pharmaceutical Information Center");
        }
        else if (processed_id.StartsWith("JMA"))
        {
            return new StudyIdentifier(sid, processed_id, 11, "Trial Registry ID", 100158, "Japan Medical Association – Center for Clinical Trials");
        }
        else if (processed_id.StartsWith("jCRT") || processed_id.StartsWith("JCRT"))
        {
            return new StudyIdentifier(sid, processed_id, 11, "Trial Registry ID", 104547, "Japan Registry of Clinical Trials");
        }
        else if (processed_id.StartsWith("UMIN"))
        {
            processed_id = processed_id.Replace("ID", "").Replace("No", "").Replace(":", "").Replace(".  ", "").Trim();
            return new StudyIdentifier(sid, processed_id, 11, "Trial Registry ID", 100156, "University Hospital Medical Information Network CTR");
        }
        else
        {
            if (sponsor_is_org == true)
            {
                return new StudyIdentifier(sid, processed_id, 14, "Sponsor ID", null, sponsor_name);
            }
            else
            {
                return new StudyIdentifier(sid, processed_id, 14, "Sponsor ID", 12, "No organisation name provided in source data");
            }
        }
    }



}
