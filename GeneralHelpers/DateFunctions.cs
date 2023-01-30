using System.Text.RegularExpressions;

namespace MDR_Harvester.Extensions;

public static class DateStrinExtensions
{
    public static SplitDate? GetDatePartsFromCTGString(this string dateString)
    {
        // Designed to deal with CTG dates...
        // input date string is in the form of "<month name> day, year"
        // or in some cases in the form "<month name> year"

        if (string.IsNullOrEmpty(dateString))
        {
            return null;
        }

        string? year_string, month_name, day_string, month_as3 = null;
        int? year_num = null, day_num = null;

        // First split the string on any comma.

        int comma_pos = dateString.IndexOf(',');
        if (comma_pos > 0)
        {
            year_string = dateString[(comma_pos + 1)..].Trim();
            string first_part = dateString[0..comma_pos].Trim();

            // First part should split on the space

            int space_pos = first_part.IndexOf(' ');
            day_string = first_part[(space_pos + 1)..].Trim();
            month_name = first_part[..space_pos].Trim();
        }
        else
        {
            int space_pos = dateString.IndexOf(' ');
            year_string = dateString[(space_pos + 1)..].Trim();
            month_name = dateString[..space_pos].Trim();
            day_string = "";
        }

        // convert strings into integers and month abbreviation.

        if (int.TryParse(year_string, out int y))
        {
            year_num = y;
        }
        int month_num = GetMonthAsInt(month_name);
        if (month_num != 0)
        {
            month_as3 = ((Months3)month_num).ToString();
        }
        if (int.TryParse(day_string, out int d))
        {
            day_num = d;
        }

        // Get date as string.

        string? date_as_string;
        if (year_num is not null && month_as3 is not null && day_num is not null)
        {
            date_as_string = year_num.ToString() + " " + month_as3 + " " + day_num.ToString();
        }
        else if (year_num is not null && month_as3 is not null && day_num is null)
        {
            date_as_string = year_num.ToString() + ' ' + month_as3;
        }
        else if (year_num is not null && month_as3 is null && day_num is null)
        {
            date_as_string = year_num.ToString();
        }
        else
        {
            date_as_string = null;
        }

        return date_as_string == null 
            ? null 
            : new SplitDate(year_num, month_num, day_num, date_as_string);
    }

      
    public static SplitDate? GetDatePartsFromEUCTRString(this string dateString)
    {
        // date here is in the format dd Mon yyyy, e.g. 16 Aug 2017.

        if (string.IsNullOrEmpty(dateString))
        {
            return null;
        }

        // check format.

        if (Regex.Match(dateString, @"^\d{2} \w{3} \d{4}$").Success)
        {
            int? year_num = null, day_num = null;

            string day_string = dateString.Substring(0, 2);
            string month_string = dateString.Substring(3, 3);
            string year_string = dateString.Substring(7, 4);

            // convert strings into integers.

            if (int.TryParse(year_string, out int y))
            {
                year_num = y;
            }
            if (int.TryParse(day_string, out int d))
            {
                day_num = d;
            }
            int month_num = GetMonth3AsInt(month_string);


            if (year_num != null && month_num != 0 && day_num != null)
            {
                // get date as standard string.

                string date_as_string = year_num.ToString() + " " + month_string + " " + day_num.ToString();

                return new SplitDate(year_num, month_num, day_num, date_as_string);
            }
            else
            {
                return null;
            }
        }
        else
        {
            return null;
        }

    }


    public static SplitDate? GetDatePartsFromUSString(this string dateString)
    {
        // date here is in the format mm/dd/yyyy, e.g. 25/06/2018.

        if (string.IsNullOrEmpty(dateString))
        {
            return null;
        }

        // check format.

        if (Regex.Match(dateString, @"^\d{2}/\d{2}/\d{4}$").Success)
        {
            int? year_num = null, month_num = null, day_num = null;
            string? month_as3 = null;

            string month_string = dateString.Substring(0, 2);
            string day_string = dateString.Substring(3, 2);
            string year_string = dateString.Substring(6, 4);

            // convert strings into integers
            if (int.TryParse(year_string, out int y))
            {
                year_num = y;
            }
            if (int.TryParse(month_string, out int m))
            {
                month_num = m;
            }
            if (month_num is not null && month_num > 0)
            {
                month_as3 = ((Months3)month_num).ToString();
            }
            if (int.TryParse(day_string, out int d))
            {
                day_num = d;
            }

            if (year_num is not null && month_as3 is not null && day_num is not null)
            {
                // Get date with standard string.

                string date_as_string = year_num.ToString() + " " + month_as3 + " " + day_num.ToString();
                return new SplitDate(year_num, month_num, day_num, date_as_string);
            }
            else
            {
                return null;
            }
        }
        else
        {
            return null;
        }

    }


    public static SplitDate? GetDatePartsFromISOString(this string dateString)
    {
        // input date string, if present, is in the form of "yyyy-mm-dd".

        if (string.IsNullOrEmpty(dateString))
        {
            return null;
        }
        
        // check format and possible alternative.

        if (Regex.Match(dateString, @"^\d{8}$").Success)
        {
            // If presented without hyphens add them to create the standard format.

            dateString = dateString.Substring(0, 4) + "-" + dateString.Substring(4, 2) + "-" + dateString.Substring(6, 2);
        }

        if (Regex.Match(dateString, @"^\d{4}-\d{2}-\d{2}$").Success)
        {
            int? year_num = null, month_num = null, day_num = null;
            string? month_as3 = null;

            string year_string = dateString.Substring(0, 4);
            string month_string = dateString.Substring(5, 2);
            string day_string = dateString.Substring(8, 2);

            // convert strings into integers
            // convert strings into integers
            if (int.TryParse(year_string, out int y))
            {
                year_num = y;
            }
            if (int.TryParse(month_string, out int m))
            {
                month_num = m;
            }
            if (month_num is not null && month_num > 0)
            {
                month_as3 = ((Months3)month_num).ToString();
            }
            if (int.TryParse(day_string, out int d))
            {
                day_num = d;
            }

            if (year_num is not null && month_as3 is not null && day_num is not null)
            {
                // get date with standard string.

                string date_as_string = year_num.ToString() + " " + month_as3 + " " + day_num.ToString();
                return new SplitDate(year_num, month_num, day_num, date_as_string);
            }
            else
            {
                return null;
            }
        }
        else
        {
            return null;
        }

    }


    public static string? StandardiseCTGDateFormat(this string? inputDate)
    {
        if (string.IsNullOrEmpty(inputDate))
        {
            return null;
        }
        else
        {
            SplitDate? SD = inputDate.GetDatePartsFromCTGString();
            if (SD is null)
            { 
                return null;
            }
            else
            {
                return SD.date_string;
            }
        }
    }


    private static int GetMonthAsInt(string month_name)
    {
        try
        {
            return (int)(Enum.Parse<MonthsFull>(month_name));
        }
        catch (ArgumentException)
        {
            return 0;
        }
    }

    private static int GetMonth3AsInt(string month_abbrev)
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

public enum MonthsFull
{
    January = 1, February, March, April, May, June,
    July, August, September, October, November, December
};


public enum Months3
{
    Jan = 1, Feb, Mar, Apr, May, Jun,
    Jul, Aug, Sep, Oct, Nov, Dec
};
