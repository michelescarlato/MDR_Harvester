using System.Text.RegularExpressions;

namespace MDR_Harvester.Extensions;

public static class DateStringExtensions
{
    private static SplitDate? GetDateFromParts(string year_string, string month_string, string day_string)
    {
        // convert strings into integers and month abbreviation.
        
        int? year_num = null, month_num = null, day_num = null;
        string? month_as3 = null;
        
        if (int.TryParse(year_string, out int y))
        {
            year_num = y;
        }
        if (int.TryParse(month_string, out int m))
        {
            month_num = m;
        }
        if (int.TryParse(day_string, out int d))
        {
            day_num = d;
        }
        if (month_num > 0)
        {
            month_as3 = ((Months3)month_num).ToString();
        }
        
        string? date_as_string;     // Get date as standard string.
        if (year_num is not null && month_as3 is not null && day_num is not null)
        {
            date_as_string = $"{day_num} {month_as3} {year_num}";
        }
        else if (year_num is not null && month_as3 is not null && day_num is null)
        {
            date_as_string = $"{month_as3} {year_num}";
        }
        else if (year_num is not null && month_as3 is null && day_num is null)
        {
            date_as_string = $"{year_num}";
        }
        else
        {
            date_as_string = null;
        }

        return date_as_string == null 
            ? null 
            : new SplitDate(year_num, month_num, day_num, date_as_string);
    }
    
  
    public static SplitDate? GetDatePartsFromEuropeanString(this string dateString)
    {
        // Dates in different EU sources nay be in different formats
        // including dd/MM/yyyy, yyyy-MM-dd, and dd MMM yyyy, e.g. 16 Aug 2017.
        // This function checks the format before calling the appropriate 
        // date conversion function.

        if (string.IsNullOrEmpty(dateString))
        {
            return null;
        }
        if (Regex.Match(dateString, @"^\d{2}/\d{2}/\d{4}$").Success)
        {
            return GetDatePartsFromEUString(dateString);
        }
        if (Regex.Match(dateString, @"^\d{4}-\d{2}-\d{2}$").Success)
        {
            return GetDatePartsFromISOString(dateString);
        }
        if (Regex.Match(dateString, @"^\d{2} \w{3} \d{4}$").Success)
        {
            return GetDatePartsFromEUCTRString(dateString);
        }
        return null;
    }

    
    public static SplitDate? GetDatePartsFromEUCTRString(this string dateString)
    {
        // date here is in the format dd MMM yyyy, e.g. 16 Aug 2017.

        if (string.IsNullOrEmpty(dateString))
        {
            return null;
        }
        if (!Regex.Match(dateString, @"^\d{2} \w{3} \d{4}$").Success)
        {
            return null;
        }

        string day_string = dateString.Substring(0, 2);
        string month_as3string = dateString.Substring(3, 3);
        string year_string = dateString.Substring(7, 4);
        string month_string = GetMonth3AsInt(month_as3string).ToString();
        return GetDateFromParts(year_string, month_string, day_string);
    }

/*
    public static SplitDate? GetDatePartsFromUSString(this string dateString)
    {
        // date here is in the format mm/dd/yyyy, e.g. 06/25/2018.

        if (string.IsNullOrEmpty(dateString))
        {
            return null;
        }
        if (!Regex.Match(dateString, @"^\d{2}/\d{2}/\d{4}$").Success)
        {
            return null;
        }

        string month_string = dateString.Substring(0, 2);
        string day_string = dateString.Substring(3, 2);
        string year_string = dateString.Substring(6, 4);
        return GetDateFromParts(year_string, month_string, day_string);
    }
    */

    public static SplitDate? GetDatePartsFromEUString(this string dateString)
    {
        // date here is in the format dd/mm/yyyy, e.g. 25/06/2018.

        if (string.IsNullOrEmpty(dateString))
        {
            return null;
        }
        if (!Regex.Match(dateString, @"^\d{2}/\d{2}/\d{4}$").Success)
        {
            return null;
        }

        string day_string = dateString.Substring(0, 2);        
        string month_string = dateString.Substring(3, 2);
        string year_string = dateString.Substring(6, 4);
        return GetDateFromParts(year_string, month_string, day_string);
    }
    
    public static SplitDate? GetDatePartsFromISOString(this string dateString)
    {
        // input date string is in the form of "yyyy-MM-dd", sometimes yyyy-MM,
        // occasionally just yyyyMMdd or yyyMM.

        if (string.IsNullOrEmpty(dateString))
        {
            return null;
        }
        
        string year_string;
        string month_string;
        string day_string;
        
        if (Regex.Match(dateString, @"^\d{4}-\d{2}-\d{2}$").Success)   // Normal ISO date.
        {
            year_string = dateString.Substring(0, 4);
            month_string = dateString.Substring(5, 2);
            day_string = dateString.Substring(8, 2);
            return GetDateFromParts(year_string, month_string, day_string);
        }
        if (Regex.Match(dateString, @"^\d{4}-\d{2}$").Success)   // Partial date.
        {
            year_string = dateString.Substring(0, 4);
            month_string = dateString.Substring(5, 2);
            return GetDateFromParts(year_string, month_string, "");
        }
        if (Regex.Match(dateString, @"^\d{8}$").Success)   // condensed data
        {
            year_string = dateString.Substring(0, 4);
            month_string = dateString.Substring(4, 2);
            day_string = dateString.Substring(6, 2);
            return GetDateFromParts(year_string, month_string, day_string);
        }
        if (Regex.Match(dateString, @"^\d{6}$").Success)   // condensed partial date.
        {
            year_string = dateString.Substring(0, 4);
            month_string = dateString.Substring(4, 2);
            return GetDateFromParts(year_string, month_string, "");
        }
        if (Regex.Match(dateString, @"^\d{4}$").Success) // year only
        {
            return GetDateFromParts(dateString, "", "");
        }
        return null;  // does not match anything
    }


    public static string? StandardiseCTGDateString(this string? inputDate)
    {
        if (string.IsNullOrEmpty(inputDate))
        {
            return null;
        }
        SplitDate? SD = inputDate.GetDatePartsFromISOString();
        return SD?.date_string;
    }
    
    
    public static string? MonthYearDateString(this string? inputDate)
    {
        if (string.IsNullOrEmpty(inputDate))
        {
            return null;
        }
        SplitDate? SD = inputDate.GetDatePartsFromISOString();
        if (SD is null)
        {
            return null;
        }
        string? month_year;
        if (SD.month is not null && SD.month != 0)
        {
            month_year = (MonthsFull)SD.month + " " + (SD.year?.ToString() ?? "");
        }
        else
        {
            month_year = SD.year?.ToString();
        }
        return month_year;
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
