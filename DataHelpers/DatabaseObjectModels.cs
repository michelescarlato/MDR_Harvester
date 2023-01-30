using System.Diagnostics.CodeAnalysis;
using Dapper.Contrib.Extensions;


namespace MDR_Harvester
{
    [SuppressMessage("ReSharper", "CollectionNeverUpdated.Global")]
    public class FullDataObject
    {
        public string? sd_oid { get; set; }
        public string? sd_sid { get; set; }
        public string? title { get; set; }
        public string? version { get; set; }
        public string? display_title { get; set; }
        public string? doi { get; set; }
        public int? doi_status_id { get; set; }
        public int? publication_year { get; set; }
        public int? object_class_id { get; set; }
        public string? object_class { get; set; }
        public int? object_type_id { get; set; }
        public string? object_type { get; set; }
        public int? managing_org_id { get; set; }
        public string? managing_org { get; set; }
        public string? lang_code { get; set; }
        public int? access_type_id { get; set; }
        public string? access_type { get; set; }
        public string? access_details { get; set; }
        public string? access_details_url { get; set; }
        public DateTime? url_last_checked { get; set; }
        public int? eosc_category { get; set; }
        public bool? add_study_contribs { get; set; }
        public bool? add_study_topics { get; set; }
        public DateTime? datetime_of_data_fetch { get; set; }
        public JournalDetails? journal_details { get; set; }
        public List<ObjectDate>? object_dates { get; set; }
        public List<ObjectTitle>? object_titles { get; set; }
        public List<ObjectIdentifier>? object_identifiers { get; set; }
        public List<ObjectTopic>? object_topics { get; set; }
        public List<ObjectPublicationType>? object_pubtypes { get; set; }
        public List<ObjectDescription>? object_descriptions { get; set; }
        public List<ObjectInstance>? object_instances { get; set; }
        public List<ObjectContributor>? object_contributors { get; set; }
        public List<ObjectComment>? object_comments { get; set; }
        public List<ObjectRelationship>? object_relationships { get; set; }
        public List<ObjectRight>? object_rights { get; set; }
        public List<ObjectDBLink>? object_db_ids { get; set; }
    }


    [Table("sd.data_objects")]
    public class DataObject
    {
        public string? sd_oid { get; set; }
        public string? sd_sid { get; set; }
        public string? title { get; set; }
        public string? version { get; set; }
        public string? display_title { get; set; }
        public string? doi { get; set; }
        public int? doi_status_id { get; set; }
        public int? publication_year { get; set; }
        public int? object_class_id { get; set; }
        public string? object_class { get; set; }
        public int? object_type_id { get; set; }
        public string? object_type { get; set; }
        public int? managing_org_id { get; set; }
        public string? managing_org { get; set; }
        public string? lang_code { get; set; }
        public int? access_type_id { get; set; }
        public string? access_type { get; set; }
        public string? access_details { get; set; }
        public string? access_details_url { get; set; }
        public DateTime? url_last_checked { get; set; }
        public int? eosc_category { get; set; }
        public bool? add_study_contribs { get; set; }
        public bool? add_study_topics { get; set; }
        public DateTime? datetime_of_data_fetch { get; set; }

        public DataObject(FullDataObject fob)
        {
            sd_oid = fob.sd_oid;
            sd_sid = fob.sd_sid;
            title = fob.title;
            version = fob.version;
            display_title = fob.display_title;
            doi = fob.doi;
            doi_status_id = fob.doi_status_id;
            publication_year = fob.publication_year;
            object_class_id = fob.object_class_id;
            object_class = fob.object_class;
            object_type_id = fob.object_type_id;
            object_type = fob.object_type;
            managing_org_id = fob.managing_org_id;
            managing_org = fob.managing_org;
            lang_code = "en";
            access_type_id = fob.access_type_id;
            access_type = fob.access_type;
            access_details = fob.access_details;
            access_details_url = fob.access_details_url;
            url_last_checked = fob.url_last_checked;
            eosc_category = fob.eosc_category;
            add_study_contribs = fob.add_study_contribs;
            add_study_topics = fob.add_study_topics;
            datetime_of_data_fetch = fob.datetime_of_data_fetch;
        }


