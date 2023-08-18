using System.Globalization;
using System.Text.RegularExpressions;

namespace MDR_Harvester.Who;

internal static class WhoHelpers
{
    internal static string? GetSourceName(this int? source_id)
    {
        if (!source_id.HasValue)
        {
            return null;
        }

        return source_id switch
        {
            100115 => "International Clinical Trial Registry Platform",
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
            101989 => "Lebanon Clinical Trial Registry",
            104545 => "Chinese Medicine Clinical Trials Registry",
            109108 => "International Traditional Medicine Clinical Trial Registry",
            102000 => "Anvisa (Brazil)",
            102001 => "Comitê de Ética em Pesquisa (local) (Brazil)",
            _ => null
        };
    }


    internal static string? GetRegistryPrefix(this int? source_id)
    {
        // Used for WHO registries only
        
        if (!source_id.HasValue)
        {
            return null;
        }
        
        return source_id switch
        {
            100116 => "Australian / NZ ",
            100117 => "Brazilian ",
            100118 => "Chinese ",
            100119 => "South Korean ",
            100121 => "Indian ",
            100122 => "Cuban ",
            100124 => "German ",
            100125 => "Iranian ",
            100127 => "Japanese ",
            100128 => "Pan African ",
            100129 => "Peruvian ",
            100130 => "Sri Lankan ",
            100131 => "Thai ",
            100132 => "Dutch ",
            101989 => "Lebanese ",
            109108 => "Traditional Medicine ",
            _ => null
        };
    }
    

