using System;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace DataHarvester
{
    public class DateHelpers
    {
        public SplitDate GetDatePartsFromCTGString(string dateString)
        {
            // Designed to deal with CTG dates...
            // input date string is in the form of "<month name> day, year"
            // or in some cases in the form "<month name> year"
            // split the string on the comma
            string year_string, month_name, day_string;
            int? year_num = null, month_num, day_num = null;

            int comma_pos = dateString.IndexOf(',');
            if (comma_pos > 0)
            {
                year_string = dateString.Substring(comma_pos + 1).Trim();
                string first_part = dateString.Substring(0, comma_pos).Trim();

                // first part should split on the space
                int space_pos = first_part.IndexOf(' ');
                day_string = first_part.Substring(space_pos + 1).Trim();
                month_name = first_part.Substring(0, space_pos).Trim();
            }
            else
            {
                int space_pos = dateString.IndexOf(' ');
                year_string = dateString.Substring(space_pos + 1).Trim();
                month_name = dateString.Substring(0, space_pos).Trim();
                day_string = "";
            }

            // convert strings into integers
            if (int.TryParse(year_string, out int y)) year_num = y;
            month_num = GetMonthAsInt(month_name);
            if (int.TryParse(day_string, out int d)) day_num = d;
            string month_as3 = ((Months3)month_num).ToString();

            // get date as string
            string date_as_string;
            if (year_num != null && month_num != null && day_num != null)
            {
                date_as_string = year_num.ToString() + " " + month_as3 + " " + day_num.ToString();
            }
            else if (year_num != null && month_num != null && day_num == null)
            {
                date_as_string = year_num.ToString() + ' ' + month_as3;
            }
            else if (year_num != null && month_num == null && day_num == null)
            {
                date_as_string = year_num.ToString();
            }
            else
            {
                date_as_string = null;
            }

            return new SplitDate(year_num, month_num, day_num, date_as_string);
        }

                    
        public SplitDate GetDatePartsFromEUCTRString(string dateString)
        {
            // date here is in the format dd Mon yyyy, e.g. 16 Aug 2017

            try
            {
                if (dateString == null) return null;

                // check format 
                if (Regex.Match(dateString, @"^\d{2} \w{3} \d{4}$").Success)
                {
                    string year_string, month_string, day_string;
                    int? year_num = null, month_num = null, day_num = null;

                    day_string = dateString.Substring(0, 2);
                    month_string = dateString.Substring(3, 3);
                    year_string = dateString.Substring(7, 4);

                    year_num = null;
                    // convert strings into integers
                    if (int.TryParse(year_string, out int y)) year_num = y;
                    if (int.TryParse(day_string, out int d)) day_num = d;

                    // convert month name to integer using the enumeration
                    month_num = GetMonth3AsInt(month_string);

                    if (year_num != null && month_num != 0 && day_num != null)
                    {
                        // get date as standard string
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
            catch (Exception e)
            {
                string s = e.Message;
                return null;
            }
        }


        public SplitDate GetDatePartsFromUSString(string dateString)
        {
            // date here is in the format mm/dd/yyyy, e.g. 25/06/2018

            try
            {
                if (dateString == null) return null;

                // check format 
                if (Regex.Match(dateString, @"^\d{2}/\d{2}/\d{4}$").Success)
                {
                    string year_string, month_string, day_string;
                    int? year_num = null, month_num = null, day_num = null;

                    month_string = dateString.Substring(0, 2);
                    day_string = dateString.Substring(3, 2);
                    year_string = dateString.Substring(6, 4);

                    year_num = null;

                    // convert strings into integers
                    if (int.TryParse(year_string, out int y)) year_num = y; else year_num = null;
                    if (int.TryParse(month_string, out int m)) month_num = m; else month_num = null;
                    if (int.TryParse(day_string, out int d)) day_num = d; else day_num = null;
                    string month_as3 = ((Months3)month_num).ToString();

                    if (year_num != null && month_num != 0 && day_num != null)
                    {
                        // get date as standard string
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
            catch (Exception e)
            {
                string s = e.Message;
                return null;
            }
        }


        public SplitDate GetDatePartsFromISOString(string dateString)
        {
            try
            {
                // input date string, if present, is in the form of "yyyy-mm-dd"
                if (dateString == null) return null;

                // check format and possible alternative
                if (Regex.Match(dateString, @"^\d{8}$").Success)
                {
                    // If presented without hyphens add them to create the standard format
                    dateString = dateString.Substring(0, 4) + "-" + dateString.Substring(4, 2) + "-" + dateString.Substring(6, 2);
                }

                if (Regex.Match(dateString, @"^\d{4}-\d{2}-\d{2}$").Success)
                {
                    string year_string, month_string, day_string;
                    int? year_num, month_num, day_num;

                    year_string = dateString.Substring(0, 4);
                    month_string = dateString.Substring(5, 2);
                    day_string = dateString.Substring(8, 2);

                    // convert strings into integers
                    if (int.TryParse(year_string, out int y)) year_num = y; else year_num = null;
                    if (int.TryParse(month_string, out int m)) month_num = m; else month_num = null;
                    if (int.TryParse(day_string, out int d)) day_num = d; else day_num = null;
                    string month_as3 = ((Months3)month_num).ToString();

                    // get date as string
                    string date_as_string;
                    if (year_num != null && month_num != null && day_num != null)
                    {
                        date_as_string = year_num.ToString() + " " + month_as3 + " " + day_num.ToString();
                    }
                    else if (year_num != null && month_num != null && day_num == null)
                    {
                        date_as_string = year_num.ToString() + ' ' + month_as3;
                    }
                    else if (year_num != null && month_num == null && day_num == null)
                    {
                        date_as_string = year_num.ToString();
                    }
                    else
                    {
                        date_as_string = null;
                    }

                    return new SplitDate(year_num, month_num, day_num, date_as_string);
                }
                else
                {
                    return null;
                }
            }
            catch (Exception e)
            {
                string s = e.Message;
                return null;
            }
        }



        // ProcessDate takes the standard composite date element as an input, extracts the various constituent
        // parts, and returns an ObjectDate classs, which also indicates if the date was partial, and which includes
        // a standardised string repreesentation as well as Y, M, D integer components.

        public ObjectDate ProcessDate(string sd_oid, XElement composite_date, int date_type_id, string date_type)
        {
            //composite_date should normnally have year, month and day entries but these may not all be present
            int? year = (composite_date.Element("Year") == null) ? null : (int?)composite_date.Element("Year");
            int? day = (composite_date.Element("Day") == null) ? null : (int?)composite_date.Element("Day");

            // month may be a number or a 3 letter month abbreviation
            int? month = null;
            string monthas3 = (composite_date.Element("Month") == null) ? 
                                          null : (string)composite_date.Element("Month");

            if (monthas3 != null)
            {
                if (Int32.TryParse(monthas3, out int mn))
                {
                    // already an integer
                    month = mn;
                    // change the monthAs3 to the expected string
                    monthas3 = ((Months3)mn).ToString();
                }
                else
                {
                    // derive month using the enumeration
                    // monthAs3 stays the same
                    month = GetMonth3AsInt(monthas3);
                }
            }

            string date_as_string = "";
            if (year != null && month != null && day != null)
            {
                date_as_string = year.ToString() + " " + monthas3 + " " + day.ToString();
            }
            else
            {
                if (year != null && monthas3 != null && day == null)
                {
                    date_as_string = year.ToString() + ' ' + monthas3;
                }
                else if (year != null && monthas3 == null && day == null)
                {
                    date_as_string = year.ToString();
                }
                else
                {
                    date_as_string = null;
                }
            }

            ObjectDate dt = new ObjectDate(sd_oid, date_type_id, date_type, year, month, day, date_as_string);
            dt.date_is_range = false;

            return dt;
        }



        // ProcessMedlineDate tries to extractr as much information as possible from 
        // a non-standard 'Medline' date entry. 

        public ObjectDate ProcessMedlineDate(string sd_oid, string date_string, int date_type_id, string date_type)
        {

            int? pub_year = null;
            bool year_at_start = false, year_at_end = false;
            date_string = date_string.Trim();
            string orig_date_string = date_string;

            // A 4 digit year is sought at either the beginning or end of the string.
            // An end year is moved to the beginning.

            if (date_string.Length == 4)
            {
                if (int.TryParse(date_string, out int pub_year_try))
                {
                    pub_year = pub_year_try;
                }
            }
            else if (date_string.Length >= 4)
            {
                if (int.TryParse(date_string.Substring(0, 4), out int pub_year_stry))
                {
                    pub_year = pub_year_stry;
                    year_at_start = true;
                }
                if (int.TryParse(date_string.Substring(date_string.Length - 4, 4), out int pub_year_etry))
                {
                    pub_year = pub_year_etry;
                    year_at_end = true;
                }
                if (year_at_start && year_at_end && date_string.Length >= 4)
                {
                    // very occasionally happens - remove last year
                    date_string = date_string.Substring(date_string.Length - 4, 4).Trim();
                }
                else if (!year_at_start && year_at_end)
                {
                    // occasionally happens, as with EUCTR dates - switch year to beginning
                    date_string = pub_year.ToString() + " " + date_string.Substring(date_string.Length - 4, 4).Trim();
                }
            }

            // Create a 'default' date object, with as much as possible of the details to 
            // be completed using the string parsing below, which tries to identify start and end single dates.

            ObjectDate dt = new ObjectDate(sd_oid, date_type_id, date_type, orig_date_string, pub_year);

            if (pub_year != null)
            {
                string non_year_date = date_string.Substring(4).Trim();
                if (non_year_date.Length > 3)
                {
                    // try to regularise separators
                    non_year_date = non_year_date.Replace(" - ", "-");
                    non_year_date = non_year_date.Replace("/", "-");

                    // replace seasonal references
                    non_year_date = non_year_date.Replace("Spring", "Apr-Jun");
                    non_year_date = non_year_date.Replace("Summer", "Jul-Sep");
                    non_year_date = non_year_date.Replace("Autumn", "Oct-Dec");
                    non_year_date = non_year_date.Replace("Fall", "Oct-Dec");
                    non_year_date = non_year_date.Replace("Winter", "Jan-Mar");

                    if (non_year_date[3] == ' ')
                    {
                        // May be a month followed by two dates, e.g. "Jun 12-21".

                        string month_abbrev = non_year_date.Substring(0, 3);
                        int? month = GetMonthAsInt(month_abbrev);
                        if (month != null)
                        {
                            dt.start_year = pub_year;
                            dt.end_year = pub_year;
                            dt.start_month = month;
                            dt.end_month = month;
                            dt.date_is_range = true;

                            string rest = non_year_date.Substring(3).Trim();

                            if (rest.IndexOf("-") != -1)
                            {
                                int hyphen_pos = rest.IndexOf("-");
                                string s_day = rest.Substring(0, hyphen_pos);
                                string e_day = rest.Substring(hyphen_pos + 1);
                                if (Int32.TryParse(s_day, out int s_day_int) && (Int32.TryParse(e_day, out int e_day_int)))
                                {
                                    if ((s_day_int > 0 && s_day_int < 32) && (e_day_int > 0 && e_day_int < 32))
                                    {
                                        dt.start_day = s_day_int;
                                        dt.end_day = e_day_int;
                                    }
                                }
                            }
                        }

                        dt.date_is_range = true;
                    }

                    if (non_year_date[3] == '-')
                    {
                        // May be two months separated by a hyphen, e.g."May-Jul".

                        int hyphen_pos = non_year_date.IndexOf("-");
                        string s_month = non_year_date.Substring(0, hyphen_pos).Trim();
                        string e_month = non_year_date.Substring(hyphen_pos + 1).Trim();
                        s_month = s_month.Substring(0, 3);  // just get first 3 characters
                        e_month = e_month.Substring(0, 3);
                        int? smonth = GetMonthAsInt(s_month);
                        int? emonth = GetMonthAsInt(e_month);
                        if (smonth != null && emonth != null)
                        {
                            dt.start_year = pub_year;
                            dt.end_year = pub_year;
                            dt.start_month = smonth;
                            dt.end_month = emonth;
                            dt.date_is_range = true;
                        }
                    }
                }
                else
                {
                    dt.end_year = dt.start_year;
                    dt.date_is_range = true;
                }
            }

            return dt;
        }


        public string StandardiseCTGDateFormat(string inputDate)
        {
            SplitDate SD = GetDatePartsFromCTGString(inputDate);
            return SD.date_string;
        }


        public int GetMonthAsInt(string month_name)
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


        public int GetMonth3AsInt(string month_abbrev)
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


    public class SplitDate
    {
        public int? year;
        public int? month;
        public int? day;
        public string date_string;

        public SplitDate(int? _year, int? _month, int? _day, string _date_string)
        {
            year = _year;
            month = _month;
            day = _day;
            date_string = _date_string;
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
}