        public DataObject(string? _sd_oid, string? _sd_sid, string? _title, string? _display_title, int? _publication_year, int? _object_class_id,
                            string? _object_class, int? _object_type_id, string? _object_type,
                            int? _managing_org_id, string? _managing_org, 
                            int? _access_type_id, DateTime? _datetime_of_data_fetch)
        {
            sd_oid = _sd_oid;
            sd_sid = _sd_sid;
            title = _title;
            display_title = _display_title;
            doi_status_id = 9;
            publication_year = _publication_year;
            object_class_id = _object_class_id;
            object_class = _object_class;
            object_type_id = _object_type_id;
            object_type = _object_type;
            managing_org_id = _managing_org_id;
            managing_org = _managing_org;
            lang_code = "en";
            access_type_id = _access_type_id;
            if (_access_type_id == 11) access_type = "Public on-screen access and download";
            if (_access_type_id == 12) access_type = "Public on-screen access (open)";
            eosc_category = (_object_class_id == 23) ? 0 : 3;
            add_study_contribs = true;
            add_study_topics = true;
            datetime_of_data_fetch = _datetime_of_data_fetch;
        }


        public DataObject(string? _sd_oid, string? _sd_sid, string? _title, string? _display_title, int? _publication_year, int? _object_class_id,
                            string? _object_class, int? _object_type_id, string? _object_type,
                            int? _managing_org_id, string? _managing_org,
                            int? _access_type_id, string? _access_type, string? _access_details,
                            string? _access_details_url, DateTime? _url_last_checked,
                            DateTime? _datetime_of_data_fetch)
        {
            sd_oid = _sd_oid;
            sd_sid = _sd_sid;
            title = _title;
            display_title = _display_title;
            doi_status_id = 9;
            publication_year = _publication_year;
            object_class_id = _object_class_id;
            object_class = _object_class;
            object_type_id = _object_type_id;
            object_type = _object_type;
            managing_org_id = _managing_org_id;
            managing_org = _managing_org;
            lang_code = "en";
            access_type_id = _access_type_id;
            access_type = _access_type;
            access_details = _access_details;
            access_details_url = _access_details_url;
            url_last_checked = _url_last_checked;
            eosc_category = (_object_class_id == 23) ? 0 : 3;
            add_study_contribs = true;
            add_study_topics = true;
            datetime_of_data_fetch = _datetime_of_data_fetch;
        }


        public DataObject(string? _sd_oid, string? _sd_sid, string? _title, string? _display_title, int? _publication_year, int? _object_class_id,
                            string? _object_class, int? _object_type_id, string? _object_type,
                            int? _managing_org_id, string? _managing_org, string? _lang_code,
                            int? _access_type_id, string? _access_type, string? _access_details,
                            string? _access_details_url, DateTime? _url_last_checked,
                            int? _eosc_category, DateTime? _datetime_of_data_fetch)
        {
            sd_oid = _sd_oid;
            sd_sid = _sd_sid;
            title = _title;
            display_title = _display_title;
            doi_status_id = 9;
            publication_year = _publication_year;
            object_class_id = _object_class_id;
            object_class = _object_class;
            object_type_id = _object_type_id;
            object_type = _object_type;
            managing_org_id = _managing_org_id;
            managing_org = _managing_org;
            lang_code = _lang_code;
            access_type_id = _access_type_id;
            access_type = _access_type;
            access_details = _access_details;
            access_details_url = _access_details_url;
            url_last_checked = _url_last_checked;
            eosc_category = _eosc_category;
            add_study_contribs = true;
            add_study_topics = true;
            datetime_of_data_fetch = _datetime_of_data_fetch;
        }

    }


    public class ObjectDataset
    {
        public string? sd_oid { get; set; }
        public int? record_keys_type_id { get; set; }
        public string? record_keys_type { get; set; }
        public string? record_keys_details { get; set; }
        public int? deident_type_id { get; set; }
        public string? deident_type { get; set; }
        public bool? deident_direct { get; set; }
        public bool? deident_hipaa { get; set; }
        public bool? deident_dates { get; set; }
        public bool? deident_nonarr { get; set; }
        public bool? deident_kanon { get; set; }
        public string? deident_details { get; set; }
        public int? consent_type_id { get; set; }
        public string? consent_type { get; set; }
        public bool? consent_noncommercial { get; set; }
        public bool? consent_geog_restrict { get; set; }
        public bool? consent_research_type { get; set; }
        public bool? consent_genetic_only { get; set; }
        public bool? consent_no_methods { get; set; }
        public string? consent_details { get; set; }

