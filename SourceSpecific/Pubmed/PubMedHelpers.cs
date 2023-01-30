namespace MDR_Harvester.Pubmed;

internal static class PubMedHelpers
{
    // Two check routines that scan previously extracted Identifiers or Dates, to 
    // indicate if the input Id / Date type has already beenm extracted.

    internal static bool IdNotPresent(List<ObjectIdentifier> ids, int id_type, string id_value)
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


    internal static bool DateNotPresent(List<ObjectDate> dates, int datetype_id, int? year, int? month, int? day)
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

    internal static bool IsAnOrganisation(this string? fullname)
    {
        if (string.IsNullOrEmpty(fullname))
        {
            return false;
        }
        else
        {
            bool is_org = false;
            string fname = fullname.ToLower();
            if (fname.Contains(" group") || fname.StartsWith("group") ||
                fname.Contains(" assoc") || fname.Contains(" team") ||
                fname.Contains("collab") || fname.Contains("network"))
            {
                is_org = true;
            }

            return is_org;
        }
    }

    internal static SplitDate? GetSplitDateFromNumericDate(int? year, int? month, int? day)
    {
        if (!year.HasValue)
        {
            return null;
        }
        
        string? monthas3 = null;
        if (month.HasValue)
        {
            monthas3 = ((Months3)month).ToString();
        }

        string? date_as_string = null;        
        if (month.HasValue && day.HasValue)
        {
            date_as_string = $"{year} {monthas3} {day}";
        }
        else if (month.HasValue && day is null)
        {
            date_as_string = $"{year} {monthas3}";
        }
        else if (monthas3 is null && day is null)
        {
            date_as_string = $"{year}";
        }

        return new SplitDate(year, month, day, date_as_string);
    }


    internal static SplitDate? GetSplitDateFromPubDate(int? year, string? monthas3, int? day)
    {
        if (!year.HasValue)
        {
            return null;
        }
        
        int? month = null;
        if (!string.IsNullOrEmpty(monthas3))
        {
            month = monthas3.GetMonth3AsInt();
        }

        string? date_as_string = null;
        if (!string.IsNullOrEmpty(monthas3) && day.HasValue)
        {
            date_as_string = $"{year} {monthas3} {day}";
        }
        else if (!string.IsNullOrEmpty(monthas3) && day is null)
        {
            date_as_string = $"{year} {monthas3}";
        }
        else if (string.IsNullOrEmpty(monthas3) && day is null)
        {
            date_as_string = $"{year}";
        }

        return new SplitDate(year, month, day, date_as_string);
    }