    internal static StudyIdentifier TryToGetANZIdentifier(this string sid, string processed_id, 
                                                   bool? sponsor_is_org, string sponsor_name)
    {
        // australian nz identifiers
        if (processed_id.StartsWith("ADHB"))
        {
            return new StudyIdentifier(sid, processed_id, 41, "Regulatory body ID", 104531, "Aukland District Health Board");
        }

        if (processed_id.StartsWith("Auckland District Health Board"))
        {
            processed_id = processed_id.Replace("Auckland District Health Board", "");
            processed_id = processed_id.Replace("registration", "").Replace("research", "");
            processed_id = processed_id.Replace("number", "").Replace(":", "").Trim();
            return new StudyIdentifier(sid, processed_id, 41, "Regulatory body ID", 104531, "Aukland District Health Board");
        }

        if (processed_id.StartsWith("AG01") || processed_id.StartsWith("AG02") || processed_id.StartsWith("AG03")
            || processed_id.StartsWith("AG101") || processed_id.StartsWith("AGITG"))
        {
            return new StudyIdentifier(sid, processed_id, 43, "Collaborative Group ID", 104532, "Australian Gastrointestinal Trials Group");
        }

        if (processed_id.Contains("Australasian Gastro-Intestinal Trials Group: "))
        {
            processed_id = processed_id.Replace("Australasian Gastro-Intestinal Trials Group:", "");
            processed_id = processed_id.Replace("The", "").Replace("(AGITG)", "");
            processed_id = processed_id.Replace("Protocol No:", "").Trim();
            return new StudyIdentifier(sid, processed_id, 43, "Collaborative Group ID", 104532, "Australian Gastrointestinal Trials Group");
        }

        if (processed_id.StartsWith("ALLG") || processed_id.StartsWith("AMLM"))
        {
            return new StudyIdentifier(sid, processed_id, 43, "Collaborative Group ID", 103289, "Australasian Leukaemia and Lymphoma Group");
        }

        if (processed_id.Contains("Australasian Leukaemia and Lymphoma Group"))
        {
            processed_id = processed_id.Replace("Australasian Leukaemia and Lymphoma Group", "");
            processed_id = processed_id.Replace("The", "").Replace("(ALLG):", "");
            processed_id = processed_id.Replace(":", "").Replace("-", "").Trim();
            return new StudyIdentifier(sid, processed_id, 43, "Collaborative Group ID", 103289, "Australasian Leukaemia and Lymphoma Group");
        }

        if (processed_id.StartsWith("ANZGOG"))
        {
            return new StudyIdentifier(sid, processed_id, 43, "Collaborative Group ID", 104533, "Australia New Zealand Gynaecological Oncology Group");
        }

        if (processed_id.StartsWith("Australia and New Zealand Gynecological Oncology Group: "))
        {
            processed_id = processed_id.Replace("Australia and New Zealand Gynecological Oncology Group:", "").Trim();
            return new StudyIdentifier(sid, processed_id, 43, "Collaborative Group ID", 104533, "Australia New Zealand Gynaecological Oncology Group");
        }

        if (processed_id.StartsWith("Australasian Sarcoma Study Group Number"))
        {
            processed_id = processed_id.Replace("Australasian Sarcoma Study Group Number", "").Trim();
            return new StudyIdentifier(sid, processed_id, 43, "Collaborative Group ID", 104534, "Australasian Sarcoma Study Group");
        }

        if (processed_id.StartsWith("ANZMTG"))
        {
            return new StudyIdentifier(sid, processed_id, 43, "Collaborative Group ID", 104535, "Australia and New Zealand Melanoma Trials Group");
        }

        if (processed_id.StartsWith("ANZUP"))
        {
            return new StudyIdentifier(sid, processed_id, 43, "Collaborative Group ID", 104536, "Australian and New Zealand Urogenital and Prostate Cancer Trials Group");
        }

        if (processed_id.StartsWith("APP") || processed_id.StartsWith("GNT"))
        {
            return new StudyIdentifier(sid, processed_id, 13, "Funder’s ID", 100690, "National Health and Medical Research Council, Australia");
        }

        if (processed_id.StartsWith("Australian NH&MRC"))
        {
            processed_id = processed_id.Replace("Australian NH&MRC", "");
            processed_id = processed_id.Replace("Project", "").Replace("Grant", "");
            processed_id = processed_id.Replace("Targeted Call for Research", "").Trim();

            return new StudyIdentifier(sid, processed_id, 13, "Funder’s ID", 100690, "National Health and Medical Research Council, Australia");
        }

        if (processed_id.StartsWith("National Health and Medical Research Council") ||
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

        if (processed_id.StartsWith("NHMRCC"))
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

        if (processed_id.StartsWith("Australian Research Council"))
        {
            processed_id = processed_id.Replace("Australian Research Council", "");
            processed_id = processed_id.Replace("(", "").Replace(")", "").Trim();

            return new StudyIdentifier(sid, processed_id, 13, "Funder’s ID", 104537, "Australian Research Council");
        }

        if (processed_id.StartsWith("Australian Therapeutic Goods Administration"))
        {
            processed_id = processed_id.Replace("Australian Therapeutic Goods Administration", "");
            processed_id = processed_id.Replace("Clinical Trial", "").Replace("TGA", "");
            processed_id = processed_id.Replace("CTN", "").Replace("(", "").Replace(")", "");
            processed_id = processed_id.Replace("number", "", StringComparison.CurrentCultureIgnoreCase);
            processed_id = processed_id.Replace("-", "").Replace(".", "").Replace("Notification", "").Trim();

            return new StudyIdentifier(sid, processed_id, 41, "Regulatory body ID", 104538, "Australian Therapeutic Goods Administration CTN");
        }

        if (processed_id.Contains("Clinical Trial") && processed_id.Contains("CTN"))
        {
            processed_id = processed_id.Replace("Clinical Trial", "").Replace("TGA", "");
            processed_id = processed_id.Replace("CTN", "").Replace("(", "").Replace(")", "").Replace(":", "");
            processed_id = processed_id.Replace("Network", "").Replace("Notification", "").Trim();

            return new StudyIdentifier(sid, processed_id, 41, "Regulatory body ID", 104538, "Australian Therapeutic Goods Administration CTN");
        }

        if (processed_id.StartsWith("TGA"))
        {
            processed_id = processed_id.Replace("Clinical", "").Replace("TGA", "").Replace("CTN", "");
            processed_id = processed_id.Replace("trial", "", StringComparison.CurrentCultureIgnoreCase)
                                        .Replace("trials", "", StringComparison.CurrentCultureIgnoreCase);
            processed_id = processed_id.Replace("number", "", StringComparison.CurrentCultureIgnoreCase);
            processed_id = processed_id.Replace(":", "").Replace("Notification", "").Trim();

            return new StudyIdentifier(sid, processed_id, 41, "Regulatory body ID", 104538, "Australian Therapeutic Goods Administration CTN");
        }

        if (processed_id.StartsWith("Therapeutic good administration") ||
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

        if (processed_id.StartsWith("CRG") || processed_id.StartsWith("Cochrane Renal Group"))
        {
            processed_id = processed_id.Replace("Cochrane Renal Group", "").Replace("CRG", "");
            processed_id = processed_id.Replace("(", "").Replace(")", "").Replace("-", "").Replace(".", "").Replace(":", "").Trim();

            return new StudyIdentifier(sid, "CRG" + processed_id, 43, "Collaborative Group ID", 104539, "Cochrane Renal Group");
        }

        if (processed_id.StartsWith("Commonwealth Scientific and Industrial Research Organisation"))
        {
            processed_id = processed_id.Replace("Commonwealth Scientific and Industrial Research Organisation", "");
            processed_id = processed_id.Replace("(CSIRO)", "").Replace(":", "").Replace("and", "").Trim();

            return new StudyIdentifier(sid, processed_id, 13, "Funder’s ID", 104540, "Commonwealth Scientific and Industrial Research Organisation");
        }

        if (processed_id.StartsWith("Health Research Council") && !processed_id.Contains("Funding"))
        {
            processed_id = processed_id.Replace("Health Research Council", "");
            processed_id = processed_id.Replace("of New Zealand", "");
            processed_id = processed_id.Replace("NZ", "").Replace("HRC", "");
            processed_id = processed_id.Replace("reference", "", StringComparison.CurrentCultureIgnoreCase).Replace("Ref.", "");
            processed_id = processed_id.Replace("number", "", StringComparison.CurrentCultureIgnoreCase).Replace("programme", "");
            processed_id = processed_id.Replace("grant", "").Trim();

            return new StudyIdentifier(sid, processed_id, 13, "Funder’s ID", 104541, "Health Research Council of New Zealand");
        }

        if (processed_id.StartsWith("HRC"))
        {
            processed_id = processed_id.Replace("HRC", "");
            processed_id = processed_id.Replace("Emerging Research First Grant- ", "");
            processed_id = processed_id.Replace("Project Grant Number #", "");
            processed_id = processed_id.Replace("Ref:", "").Trim();

            return new StudyIdentifier(sid, processed_id, 13, "Funder’s ID", 104541, "Health Research Council of New Zealand");
        }

        if (processed_id.StartsWith("HREC"))
        {
            return sponsor_is_org is true 
                           ? new StudyIdentifier(sid, processed_id, 12, "Ethics review ID", null, sponsor_name) 
                           : new StudyIdentifier(sid, processed_id, 12, "Ethics review ID", 12, "No organisation name provided in source data");
        }

        if (processed_id.StartsWith("Human Research Ethics Committee (HREC):"))
        {
            // ??? change type and keep sponsor
            processed_id = processed_id.Replace("Human Research Ethics Committee (HREC):", "");

            return sponsor_is_org is true 
                           ? new StudyIdentifier(sid, processed_id, 12, "Ethics review ID", null, sponsor_name) 
                           : new StudyIdentifier(sid, processed_id, 12, "Ethics review ID", 12, "No organisation name provided in source data");
        }

        if (processed_id.StartsWith("MRINZ"))
        {
            return new StudyIdentifier(sid, processed_id, 43, "Collaborative Group ID", 103010, "Medical Research Institute of New Zealand");
        }

        if (processed_id.StartsWith("National Clinical Trials Registry"))
        {
            processed_id = processed_id.Replace("National Clinical Trials Registry:", "").Trim();
            return new StudyIdentifier(sid, processed_id, 11, "Trial registry ID", 104548, "National Clinical Trials Registry (Australia)");
        }
        
        if (processed_id.StartsWith("Perinatal Trials Registry:"))
        {
            processed_id = processed_id.Replace("Perinatal Trials Registry:", "").Trim();
            return new StudyIdentifier(sid, processed_id, 11, "Trial registry ID", 104542, "Perinatal Trials Registry (Australia)");
        }

        if (processed_id.StartsWith("TROG"))
        {
            return new StudyIdentifier(sid, processed_id, 43, "Collaborative Group ID", 104543, "Trans Tasman Radiation Oncology Group");
        }

        return sponsor_is_org is true 
                        ? new StudyIdentifier(sid, processed_id, 14, "Sponsor ID", null, sponsor_name) 
                        : new StudyIdentifier(sid, processed_id, 14, "Sponsor ID", 12, "No organisation name provided in source data");
    }


    internal static StudyIdentifier? TryToGetChineseIdentifier(this string sid, string processed_id, 
                                                        bool? sponsor_is_org, string sponsor_name)
    {
        if (processed_id.EndsWith("#32"))    // first ignore these (small sub group)
        {
            return null;
        }
        
        if (processed_id.StartsWith("AMCTR"))
        {
            return new StudyIdentifier(sid, processed_id, 11, "Trial Registry ID", 104544, "Acupuncture-Moxibustion Clinical Trial Registry");
        }
        if (processed_id.StartsWith("ChiMCTR"))
        {
            return new StudyIdentifier(sid, processed_id, 11, "Trial Registry ID", 104545, "Chinese Medicine Clinical Trials Registry");
        }
        if (processed_id.StartsWith("CUHK"))
        {
            return new StudyIdentifier(sid, processed_id, 11, "Trial Registry ID", 104546, "CUHK Clinical Research and Biostatistics Clinical Trials Registry");
        }

        return sponsor_is_org is true 
                        ? new StudyIdentifier(sid, processed_id, 14, "Sponsor ID", null, sponsor_name) 
                        : new StudyIdentifier(sid, processed_id, 14, "Sponsor ID", 12, "No organisation name provided in source data");
    }


    internal static StudyIdentifier TryToGetJapaneseIdentifier(this string sid, string processed_id, 
                                                        bool? sponsor_is_org, string sponsor_name)
    {
        if (processed_id.StartsWith("JapicCTI"))
        {
            return new StudyIdentifier(sid, processed_id, 11, "Trial Registry ID", 100157, "Japan Pharmaceutical Information Center");
        }
        if (processed_id.StartsWith("JMA"))
        {
            return new StudyIdentifier(sid, processed_id, 11, "Trial Registry ID", 100158, "Japan Medical Association – Center for Clinical Trials");
        }
        if (processed_id.StartsWith("jCRT") || processed_id.StartsWith("JCRT"))
        {
            return new StudyIdentifier(sid, processed_id, 11, "Trial Registry ID", 104547, "Japan Registry of Clinical Trials");
        }
        if (processed_id.StartsWith("UMIN"))
        {
            processed_id = processed_id.Replace("ID", "").Replace("No", "").Replace(":", "").Replace(".  ", "").Trim();
            return new StudyIdentifier(sid, processed_id, 11, "Trial Registry ID", 100156, "University Hospital Medical Information Network CTR");
        }
        
        return sponsor_is_org is true 
                       ? new StudyIdentifier(sid, processed_id, 14, "Sponsor ID", null, sponsor_name) 
                       : new StudyIdentifier(sid, processed_id, 14, "Sponsor ID", 12, "No organisation name provided in source data");
    }
    
    
    internal static List<string> SplitNTRIdString(this string input_string, string splitter)
    {
        List<string> possible_ids = new();
        string[] sections = input_string.Split(splitter);
        if (sections.Length == 2)
        {
            sections[0] = sections[0].Trim('(', ')', ' ');
            if (sections[0].ToLower() != "ccmo" && sections[0].ToLower() != "abr")
            {
                possible_ids.Add(sections[0].Trim());
            }
            sections[1] = sections[1].Trim('(', ')', ' ');
            if (sections[1].ToLower() != "ccmo" && sections[1].ToLower() != "abr")
            {
                possible_ids.Add(sections[1].Trim());
            }
        }
        else if (sections.Length == 3 && sections[1].Contains(':'))
        {
            int colon_pos = sections[1].IndexOf(':');
            string part_1 = sections[0] + " : " + sections[1][(colon_pos + 1)..];
            string part_2 = sections[1][..colon_pos] + " : " + sections[2];
            possible_ids.Add(part_1.Trim());
            possible_ids.Add(part_2.Trim());

        }
        else
        {
            foreach (string sec in sections)
            {
                if (sec.ToLower() != "ccmo" && sec.ToLower() != "abr")
                {
                    possible_ids.Add(sec.Trim());
                }
            }
        }
        return possible_ids;
    }
    
    
    
    internal static StudyIdentifier? TryToGetDutchIdentifier(this string sid, string processed_id, 
                                                     bool? sponsor_is_org, string sponsor_name)
    {
        processed_id = processed_id.Replace("dossiernummer", "").Trim();
        processed_id = processed_id.Replace("dossiernr", "").Trim();
        processed_id = processed_id.Replace("project", "").Trim();
        processed_id = processed_id.Replace("research file", "").Trim();
        processed_id = processed_id.Replace("number", "").Trim();
        processed_id = processed_id.Replace("nummer", "").Trim();
        processed_id = processed_id.Replace("nr.", "").Trim();
        processed_id = processed_id.Replace("of the financer", "").Trim();
        processed_id = processed_id.Replace("Toetsingonline", "").Trim();
        processed_id = processed_id.Trim('-', ':', ' ', '/', '*', '.');

        if (processed_id.Length < 3 || processed_id == "001" || processed_id == "0001")
        {
            return null;
        }
        
        if (Regex.Match(processed_id, @"^NL(\s?|-?|\.?)\d{5}\.\d{3}\.\d{2}").Success)
        {
            return new StudyIdentifier(sid, processed_id, 41, "Regulatory Body ID", 109113, "CCMO");
        }
        if (processed_id.Contains("CCMO", StringComparison.OrdinalIgnoreCase))
        {
            processed_id = processed_id.Replace("CCMO", "").Trim();
            processed_id = processed_id.Trim(' ', ':', '-');
            return new StudyIdentifier(sid, processed_id, 41, "Regulatory Body ID", 109113, "CCMO");
        }
        if (processed_id.Contains("ABR", StringComparison.OrdinalIgnoreCase))
        {
            processed_id = processed_id.Replace("ABR", "").Trim();
            processed_id = processed_id.Trim(' ', ':', '-');
            return new StudyIdentifier(sid, processed_id, 41, "Regulatory Body ID", 109113, "CCMO");
        }
        if (processed_id.Contains("ZonMw", StringComparison.OrdinalIgnoreCase))
        {
            processed_id = processed_id.Replace("ZonMw", "", StringComparison.OrdinalIgnoreCase).Trim();
            processed_id = processed_id.Trim(' ', ':', '-');
            return new StudyIdentifier(sid, processed_id, 13, "Funder’s ID", 109113, "ZonMw");
        }

        if (processed_id.Contains("MEC-U", StringComparison.OrdinalIgnoreCase))
        {
            processed_id = processed_id.Replace("MEC-UC", "METC-U", StringComparison.OrdinalIgnoreCase).Trim();
        }
        if (processed_id.Contains("MEC AMC", StringComparison.OrdinalIgnoreCase))
        {
            processed_id = processed_id.Replace("MEC AMC", "METC AMC", StringComparison.OrdinalIgnoreCase).Trim();
        }
        if (processed_id.Contains("MEC azM/UM", StringComparison.OrdinalIgnoreCase))
        {
            processed_id = processed_id.Replace("MEC azM/UM", "METC azM/UM", StringComparison.OrdinalIgnoreCase).Trim();
        }
        if (processed_id.Contains("MEC Brabant", StringComparison.OrdinalIgnoreCase))
        {
            processed_id = processed_id.Replace("MEC Brabant", "METC Brabant", StringComparison.OrdinalIgnoreCase).Trim();
        }
        if (processed_id.Contains("MEC ErasmusMC", StringComparison.OrdinalIgnoreCase))
        {
            processed_id = processed_id.Replace("MEC ErasmusMC", "METC ErasmusMC", StringComparison.OrdinalIgnoreCase).Trim();
        }
        if (processed_id.Contains("MEC Isala", StringComparison.OrdinalIgnoreCase))
        {
            processed_id = processed_id.Replace("MEC Isala", "METC Isala", StringComparison.OrdinalIgnoreCase).Trim();
        }
        if (processed_id.Contains("MEC Leiden", StringComparison.OrdinalIgnoreCase))
        {
            processed_id = processed_id.Replace("MEC Leiden", "METC Leiden", StringComparison.OrdinalIgnoreCase).Trim();
        }
        
        if (processed_id.ToLower().Contains("METC-U", StringComparison.OrdinalIgnoreCase))
        {
            processed_id = processed_id.Replace("METC-U", "", StringComparison.OrdinalIgnoreCase).Trim();
            processed_id = processed_id.Trim(' ', ':', '-');
            return new StudyIdentifier(sid, processed_id, 12, "Ethics review ID", 109115, "Medical Research Ethics Committees United");
        }
        if (processed_id.ToLower().Contains("METC AMC", StringComparison.OrdinalIgnoreCase))
        {
            processed_id = processed_id.Replace("METC AMC", "", StringComparison.OrdinalIgnoreCase).Trim();
            processed_id = processed_id.Trim(' ', ':', '-');
            return new StudyIdentifier(sid, processed_id, 12, "Ethics review ID", 109116, "METC Amsterdam UMC");
        }
        if (processed_id.ToLower().Contains("METC azM/UM", StringComparison.OrdinalIgnoreCase))
        {
            processed_id = processed_id.Replace("METC azM/UM", "", StringComparison.OrdinalIgnoreCase).Trim();
            processed_id = processed_id.Trim(' ', ':', '-');
            return new StudyIdentifier(sid, processed_id, 12, "Ethics review ID", 109117, "METC Academisch Ziekenhuis Maastricht / Universiteit Maastricht");
        }
        if (processed_id.ToLower().Contains("METC Brabant", StringComparison.OrdinalIgnoreCase))
        {
            processed_id = processed_id.Replace("METC Brabant ", "", StringComparison.OrdinalIgnoreCase).Trim();
            processed_id = processed_id.Trim(' ', ':', '-');
            return new StudyIdentifier(sid, processed_id, 12, "Ethics review ID", 109118, "METC Brabant");
        }
        if (processed_id.ToLower().Contains("METC ErasmusMC", StringComparison.OrdinalIgnoreCase))
        {
            processed_id = processed_id.Replace("METC ErasmusMC", "", StringComparison.OrdinalIgnoreCase).Trim();
            processed_id = processed_id.Trim(' ', ':', '-');
            return new StudyIdentifier(sid, processed_id, 12, "Ethics review ID", 109119, "METC Erasmus Medisch Centrum Rotterdam");
        }
        if (processed_id.ToLower().Contains("METC Isala", StringComparison.OrdinalIgnoreCase))
        {
            processed_id = processed_id.Replace("METC Isala", "", StringComparison.OrdinalIgnoreCase).Trim();
            processed_id = processed_id.Trim(' ', ':', '-');
            return new StudyIdentifier(sid, processed_id, 12, "Ethics review ID", 109120, "METC Isala klinieken Zwolle");
        }
        if (processed_id.ToLower().Contains("METC Leiden", StringComparison.OrdinalIgnoreCase))
        {
            processed_id = processed_id.Replace("METC Leiden", "", StringComparison.OrdinalIgnoreCase).Trim();
            processed_id = processed_id.Trim(' ', ':', '-');
            return new StudyIdentifier(sid, processed_id, 12, "Ethics review ID", 109121, "METC Leiden Den Haag Delft");
        }
        if (processed_id.ToLower().Contains("METC", StringComparison.OrdinalIgnoreCase))
        {
            processed_id = processed_id.Replace("METC", "", StringComparison.OrdinalIgnoreCase).Trim();
            processed_id = processed_id.Trim(' ', ':', '-');
            return new StudyIdentifier(sid, processed_id, 12, "Ethics review ID", null, sponsor_name);
        }

        return sponsor_is_org is true 
            ? new StudyIdentifier(sid, processed_id, 14, "Sponsor ID", null, sponsor_name) 
            : new StudyIdentifier(sid, processed_id, 14, "Sponsor ID", 12, "No organisation name provided in source data");
    }
    
    internal static string CheckChineseFunderType(this string funder_name)
    {
        string fname = funder_name.ToLower();
        
        if (fname.StartsWith("self ") || fname.StartsWith("self-")
                                      || fname == "selffinance"
                                      || fname.Contains("raise") && fname.Contains("independently")
                                      || fname.Contains("own expense") || fname.Contains("oneself")
                                      || fname is "fully self-raised" or "independently"
                                          or "independent project")
        {
            return "Reported as self-funded";
        }
        
        if (fname is "authors" or "authors' own work" 
                or "autonomous" or "autonomous financing"
                or "by myself" or "by ourself" or "by ourselves"
                or "by raised" or "by self" or "byself" or "by the PI"
                or "afford self" or "at your own expense"
                or "from our own money" or "individual")
        {
            return "Reported as self-funded";
        }
        
        if (fname is "sponsor" or "sponsor funding" 
            or "provided by the sponsor" or "by study sponsor"
            or "by the sponsor" or "sponsor initiated"
            or "the sponsor")
        {
            return "sponsor";
        }
        
        if (fname is "hospital" or "hospital development center" or 
             "hospital research funding" or "hospital support" or "hospital fund" 
             or "hospital funding" or "hospital funds" or "hospital project fund"
             or "from our hospital" or "from the hospital"
             or "provided by the hospital")
        {
            return "Reported as hospital funded, no further details";
        }

        if (fname is "graduate program" or "graduate funding" or 
                "graduate research funding" or "graduate project funding" or 
                "research funding for postgraduate" or "funds for postgraduates"
                or "funds for postgraduate training" or "postgraduate funds"
                or "postgraduate graduation project" or "postgraduate project")
        {
            return "Reported funded as part of a graduate research program";
        }
        
        if (fname is "company" or "company funding" 
            or "corporate sponsorship" or "by company"
            or "corporate funding")
        {
            return "Reported as commercially funded, no further details";
        }
        
        if (fname is "government" or "government funding" or "government funds" 
            or "central government funding" or "central government"
            or "central government funds" or "central government special funds"
            or "government grants" or "government support"
            or "special funds of the central government"
            or "the government"
            || fname.Contains("government scientific research funding"))
        {
            return "Reported as government funded, no further details";
        }
        
        if (fname is "project funding" or "project funds" 
            or "a special fund" or "project fund"
            or "project support" or "special fund" or "special funds")
        {
            return "Reported as project funding, no further details";
        }
        
        if (fname is "fund" or "funding" 
            or "a topic of one's choice" or "spontaneous" 
            || fname.Contains("appropriations")
            || fname.StartsWith("???"))
        {
            return "";
        }
        
        if (fname.StartsWith("apply ") || fname.StartsWith("applying ")
            || fname == "applying")
        {
            return "Reported as being applied for";
        }
        
        if (fname is "research fund" or "research funding" or 
            "research funds" or "research group funding"
            or "scientific research fund" or "scientific research funds" 
            or "grants for scientific research" or "applied research fund"
            or "from project funding")
        {
            return "Reported as research funding, no further details";
        }
        
        if (fname is "departmental finance" or "departmental funding" 
            or "affiliation" or "department budget" or "dean's fund" 
            or "department funds" or "funds raised by institution"
            or "own unit" or "the unit self")
        {
            return "Reported as institution funding, no further details";
        }
        
        if (fname is "central finance" or "central financial fund" 
                or "central financial funds" or "central fiscal investment"
                or "central fiscal fund")
        {
            return "Reported as centrally funded, no further details";
        }

        return funder_name;
    }

    internal static string[] GetFunders(this string funderList, int? source_id)
    {
        // Can have multiple names separated by semi-colons
        // Cuban funders also sometimes in pairs split by commas or /

        string[] funder_names = funderList.Split(";");  
        if (source_id == 100122 && funder_names.Length == 1)
        {
            string cfn = funder_names[0];        
            List<string> cfnList = new();
            if (cfn.Contains("MINSAP") || cfn.Contains("Public Health"))
            {
                cfnList.Add("Cuban Ministry of Public Health (MINSAP)");
                cfn = cfn.Replace("Cuban Public Ministry of Health (MINSAP)", "", true, CultureInfo.CurrentCulture);
                cfn = cfn.Replace("Cuban Ministry of Public Health (MINSAP)", "", true, CultureInfo.CurrentCulture);
                cfn = cfn.Replace("Ministry of Public Health (MINSAP)", "", true, CultureInfo.CurrentCulture);
                cfn = cfn.Replace("Ministry of Public Health, CUBA", "", true, CultureInfo.CurrentCulture);
                cfn = cfn.Replace("Cuban Ministry of Public Health", "", true, CultureInfo.CurrentCulture);
                cfn = cfn.Trim(' ', ',', '/');
                if (cfn.Length > 6)
                {
                    cfnList.Add(cfn);  // added remainder of line after MINSAP reference removed
                }
            }
            else if (cfn.Contains("BioCubaFarma"))
            {
                cfnList.Add("BioCubaFarma Central Account");
                cfn = cfn.Replace("  ", " ");  // minor tidying
                cfn = cfn.Replace("Central account for BioCubaFarma", "", true, CultureInfo.CurrentCulture);
                cfn = cfn.Replace("Account for BioCubaFarma", "", true, CultureInfo.CurrentCulture);
                cfn = cfn.Replace("BioCubaFarma Central Account", "", true, CultureInfo.CurrentCulture);
                cfn = cfn.Trim(' ', ',', '/');
                if (cfn.Length > 6)
                {
                    cfnList.Add(cfn);  // added remainder of line after BioCubaFarma reference removed
                }
            }
            else
            {
                cfnList.Add(cfn); // leave as original
            }
            funder_names = cfnList.ToArray();
        }
        return funder_names;
    }
    
    internal static List<WhoCondition> CTRIConditions(this List<WhoCondition> condList)
    {
        List<WhoCondition> cList = new();
        foreach (WhoCondition cn in condList)
        {
            string? con = cn.condition;
            if (!string.IsNullOrEmpty(con))
            {
                if (!con.ToLower().Contains("health condition"))
                {
                    cList.Add(cn);   // just add as is - but only a small minority
                }
                else
                {
                    int n = 1;
                    bool end_of_string = false;
                    while (!end_of_string)
                    {
                        string start_con = "health condition " + n;
                        string end_con = "health condition " + (n + 1);
                        if (con.ToLower().Contains(end_con))
                        {
                            int endcon_pos = con.IndexOf(end_con, StringComparison.OrdinalIgnoreCase);
                            string con_string = con[..endcon_pos];
                            con_string = con_string.Replace(start_con, "", StringComparison.OrdinalIgnoreCase);
                            con_string = con_string.Trim(' ', ':');
                            if (!string.IsNullOrEmpty(con_string))
                            {
                                cList.Add(split_condition_details(con_string));
                            }
                            con = con[endcon_pos..];
                        }
                        else
                        {
                            string con_string = con.Replace(start_con, "", StringComparison.OrdinalIgnoreCase);
                            con_string = con_string.Trim(' ', ':');
                            if (!string.IsNullOrEmpty(con_string))
                            {
                                cList.Add(split_condition_details(con_string));
                            }
                            end_of_string = true;
                        }
                        n++;
                    }
                }
            }
        }
        return cList;
    }

    internal static WhoCondition split_condition_details(string con_string)
    {
        string? cond_name, cond_code = null, code_system = null;
        if (con_string.Contains('-'))    // already trimmed in calling proc
        {
            int dash_pos = con_string.IndexOf('-');
            if (dash_pos == con_string.Length - 1)
            {
                // dash is last character 
                return new WhoCondition(con_string[..dash_pos], cond_code, code_system);  
            }
            cond_code = con_string[..dash_pos].Trim();
            cond_name = con_string[(dash_pos + 1)..].Trim();
            if (!string.IsNullOrEmpty(cond_name) && cond_code.ToLower() != "null")
            {
                if (Regex.Match(cond_code, @"^[A-Z]\d{2}$").Success)
                {
                    code_system = "ICD 10";
                }
                if (Regex.Match(cond_code, @"^[A-Z]\d{3}$").Success)
                {
                    cond_code = cond_code[..3];  // use letter and first 2 digits only
                    code_system = "ICD 10";
                }
                
                if (cond_name.Length >= 5 && Regex.Match(cond_name[..4], @"^[A-Z]\d{2}\-$").Success)
                {
                    // Probably the second part of a X99-X99- coding.
                    // Though not clear what version of ICD is being used!
                    
                    cond_code += "-" + cond_name[..3];
                    cond_name = cond_name[4..].Trim();
                }
            }
            else
            {
                cond_code = null;
            }
        } 
        else
        {
            cond_name = con_string;
        }
        return new WhoCondition(cond_name, cond_code, code_system);
    }

}