        public ObjectDataset(string? _sd_oid,
                            int? _record_keys_type_id, string? _record_keys_type, string? _record_keys_details,
                            int? _deident_type_id, string? _deident_type, string? _deident_details,
                            int? _consent_type_id, string? _consent_type, string? _consent_details)
        {
            sd_oid = _sd_oid;
            record_keys_type_id = _record_keys_type_id;
            record_keys_type = _record_keys_type;
            record_keys_details = _record_keys_details;
            deident_type_id = _deident_type_id;
            deident_type = _deident_type;
            deident_details = _deident_details;
            consent_type_id = _consent_type_id;
            consent_type = _consent_type;
            consent_details = _consent_details;
        }

        public ObjectDataset(string? _sd_oid,
                            int? _record_keys_type_id, string? _record_keys_type, string? _record_keys_details,
                            int? _deident_type_id, string? _deident_type,
                            bool? _deident_direct, bool? _deident_hipaa, bool? _deident_dates,
                            bool? _deident_nonarr, bool? _deident_kanon, string? _deident_details,
                            int? _consent_type_id, string? _consent_type,
                            bool? _consent_noncommercial, bool? _consent_geog_restrict, bool? _consent_research_type,
                            bool? _consent_genetic_only, bool? _consent_no_methods,
                            string? _consent_details)
        {
            sd_oid = _sd_oid;
            record_keys_type_id = _record_keys_type_id;
            record_keys_type = _record_keys_type;
            record_keys_details = _record_keys_details;
            deident_type_id = _deident_type_id;
            deident_type = _deident_type;
            deident_direct = _deident_direct;
            deident_hipaa = _deident_hipaa;
            deident_dates = _deident_dates;
            deident_nonarr = _deident_nonarr;
            deident_kanon = _deident_kanon;
            deident_details = _deident_details;
            consent_type_id = _consent_type_id;
            consent_type = _consent_type;
            consent_noncommercial = _consent_noncommercial;
            consent_geog_restrict = _consent_geog_restrict;
            consent_research_type = _consent_research_type;
            consent_genetic_only = _consent_genetic_only;
            consent_no_methods = _consent_no_methods;
            consent_details = _consent_details;
        }
    }

    
    public class ObjectTitle
    {
        public string? sd_oid { get; set; }
        public int? title_type_id { get; set; }
        public string? title_type { get; set; }
        public string? title_text { get; set; }
        public string? lang_code { get; set; }
        public int? lang_usage_id { get; set; }
        public bool? is_default { get; set; }
        public string? comments { get; set; }

        public ObjectTitle(string? _sd_oid, string? _title_text,
                                int? _title_type_id, string? _title_type, bool? _is_default)
        {
            sd_oid = _sd_oid;
            title_text = _title_text;
            title_type_id = _title_type_id;
            title_type = _title_type;
            lang_code = "en";
            lang_usage_id = 11;  // default
            is_default = _is_default;
        }

        public ObjectTitle(string? _sd_oid, string? _title_text, 
                               int? _title_type_id, string? _title_type, bool? _is_default, string? _comments)
        {
            sd_oid = _sd_oid;
            title_text = _title_text;
            title_type_id = _title_type_id;
            title_type = _title_type;
            lang_code = "en";
            lang_usage_id = 11;  // default
            is_default = _is_default;
            comments = _comments;
        }

        public ObjectTitle(string? _sd_oid, string? _title_text, 
                                int? _title_type_id, string? _title_type, string? _lang_code,
                                int? _lang_usage_id, bool? _is_default, string? _comments)
        {
            sd_oid = _sd_oid;
            title_text = _title_text;
            title_type_id = _title_type_id;
            title_type = _title_type;
            lang_code = _lang_code;
            lang_usage_id = _lang_usage_id;
            is_default = _is_default;
            comments = _comments;
        }

    }


    public class ObjectInstance
    {
        public string? sd_oid { get; set; }
        public int? repository_org_id { get; set; }
        public string? repository_org { get; set; }
        public string? url { get; set; }
        public bool? url_accessible { get; set; }
        public DateTime? url_last_checked { get; set; }
        public int? resource_type_id { get; set; }
        public string? resource_type { get; set; }
        public string? resource_size { get; set; }
        public string? resource_size_units { get; set; }
        public string? resource_comments { get; set; }

        public ObjectInstance(string _sd_oid, int? _repository_org_id, string? _repository_org, 
                    string? _url, bool? _url_accessible,
                    int? _resource_type_id, string? _resource_type)
        {
            sd_oid = _sd_oid;
            repository_org_id = _repository_org_id;
            repository_org = _repository_org;
            url = _url;
            url_accessible = _url_accessible;
            resource_type_id = _resource_type_id;
            resource_type = _resource_type;
        }


