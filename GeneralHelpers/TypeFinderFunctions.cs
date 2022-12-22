using System.Text.RegularExpressions;

namespace DataHarvester
{
    public class TypeHelpers
    {
        public int? GetStatusId(string study_status)
        {
            int? type_id = null;
            switch (study_status.ToLower())
            {
                case "completed": type_id = 21; break;
                case "recruiting": type_id = 14; break;
                case "ongoing": type_id = 25; break;                
                case "active, not recruiting": type_id = 15; break;
                case "not yet recruiting": type_id = 16; break;
                case "unknown status": type_id = 0; break;
                case "withdrawn": type_id = 11; break;
                case "available": type_id = 12; break;
                case "withheld": type_id = 13; break;
                case "no longer available": type_id = 17; break;
                case "suspended": type_id = 18; break;
                case "terminated": type_id = 22; break;
                case "prematurely ended": type_id = 22; break;
                case "enrolling by invitation": type_id = 19; break;
                case "approved for marketing": type_id = 20; break;
                case "other": type_id = 24; break;
                default: type_id = 0; break;
            }
            return type_id;
        }

        public int? GetTypeId(string study_type)
        {
            int? type_id = null;
            switch (study_type.ToLower())
            {
                case "interventional": type_id = 11; break;
                case "observational": type_id = 12; break;
                case "observational patient registry": type_id = 13; break;
                case "expanded access": type_id = 14; break;
                case "funded programme": type_id = 15; break;
                case "not yet known": type_id = 0; break;
            }
            return type_id;
        }

        public int? GetGenderEligId(string gender_elig)
        {
            int? type_id = null;
            switch (gender_elig.ToLower())
            {
                case "both": type_id = 900; break;
                case "female": type_id = 905; break;
                case "male": type_id = 910; break;
                case "not provided": type_id = 915; break;
            }
            return type_id;
        }

        public int? GetTimeUnitsId(string time_units)
        {
            int? type_id = null;
            switch (time_units.ToLower())
            {
                case "seconds": type_id = 11; break;
                case "minutes": type_id = 12; break;
                case "hours": type_id = 13; break;
                case "days": type_id = 14; break;
                case "weeks": type_id = 15; break;
                case "months": type_id = 16; break;
                case "years": type_id = 17; break;
                case "not provided": type_id = 0; break;
            }
            return type_id;
        }

        public int? GetPhaseId(string phase)
        {
            int? type_id = null;
            switch (phase.ToLower())
            {
                case "n/a": type_id = 100; break;
                case "not applicable": type_id = 100; break;
                case "early phase 1": type_id = 105; break;
                case "phase 1": type_id = 110; break;
                case "phase 1/phase 2": type_id = 115; break;
                case "phase 2": type_id = 120; break;
                case "phase 2/phase 3": type_id = 125; break;
                case "phase 3": type_id = 130; break;
                case "phase 4": type_id = 135; break;
                case "not provided": type_id = 140; break;
            }
            return type_id;
        }

        public int? GetPrimaryPurposeId(string primary_purpose)
        {
            int? type_id = null;
            switch (primary_purpose.ToLower())
            {
                case "treatment": type_id = 400; break;
                case "prevention": type_id = 405; break;
                case "diagnostic": type_id = 410; break;
                case "supportive care": type_id = 415; break;
                case "screening": type_id = 420; break;
                case "health services research": type_id = 425; break;
                case "basic science": type_id = 430; break;
                case "device feasibility": type_id = 435; break;
                case "other": type_id = 440; break;
                case "not provided": type_id = 445; break;
                case "educational/counseling/training": type_id = 450; break;
            }
            return type_id;
        }

        public int? GetAllocationTypeId(string allocation_type)
        {
            int? type_id = null;
            switch (allocation_type.ToLower())
            {
                case "n/a": type_id = 200; break;
                case "randomized": type_id = 205; break;
                case "non-randomized": type_id = 210; break;
                case "not provided": type_id = 215; break;
            }
            return type_id;
        }

        public int? GetDesignTypeId(string design_type)
        {
            int? type_id = null;
            switch (design_type.ToLower())
            {
                case "single group assignment": type_id = 300; break;
                case "parallel assignment": type_id = 305; break;
                case "crossover assignment": type_id = 310; break;
                case "factorial assignment": type_id = 315; break;
                case "sequential assignment": type_id = 320; break;
                case "not provided": type_id = 325; break;
            }
            return type_id;
        }

        public int? GetMaskingTypeId(string masking_type)
        {
            int? type_id = null;
            switch (masking_type.ToLower())
            {
                case "none (open label)": type_id = 500; break;
                case "single": type_id = 505; break;
                case "double": type_id = 510; break;
                case "triple": type_id = 515; break;
                case "quadruple": type_id = 520; break;
                case "not provided": type_id = 525; break;
            }
            return type_id;
        }

        public int? GetObsModelTypeId(string obsmodel_type)
        {
            int? type_id = null;
            switch (obsmodel_type.ToLower())
            {
                case "cohort": type_id = 600; break;
                case "case control": type_id = 605; break;
                case "case-control": type_id = 605; break;
                case "case-only": type_id = 610; break;
                case "case-crossover": type_id = 615; break;
                case "ecologic or community": type_id = 620; break;
                case "family-based": type_id = 625; break;
                case "other": type_id = 630; break;
                case "not provided": type_id = 635; break;
                case "defined population": type_id = 640; break;
                case "natural history": type_id = 645; break;
            }
            return type_id;
        }

        public int? GetTimePerspectiveId(string time_perspective)
        {
            int? type_id = null;
            switch (time_perspective.ToLower())
            {
                case "retrospective": type_id = 700; break;
                case "prospective": type_id = 705; break;
                case "cross-sectional": type_id = 710; break;
                case "other": type_id = 715; break;
                case "not provided": type_id = 720; break;
                case "retrospective/prospective": type_id = 725; break;
                case "longitudinal": type_id = 730; break;
            }
            return type_id;
        }

        public int? GetSpecimentRetentionId(string specimen_retention)
        {
            int? type_id = null;
            switch (specimen_retention.ToLower())
            {
                case "none retained": type_id = 800; break;
                case "samples with dna": type_id = 805; break;
                case "samples without dna": type_id = 810; break;
                case "not provided": type_id = 815; break;
            }
            return type_id;
        }


        public string GetTimeUnits(string time_units)
        {
            // was not classified previously...
            // starts with "Other" and has brackets around the text
            string time_string = time_units.Replace("Other", "").Trim();
            time_string = time_string.TrimStart('(').TrimEnd(')').ToLower();

            // was not classified previously...
            if (Regex.Match(time_string, @"\d+y").Success)
            {
                return "Years";
            }
            else if (Regex.Match(time_string, @"\d+m").Success)
            {
                return "Months";
            }
            else if (Regex.Match(time_string, @"\d+w").Success)
            {
                return "Weeks";
            }
            else if (Regex.Match(time_string, @"\d+d").Success)
            {
                return "Days";
            }
            else if (Regex.Match(time_string, @"^\d+$").Success)
            {
                // default of years for numbers on their own
                return "Years";
            }
            else
            {
                return null;

            }
        }
    }

}
