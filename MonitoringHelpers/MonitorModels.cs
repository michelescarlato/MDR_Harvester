using Dapper.Contrib.Extensions;

namespace MDR_Harvester;

[Table("sf.source_parameters")]
public class Source
{
    public int id { get; }
    public string? source_type { get; }
    public int? preference_rating { get; }
    public string? database_name { get; }
    public string? repo_name { get; }
    public string? db_conn { get; set; }
    public bool? uses_who_harvest { get; }
    public int? harvest_chunk { get; }
    public string? local_folder { get; }
    public bool? local_files_grouped { get; }
    public int? grouping_range_by_id { get; }
    public string? local_file_prefix { get; }
    public bool? has_study_tables { get; }
    public bool? has_study_topics { get; }
    public bool? has_study_conditions { get; }
    public bool? has_study_features { get; }
    public bool? has_study_iec{ get; }
    public string? study_iec_storage_type { get; }
    public bool? has_study_organisations { get; }
    public bool? has_study_people { get; }
    public bool? has_study_references { get; }
    public bool? has_study_relationships { get; }
    public bool? has_study_links { get; }
    public bool? has_study_countries { get; }
    public bool? has_study_locations { get; }
    public bool? has_study_ipd_available { get; }
    public bool? has_object_datasets { get; }
    public bool? has_object_dates { get; }
    public bool? has_object_relationships { get; }
    public bool? has_object_rights { get; }
    public bool? has_object_pubmed_set { get; }
    public bool? has_object_bbmri_set { get; }
}


[Table("sf.harvest_events")]
public class HarvestEvent
{
    [ExplicitKey]
    public int? id { get; set; }
    public int? source_id { get; set; }
    public int? type_id { get; set; }
    public DateTime? time_started { get; set; }
    public DateTime? time_ended { get; set; }
    public int? num_records_available { get; set; }
    public int? num_records_harvested { get; set; }
    public string? comments { get; set; }

    public HarvestEvent(int _id, int _source_id, int _type_id)
    {
        id = _id;
        source_id = _source_id;
        type_id = _type_id;
        time_started = DateTime.Now;
    }

    public HarvestEvent() { }
}

[Table("mn.source_data")]
public class StudyFileRecord
{
    public int? id { get; set; }
    public string? sd_sid { get; set; }
    public string? remote_url { get; set; }
    public DateTime? last_revised { get; set; }
    public bool? assume_complete { get; set; }
    public int? download_status { get; set; }
    public string? local_path { get; set; }
    public int? last_dl_id { get; set; }
    public DateTime? last_downloaded { get; set; }
    public int? last_harvest_id { get; set; }
    public DateTime? last_harvested { get; set; }
    public int? last_import_id { get; set; }
    public DateTime? last_imported { get; set; }

    // constructor when a revision data can be expected (not always there)
    public StudyFileRecord(string? _sd_sid, string? _remote_url, int? _last_dl_id,
                                          DateTime? _last_revised, string? _local_path)
    {
        sd_sid = _sd_sid;
        remote_url = _remote_url;
        last_dl_id = _last_dl_id;
        last_revised = _last_revised;
        download_status = 2;
        last_downloaded = DateTime.Now;
        local_path = _local_path;
    }

    // constructor when an 'assumed complete' judgement can be expected (not always there)
    public StudyFileRecord(string? _sd_sid, string? _remote_url, int? _last_dl_id,
                                          bool? _assume_complete, string? _local_path)
    {
        sd_sid = _sd_sid;
        remote_url = _remote_url;
        last_dl_id = _last_dl_id;
        assume_complete = _assume_complete;
        download_status = 2;
        last_downloaded = DateTime.Now;
        local_path = _local_path;
    }

    public StudyFileRecord()
    { }
}


[Table("mn.source_data")]
public class ObjectFileRecord
{
    public int? id { get; set; }
    public string? sd_oid { get; set; }
    public string? remote_url { get; set; }
    public DateTime? last_revised { get; set; }
    public bool? assume_complete { get; set; }
    public int? download_status { get; set; }
    public string? local_path { get; set; }
    public int? last_dl_id { get; set; }
    public DateTime? last_downloaded { get; set; }
    public int? last_harvest_id { get; set; }
    public DateTime? last_harvested { get; set; }
    public int? last_import_id { get; set; }
    public DateTime? last_imported { get; set; }

    // constructor when a revision data can be expected (not always there)
    public ObjectFileRecord(string? _sd_oid, string? _remote_url, int? _last_dl_id,
                                          DateTime? _last_revised, string? _local_path)
    {
        sd_oid = _sd_oid;
        remote_url = _remote_url;
        last_dl_id = _last_dl_id;
        last_revised = _last_revised;
        download_status = 2;
        last_downloaded = DateTime.Now;
        local_path = _local_path;
    }

    // constructor when an 'assumed complete' judgement can be expected (not always there)
    public ObjectFileRecord(string _sd_oid, string _remote_url, int _last_dl_id,
                                          bool? _assume_complete, string _local_path)
    {
        sd_oid = _sd_oid;
        remote_url = _remote_url;
        last_dl_id = _last_dl_id;
        assume_complete = _assume_complete;
        download_status = 2;
        last_downloaded = DateTime.Now;
        local_path = _local_path;
    }

    public ObjectFileRecord()
    { }

}