        public ObjectInstance(string? _sd_oid, int? _repository_org_id, string? _repository_org, 
                    string? _url, bool? _url_accessible,
                    int? _resource_type_id, string? _resource_type,
                    string? _resource_size, string? _resource_size_units)
        {
            sd_oid = _sd_oid;
            repository_org_id = _repository_org_id;
            repository_org = _repository_org;
            url = _url;
            url_accessible = _url_accessible;
            resource_type_id = _resource_type_id;
            resource_type = _resource_type;
            resource_size = _resource_size;
            resource_size_units = _resource_size_units;
        }


        public ObjectInstance(string? _sd_oid, int? _repository_org_id, string? _repository_org, 
                    string? _url, bool? _url_accessible,
                    int? _resource_type_id, string? _resource_type,
                    string? _resource_size, string? _resource_size_units, string? _resource_comments)
        {
            sd_oid = _sd_oid;
            repository_org_id = _repository_org_id;
            repository_org = _repository_org;
            url = _url;
            url_accessible = _url_accessible;
            resource_type_id = _resource_type_id;
            resource_type = _resource_type;
            resource_size = _resource_size;
            resource_size_units = _resource_size_units;
            resource_comments = _resource_comments;
        }

        public ObjectInstance()
        { }
    }

    // (Object) Identifier class, a Data Object component

    public class ObjectIdentifier
    {
        public string? sd_oid { get; set; }
        public int? identifier_type_id { get; set; }
        public string? identifier_type { get; set; }
        public string? identifier_value { get; set; }
        public int? identifier_org_id { get; set; }
        public string? identifier_org { get; set; }
        public string? identifier_org_ror_id { get; set; }
        public string? identifier_date { get; set; }

        public ObjectIdentifier(string _sd_oid, int? _type_id, string? _type_name,
                string? _id_value, int? _org_id, string? _org_name)
        {
            sd_oid = _sd_oid;
            identifier_type_id = _type_id;
            identifier_type = _type_name;
            identifier_value = _id_value;
            identifier_org_id = _org_id;
            identifier_org = _org_name;
        }
    }


    public class ObjectDate
    {
        public string? sd_oid { get; set; }
        public int? date_type_id { get; set; }
        public string? date_type { get; set; }
        public string? date_as_string { get; set; }
        public bool? date_is_range { get; set; }
        public int? start_year { get; set; }
        public int? start_month { get; set; }
        public int? start_day { get; set; }
        public int? end_year { get; set; }
        public int? end_month { get; set; }
        public int? end_day { get; set; }
        public string? details { get; set; }

        public ObjectDate(string? _sd_oid, int? _date_type_id, string? _date_type,
                                    string? _date_as_string, int? _start_year)
        {
            sd_oid = _sd_oid;
            date_type_id = _date_type_id;
            date_type = _date_type;
            date_as_string = _date_as_string;
            start_year = _start_year;
        }

        public ObjectDate(string? _sd_oid, int? _date_type_id, string? _date_type,
                                    int? _start_year, int? _start_month, int? _start_day, string? _date_as_string)
        {
            sd_oid = _sd_oid;
            date_type_id = _date_type_id;
            date_type = _date_type;
            start_year = _start_year;
            start_month = _start_month;
            start_day = _start_day;
            date_as_string = _date_as_string;
        }

        public ObjectDate(string? _sd_oid, int? _date_type_id, string? _date_type,
                                    string? _date_as_string, bool? _date_is_range,
                                    int? _start_year, int? _start_month, int? _start_day,
                                    int? _end_year, int? _end_month, int? _end_day,
                                    string? _details)
        {
            sd_oid = _sd_oid;
            date_type_id = _date_type_id;
            date_type = _date_type;
            date_as_string = _date_as_string;
            date_is_range = _date_is_range;
            start_year = _start_year;
            start_month = _start_month;
            start_day = _start_day;
            end_year = _end_year;
            end_month = _end_month;
            end_day = _end_day;
            details = _details;
        }


        public ObjectDate(string? _sd_oid, int? _date_type_id, string? _date_type,
                                    SplitDate sd)
        {
            sd_oid = _sd_oid;
            date_type_id = _date_type_id;
            date_type = _date_type;
            start_year = sd.year;
            start_month = sd.month;
            start_day = sd.day;
            date_as_string = sd.date_string;
        }


