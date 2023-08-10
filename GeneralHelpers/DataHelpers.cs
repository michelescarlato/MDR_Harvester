namespace MDR_Harvester.Extensions;

public static class DataHelpers
{
    public static string? StandardisePharmaName(this string? org_name)
    {
        if (string.IsNullOrEmpty(org_name))
        {
            return null;
        }
        string org_lower = org_name.ToLower();
        
        if (org_lower.Contains("univers") || org_lower.Contains("hospit")
            || org_lower.Contains("school") || org_lower.Contains("college")
            || org_lower.Contains("medical center") || org_lower.Contains("medical centre")
            || org_lower.Contains("nation"))
        {
            return org_name; // skip some obvious non pharma
        }

        bool matched = false;
        char FL = org_name.ToUpper()[0];
        switch (FL)
        {
            case 'A':
            {
                if (org_lower.StartsWith("astrazen"))
                {
                    org_name = "AstraZeneca";
                    matched = true;
                }
                if (org_lower.StartsWith("abbvie"))
                {
                    org_name = "AbbVie";
                    matched = true;
                }
                if (org_lower.StartsWith("astellas"))
                {
                    org_name = "Astellas Pharma";
                    matched = true;
                }
                if (org_lower.StartsWith("amgen "))
                {
                    org_name = "Amgen";
                    matched = true;
                }
                break;
            }
            case 'B':
            {
                if (org_lower.StartsWith("bayer "))
                {
                    org_name = "Bayer";
                    matched = true;
                }
                if (org_lower.StartsWith("boehringer"))
                {
                    org_name = "Boehringer Ingelheim";
                    matched = true;
                }
                if (org_lower.StartsWith("biontech"))
                {
                    org_name = "BioNTech";
                    matched = true;
                }
                if (org_lower.StartsWith("biogen "))
                {
                    org_name = "Biogen";
                    matched = true;
                }
                if (org_lower.StartsWith("bristol") && org_lower.Contains("myers"))
                {
                    org_name = "Bristol-Myers Squibb";
                    matched = true;
                }
                if (org_lower == "bms")
                {
                    org_name = "Bristol-Myers Squibb";
                    matched = true;
                }
                break;
            }
            case 'C':
            {
                if (org_lower.StartsWith("cliantha"))
                {
                    org_name = "Cliantha Research";
                    matched = true;
                }
                break;
            }
            case 'E':
            {
                if (org_lower.StartsWith("eli lilly"))
                {
                    org_name = "Eli Lilly";
                    matched = true;
                }
                if (org_lower.StartsWith("emd serono"))
                {
                    org_name = "Merck Group";
                    matched = true;
                }
                break;
            }
            case 'G':
            {
                if (org_lower.StartsWith("gsk") || org_lower.StartsWith("glaxo"))
                {
                    org_name = "GlaxoSmithKline";
                    matched = true;
                }
                if (org_lower.StartsWith("gilead "))
                {
                    org_name = "Gilead Sciences";
                    matched = true;
                }
                break;
            }
            case 'J':
            {
                if (org_lower.StartsWith("johnson & johnson"))
                {
                    org_name = "Johnson & Johnson";
                    matched = true;
                }
                break;
            }
            case 'L':
            {
                if (org_lower.StartsWith("leadiant"))
                {
                    org_name = "Leadiant Biosciences";
                    matched = true;
                }
                break;
            }
            case 'M':
            {
                if (org_lower =="msd" || org_lower.StartsWith("msd "))
                {
                    org_name = "Merck Sharp & Dohme";
                    matched = true;
                }
                if (org_lower.StartsWith("moderna"))
                {
                    org_name = "Moderna";
                    matched = true;
                }
                break;
            }
            case 'N':
            {    
                if (org_lower.StartsWith("novartis"))
                {
                    org_name = "Novartis";
                    matched = true;
                }
                if (org_lower.StartsWith("novo nordisk") && 
                    !org_lower.Contains("foundation") && !org_lower.Contains("fonden"))
                {
                    org_name = "Novo Nordisk";
                    matched = true;
                }
                break;
            }
            case 'P':
            {
                if (org_lower.StartsWith("pfizer") && !org_lower.Contains("viatris"))
                {
                    org_name = "Pfizer";
                    matched = true;
                }
                break;
            }
            case 'R':
            {
                if (org_lower == "roche" || org_lower.StartsWith("roche "))
                {
                    org_name = "Hoffmann-La Roche";
                    matched = true;
                }
                if (org_lower.StartsWith("regeneron"))
                {
                    org_name = "Regeneron";
                    matched = true;
                }
                break;
            }
            case 'S':
            {
                if (org_lower.StartsWith("sanofi"))
                {
                    if (org_lower.Contains("pasteur"))
                    {
                        org_name = "Sanofi Pasteur";
                        
                    }
                    else
                    {
                        org_name = "Sanofi";
                    }
                    matched = true;
                }
                if (org_lower.StartsWith("sigma-tau"))
                {
                    org_name = "Leadiant Biosciences";
                    matched = true;
                }
                break;
            }
            case 'T':
            {
                if (org_lower.StartsWith("takeda"))
                {
                    org_name = "Takeda";
                    matched = true;
                }
                break;
            }
            case 'W':
            {
                if (org_lower.StartsWith("wyeth"))
                {
                    org_name = "Wyeth (now part of Pfizer)";
                    matched = true;
                }
                break;
            }
        }
        if (matched)
        {
            return org_name;
        }
        
        // These company names sometimes not at the beginning of the organisation name
        
        if (org_lower.Contains("janssen"))
        {
            org_name = "Janssen Pharmaceuticals";
        }
        else if (org_lower.Contains("merck"))
        {
            if (org_lower.Contains("sharp") || org_lower.Contains("& co"))
            {
                org_name = "Merck Sharp & Dohme";
            }
            else if (org_lower.Contains("serono") || org_lower.Contains("kgaa"))
            {
                org_name = "Merck Group";
            }
        }
        else if (org_lower.Contains("hoffmann") && org_lower.Contains("roche"))
        {
            org_name = "Hoffmann-La Roche";
        }
        else if (org_lower.Contains("genentech"))
        {
           org_name = "Genentech (part of Roche)";
        }
        else if (org_lower.Contains("viatris"))
        {
           org_name = "Viatris";
        }

        return org_name;
    }