    internal static SplitDateRange? ProcessMedlineDate(string? ml_date_string)
    {
        if (string.IsNullOrEmpty(ml_date_string))
        {
            return null;
        }

        int? pub_year = null;
        ml_date_string = ml_date_string.Trim();
        if (ml_date_string.Length < 4)
        {
            return null;
        }
        else if (ml_date_string.Length == 4)
        {
            if (int.TryParse(ml_date_string, out int pub_year_try))
            {
                pub_year = pub_year_try;
            }
        }
        else if (ml_date_string.Length > 4)
        {
            bool year_at_start = false, year_at_end = false;
            if (int.TryParse(ml_date_string[..4], out int pub_year_stry))
            {
                pub_year = pub_year_stry;
                year_at_start = true;
            }
            if (int.TryParse(ml_date_string[^4..], out int pub_year_etry))
            {
                pub_year = pub_year_etry;
                year_at_end = true;
            }

            if (year_at_start && year_at_end && ml_date_string.Length >= 4)
            {
                // very occasionally happens year is at both start and end - remove last

                ml_date_string = ml_date_string[^4..].Trim();
            }
            else if (!year_at_start && year_at_end)
            {
                // occasionally happens, as with EUCTR dates - switch year to beginning.

                ml_date_string = pub_year.ToString() + " " + ml_date_string[^4..].Trim();
            }
        }

        // A 4 digit year is sought at either the beginning or end of the string.
        // An end year is moved to the beginning.

        if (!pub_year.HasValue)
        {
            return null;
        }

        string non_year_date = ml_date_string[4..].Trim();
        if (non_year_date.Length < 4)
        {
            return new SplitDateRange(pub_year, null, null, pub_year, null, null, false, ml_date_string);
        }

        // Try and process the non-year part of the date
        // First try to regularise separators
        // and then replace any seasonal references.

        non_year_date = non_year_date.Replace("/", "-").Replace("  ", " ");
        non_year_date = non_year_date.Replace(" - ", "-").Replace("- ", "-").Replace(" -", "-");

        non_year_date = non_year_date.Replace("Spring", "Apr-Jun");
        non_year_date = non_year_date.Replace("Summer", "Jul-Sep");
        non_year_date = non_year_date.Replace("Autumn", "Oct-Dec");
        non_year_date = non_year_date.Replace("Fall", "Oct-Dec");
        non_year_date = non_year_date.Replace("Winter", "Jan-Mar");
        int? smonth = null, emonth = null;

        if (non_year_date[3] == ' ')
        {
            // Often a month followed by two dates, e.g. "Jun 12-21".

            int? sday = null, eday = null;
            string month_abbrev = non_year_date[..3];
            int? month = month_abbrev.GetMonth3AsInt();
            if (month != 0)
            {
                smonth = month;
                emonth = month;
                string rest = non_year_date[3..].Trim();
                if (rest.IndexOf("-", StringComparison.Ordinal) != -1)
                {
                    int hyphen_pos = rest.IndexOf("-", StringComparison.Ordinal);
                    string s_day = rest[..hyphen_pos].Trim();
                    string e_day = rest[(hyphen_pos + 1)..];
                    if (Int32.TryParse(s_day, out int s_day_int) && (Int32.TryParse(e_day, out int e_day_int)))
                    {
                        if ((s_day_int > 0 && s_day_int < 32) && (e_day_int > 0 && e_day_int < 32))
                        {
                            sday = s_day_int;
                            eday = e_day_int;
                        }
                    }
                }
            }
            return new SplitDateRange(pub_year, smonth, sday, pub_year, emonth, eday, true, ml_date_string);
        }

        else if (non_year_date.IndexOf("-", StringComparison.Ordinal) != -1)
        {
            // Often two months separated by a hyphen, e.g."May-Jul".

            string s_month = non_year_date[..3];
            string e_month = non_year_date[4..].Trim();
            e_month = e_month[..3];
            int startmonth = s_month.GetMonth3AsInt();
            int endmonth = e_month.GetMonth3AsInt();
            if (startmonth != 0 && endmonth != 0)
            {
                smonth = startmonth;
                emonth = endmonth;
            }
            return new SplitDateRange(pub_year, smonth, null, pub_year, emonth, null, true, ml_date_string);
        }

        else
        {
            return new SplitDateRange(pub_year, null, null, pub_year, null, null, false, ml_date_string);
        }
    }


    internal static string GetCitationName(List<ObjectContributor> authors, int author_pos)
    {
        string given_name = authors[author_pos].person_given_name ?? "";
        string initials = given_name == "" ? "" : given_name[..1].ToUpper() + "";
        return (authors[author_pos].person_family_name + " " + initials).Trim();
    }
}



internal static class PubMedExtensions
{

    internal static int GetMonthAsInt(this string? month_name)
    {
        if (string.IsNullOrEmpty(month_name))
        {
            return 0;
        }
        else
        {
            try
            {
                return (int)(Enum.Parse<MonthsLong>(month_name));
            }
            catch (ArgumentException)
            {
                return 0;
            }
        }
    }


    internal static int GetMonth3AsInt(this string? month_abbrev)
    {
        if (string.IsNullOrEmpty(month_abbrev))
        {
            return 0;
        }
        else
        {
            try
            {
                return (int)(Enum.Parse<Months3>(month_abbrev));
            }
            catch (ArgumentException)
            {
                return 0;
            }
        }
    }
}


internal enum MonthsLong
{
    January = 1, February, March, April, May, June,
    July, August, September, October, November, December
};


internal enum Months3
{
    Jan = 1, Feb, Mar, Apr, May, Jun,
    Jul, Aug, Sep, Oct, Nov, Dec
};