        public ObjectDate(string? _sd_oid, int? _date_type_id, string? _date_type,
                                    SplitDateRange sdr)
        {
            sd_oid = _sd_oid;
            date_type_id = _date_type_id;
            date_type = _date_type;
            date_as_string = sdr.date_string;
            date_is_range = sdr.date_is_range;
            start_year = sdr.syear;
            start_month = sdr.smonth;
            start_day = sdr.sday;
            end_year = sdr.eyear;
            end_month = sdr.emonth;
            end_day = sdr.eday;
        }

    }



    public class SplitDate
    {
        public int? year;
        public int? month;
        public int? day;
        public string? date_string;

        public SplitDate(int? _year, int? _month, int? _day, string? _date_string)
        {
            year = _year;
            month = _month;
            day = _day;
            date_string = _date_string;
        }
    }


    public class SplitDateRange
    {
        public int? syear;
        public int? smonth;
        public int? sday;
        public int? eyear;
        public int? emonth;
        public int? eday;
        public bool? date_is_range;
        public string? date_string;

        public SplitDateRange(int? _syear, int? _smonth, int? _sday,
                         int? _eyear, int? _emonth, int? _eday, 
                         bool? _date_is_range, string? _date_string)
        {
            syear = _syear;
            smonth = _smonth;
            sday = _sday;
            eyear = _eyear;
            emonth = _emonth;
            eday = _eday;
            date_is_range = _date_is_range;
            date_string = _date_string;
        }
    }


    public class ObjectPublicationType
    {
        public string? sd_oid { get; set; }
        public string? type_name { get; set; }

        public ObjectPublicationType(string? _sd_oid, string? _type_name)
        {
            sd_oid = _sd_oid;
            type_name = _type_name;
        }
    }


    public class ObjectDescription
    {
        public string? sd_oid { get; init; }
        public int? description_type_id { get; set; }
        public string? description_type { get; set; }
        public string? label { get; set; }
        public string? description_text { get; set; }
        public string? lang_code { get; set; }
    }


    // (Object) DBLink class, a Data Object component

    public class ObjectDBLink
    {
        public string? sd_oid { get; set; }
        public int? db_sequence { get; set; }
        public string? db_name { get; set; }
        public string? id_in_db { get; set; }

        public ObjectDBLink(string? _sd_oid, int? _db_sequence, 
                           string? _db_name, string? _id_in_db)
        {
            sd_oid = _sd_oid;
            db_sequence = _db_sequence;
            db_name = _db_name;
            id_in_db = _id_in_db;
        }
    }

    // (Object) Comment Correction class, a Data Object component

    public class ObjectComment
    {
        public string? sd_oid { get; set; }
        public string? ref_type { get; set; }
        public string? ref_source { get; set; }
        public string? pmid { get; set; }
        public string? pmid_version { get; set; }
        public string? notes { get; set; }

        public ObjectComment(string? _sd_oid, string? _ref_type, string? _ref_source, 
                             string? _pmid, string? _pmid_version, string? _notes)
        {
            sd_oid = _sd_oid;
            ref_type = _ref_type;
            ref_source = _ref_source;
            pmid = _pmid;
            pmid_version = _pmid_version;
            notes = _notes;
        }
    }


    [Table("sd.object_topics")]
    public class ObjectTopic
    {
        public string? sd_oid { get; set; }
        public int? topic_type_id { get; set; }
        public string? topic_type { get; set; }
        public bool? mesh_coded { get; set; }
        public string? mesh_code { get; set; }
        public string? mesh_value { get; set; }
        public int? original_ct_id { get; set; }
        public string? original_ct_code { get; set; }
        public string? original_value { get; set; }

        // used for a mesh coded topic

        public ObjectTopic(string? _sd_oid, int? _topic_type_id, string? _topic_type,
                     bool? _mesh_coded, string? _mesh_code, string? _mesh_value)
        {
            sd_oid = _sd_oid;
            topic_type_id = _topic_type_id;
            topic_type = _topic_type;
            mesh_coded = _mesh_coded;
            mesh_code = _mesh_code;
            mesh_value = _mesh_value;
            original_ct_id = 14;
            original_ct_code = _mesh_code;
            original_value = _mesh_value;
        }

        // non mesh coded topic - topic type and name only

        public ObjectTopic(string? _sd_oid, int? _topic_type_id, string? _topic_type,
                           string? _topic_value)
        {
            sd_oid = _sd_oid;
            topic_type_id = _topic_type_id;
            topic_type = _topic_type;
            mesh_coded = false;
            original_ct_id = 0;
            original_value = _topic_value;
        }