    public static bool IsUsefulTopic(this string? topic)
    {
        if (string.IsNullOrEmpty(topic))
        {
            return false;
        }
        topic = topic.Trim();
        if (topic == "-")
        {
            return false;
        }

        string t_lower = topic.ToLower().Trim();
        bool is_useful = true;
        char FL = topic.ToUpper()[0];
        switch (FL)
        {
            case 'A':
            {
                if (t_lower is "adolescent" or "adolescents")
                {
                    is_useful = false;
                }
                break;
            }
            case 'B':
            {
                if (t_lower is "body weight")
                {
                    is_useful = false;
                }
                break;
            }
            case 'C':
            {
                if (t_lower is "child" or "chronic disease" or "clinical research/ practice" or "complication" 
                    or "complications" or "constriction, pathologic" or "critical illness" or "critically ill patients")
                {
                    is_useful = false;
                }
                break;
            }
            case 'D':
            {
                if (t_lower is "disease")
                {
                    is_useful = false;
                }
                break;
            }
            case 'E':
            {
                if (t_lower is "emergencies" or "evaluation")
                {
                    is_useful = false;
                }
                break;
            }
            case 'F':
            {
                if (t_lower is "female" or "fibrosis" or "follow-up" or "function")
                {
                    is_useful = false;
                }
                break;
            }
            case 'H':
            {
                if (t_lower.StartsWith("healthy"))
                {
                    if (t_lower is "healthy" or "healthy adults" or "healthy adult" or "healthy person" or 
                        "healthy people" or "healthy adult female" or "healthy adult male" or "healthy volunteer" or 
                        "healthy volunteers" or "healthy individual" or "healthy individuals" or 
                        "healthy older adults" or "healthy control" or "healthy japanese subjects")
                    {
                        is_useful = false;
                    }
                }
                else
                {
                    if (t_lower is "human" or "humans" or "hv" or "health condition 1: o- medical and surgical")
                    {
                        is_useful = false;
                    }
                }
                break;
            }
            case 'I':
            {
                if (t_lower is "infarction" or "inflammation" or "ischemia" or "intervention" or "implementation")
                {
                    is_useful = false;
                }
                break;
            }
            case 'M':
            {
                if (t_lower is "men" or "male" or "management")
                {
                    is_useful = false;
                }
                break;
            }
            case 'N':
            {
                if (t_lower is "n/a" or "not applicable" or "n/a(healthy adults)" or "n/a (healthy adults)" 
                    or "none (healthy adults)" or "normal control")
                {
                    is_useful = false;
                }
                break;
            }
            case 'O':
            {
                if (t_lower is "other")
                {
                    is_useful = false;
                }
                break;
            }
            case 'P':
            {
                if (t_lower is "physical activity" or "physical function" or "physical inactivity" or "prediction" 
                    or "prep" or "process evaluation" or "predictors" or "public health" or 
                    "public health - epidemiology" or "public health - health promotion/education" or "normal control")
                {
                    is_useful = false;
                }
                break;
            }
            case 'Q':
            {
                if (t_lower is "quality of life")
                {
                    is_useful = false;
                }
                break;
            }
            case 'R':
            {
                if (t_lower is "recovery" or "refractory" or "relapsed" or "recurrence")
                {
                    is_useful = false;
                }
                break;
            }
            case 'S':
            {
                if (t_lower is "sclerosis" or "sleep" or "studies" or "symptoms" or "surgery" or "syndrome")
                {
                    is_useful = false;
                }
                break;
            }
            case 'T':
            {
                if (t_lower is "tolerability" or "training" or "thrombosis" or "toxicity")
                {
                    is_useful = false;
                }
                break;
            }
            case 'U':
            {
                if (t_lower is "ulcer")
                {
                    is_useful = false;
                }
                break;
            }
            case 'V':
            {
                if (t_lower is "validation" or "volunteer" or "volunteers")
                {
                    is_useful = false;
                }
                break;
            }
            case 'W':
            {
                if (t_lower is "women")
                {
                    is_useful = false;
                }
                break;
            }
 

        }

        return is_useful;
    }
}