namespace MDR_Harvester.Euctr;

public static class EuctrExtensions
{
    public static bool NameAlreadyPresent(this string candidate_name, List<StudyTitle> titles)
    {
        if (titles.Count == 0)
        {
            return false;
        }
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

    public static bool IMPAlreadyThere(this string imp_name, List<StudyTopic> topics)
    {
        if (topics.Count == 0)
        {
            return false;
        }
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
}
  
