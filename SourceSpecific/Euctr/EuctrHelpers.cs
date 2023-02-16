namespace MDR_Harvester.Euctr;

public static class EuctrExtensions
{
    public static bool NameAlreadyPresent(this string candidate_name, List<StudyTitle> titles)
    {
        if (titles.Count == 0)
        {
            return false;
        }
        else
        {
            bool res = false;
            foreach (StudyTitle t in titles)
            {
                if (t.title_text?.ToLower() == candidate_name.ToLower())
                {
                    res = true;
                    break;
                }
            }

            return res;
        }
    }

    public static bool IMPAlreadyThere(this string imp_name, List<StudyTopic> topics)
    {
        if (topics.Count == 0)
        {
            return false;
        }
        else
        {
            bool res = false;
            foreach (StudyTopic t in topics)
            {
                if (imp_name.ToLower() == t.original_value?.ToLower())
                {
                    res = true;
                    break;
                }
            }

            return res;
        }
    }

    public static string? GetLanguageFromMemberState(this string? member_state)
    {
        if (string.IsNullOrEmpty(member_state))
        {
            return null;
        }

        string ms_lc = member_state.ToLower();
        string sec_lang = ms_lc switch
        {
            _ when ms_lc.Contains("spain")
                   || ms_lc.Contains("span") => "es",
            _ when ms_lc.Contains("portug") => "pt",
            _ when ms_lc.Contains("france")
                   || ms_lc.Contains("french") => "fr",
            _ when ms_lc.Contains("german")
                   || ms_lc.Contains("liecht")
                   || ms_lc.Contains("austri") => "de",
            _ when ms_lc.Contains("ital") => "it",
            _ when ms_lc.Contains("dutch")
                   || ms_lc.Contains("neder")
                   || ms_lc.Contains("nether") => "nl",
            _ when ms_lc.Contains("danish")
                   || ms_lc.Contains("denm") => "da",
            _ when ms_lc.Contains("swed") => "sv",
            _ when ms_lc.Contains("norw") => "no",
            _ when ms_lc.Contains("fin") => "fi",
            _ when ms_lc.Contains("icelan") => "is",
            _ when ms_lc.Contains("polish") => "pl",
            _ when ms_lc.Contains("hungar") => "hu",
            _ when ms_lc.Contains("czech") => "cs",
            _ when ms_lc.Contains("slovak") => "sk",
            _ when ms_lc.Contains("sloven") => "sl",
            _ when ms_lc.Contains("greece")
                   || ms_lc.Contains("greek")
                   || ms_lc.Contains("cypr") => "el",
            _ when ms_lc.Contains("eston") => "et",
            _ when ms_lc.Contains("latv") => "lv",
            _ when ms_lc.Contains("lithu") => "lt",
            _ when ms_lc.Contains("croat") => "hr",
            _ when ms_lc.Contains("roman") => "ro",
            _ when ms_lc.Contains("bulga") => "bg",
            _ => "??"
        };

        return sec_lang;
    }
    
    public static bool IsAnIndividual(this string? org_name)
    {
        if (string.IsNullOrEmpty(org_name))
        {
            return false;
        }
        
        bool is_an_individual = false;

        // if looks like an individual's name
        if (org_name.EndsWith(" md") || org_name.EndsWith(" phd") ||
            org_name.Contains(" md,") || org_name.Contains(" md ") ||
            org_name.Contains(" phd,") || org_name.Contains(" phd ") ||
            org_name.Contains("dr ") || org_name.Contains("dr.") ||
            org_name.Contains("prof ") || org_name.Contains("prof.") ||
            org_name.Contains("professor"))
        {
            is_an_individual = true;
            
            // but if part of a organisation reference...
            
            if (org_name.Contains("hosp") || org_name.Contains("univer") ||
                org_name.Contains("labor") || org_name.Contains("labat") ||
                org_name.Contains("institu") || org_name.Contains("istitu") ||
                org_name.Contains("school") || org_name.Contains("founda") ||
                org_name.Contains("associat"))

            {
                is_an_individual = false;
            }
        }

        // some specific individuals...
        if (org_name == "seung-jung park" || org_name == "kang yan")
        {
            is_an_individual = true;
        }
        return is_an_individual;
    }
    
    public static bool IsAnOrganisation(this string? full_name)
    {
        if (string.IsNullOrEmpty(full_name))
        {
            return false;
        }
        
        bool is_an_organisation = false;
        string f_name = full_name.ToLower();
        if (f_name.Contains(" group") || f_name.StartsWith("group") ||
            f_name.Contains(" assoc") || f_name.Contains(" team") ||
            f_name.Contains("collab") || f_name.Contains("network"))
        {
            is_an_organisation = true;
        }
        return is_an_organisation;
    }
}
  