        // non mesh coded topic - but coded using another system - comments also possible

        public ObjectTopic(string? _sd_oid, int? _topic_type_id, string? _topic_type, 
            string? _topic_value, int? _original_ct_id, string? _original_ct_code)
        {
            sd_oid = _sd_oid;
            topic_type_id = _topic_type_id;
            topic_type = _topic_type;
            mesh_coded = false;
            original_ct_id = _original_ct_id;
            original_ct_code = _original_ct_code;
            original_value = _topic_value;
        }

    }

      
    public class ObjectContributor
    {
        public string? sd_oid { get; set; }
        public int? contrib_type_id { get; set; }
        public string? contrib_type { get; set; }
        public bool? is_individual { get; set; }
        public int? person_id { get; set; }
        public string? person_given_name { get; set; }
        public string? person_family_name { get; set; }
        public string? person_full_name { get; set; }
        public string? orcid_id { get; set; }
        public string? person_affiliation { get; set; }
        public int? organisation_id { get; set; }
        public string? organisation_name { get; set; }
        public string? organisation_ror_id { get; set; }

        public ObjectContributor(string? _sd_oid, int? _contrib_type_id, string? _contrib_type,
                                int? _organisation_id, string? _organisation_name, string? _person_full_name,
                                string? _person_affiliation)
        {
            sd_oid = _sd_oid;
            contrib_type_id = _contrib_type_id;
            contrib_type = _contrib_type;
            is_individual = (_person_full_name != null);
            organisation_id = _organisation_id;
            organisation_name = _organisation_name;
            person_full_name = _person_full_name;
            person_affiliation = _person_affiliation;
        }


        public ObjectContributor(string? _sd_oid, int? _contrib_type_id, string? _contrib_type, 
                                 string? _person_given_name, string? _person_family_name, 
                                 string? _person_full_name, string? _orcid_id, string? _person_affiliation, 
                                 string? _organisation_name)
        {
            sd_oid = _sd_oid;
            contrib_type_id = _contrib_type_id;
            contrib_type = _contrib_type;
            is_individual = (_person_full_name != null);
            person_given_name = _person_given_name;
            person_family_name = _person_family_name;
            person_full_name = _person_full_name;
            orcid_id = _orcid_id;
            person_affiliation = _person_affiliation;
            organisation_name = _organisation_name;
        }


        public ObjectContributor(string? _sd_oid, int? _contrib_type_id, string? _contrib_type,
                                string? _person_given_name, string? _person_family_name, string? _person_full_name,
                                string? _orcid_id, string? _person_affiliation, 
                                int? _organisation_id, string? _organisation_name)
        {
            sd_oid = _sd_oid;
            contrib_type_id = _contrib_type_id;
            contrib_type = _contrib_type;
            is_individual = (_person_full_name == null) ? false : true;
            person_given_name = _person_given_name;
            person_family_name = _person_family_name;
            person_full_name = _person_full_name;
            orcid_id = _orcid_id;
            person_affiliation = _person_affiliation;
            organisation_id = _organisation_id;
            organisation_name = _organisation_name;
        }
    }

    

    // The Object Right class

    public class ObjectRight
    {
        public string? sd_oid { get; set; }
        public string? right_name { get; set; }
        public string? right_uri { get; set; }
        public string? notes { get; set; }

        public ObjectRight(string? _sd_oid, string? _right_name,
                            string? _right_uri, string? _notes)
        {
            sd_oid = _sd_oid;
            right_name = _right_name;
            right_uri = _right_uri;
            notes = _notes;
        }
    }

    // The Object relationship class

    public class ObjectRelationship
    {
        public string? sd_oid { get; set; }
        public int? relationship_type_id { get; set; }
        public string? relationship_type { get; set; }
        public string? target_sd_oid { get; set; }

        public ObjectRelationship(string? _sd_oid, int? _relationship_type_id, 
                                  string? _relationship_type, string? _target_sd_oid)
        {
            sd_oid = _sd_oid;
            relationship_type_id = _relationship_type_id;
            relationship_type = _relationship_type;
            target_sd_oid = _target_sd_oid;
        }
    }


    [Table("sd.journal_details")]
    public class JournalDetails
    {
        public string? sd_oid { get; set; }
        public string? journal_title { get; set; }
        public string? pissn { get; set; }
        public string? eissn { get; set; }

        public JournalDetails(string _sd_oid, string _journal_title)
        {
            sd_oid = _sd_oid;
            journal_title = _journal_title;
        }
    }

}
