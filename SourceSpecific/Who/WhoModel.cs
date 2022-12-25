using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MDR_Harvester.Who;

public class WHORecord
{
    public int source_id { get; set; }
    public string? record_date { get; set; }
    public string? sd_sid { get; set; }
    public string? public_title { get; set; }
    public string? scientific_title { get; set; }
    public string? remote_url { get; set; }
    public string? public_contact_givenname { get; set; }
    public string? public_contact_familyname { get; set; }
    public string? public_contact_email { get; set; }
    public string? public_contact_affiliation { get; set; }
    public string? scientific_contact_givenname { get; set; }
    public string? scientific_contact_familyname { get; set; }
    public string? scientific_contact_email { get; set; }
    public string? scientific_contact_affiliation { get; set; }
    public string? study_type { get; set; }
    public string? date_registration { get; set; }
    public string? date_enrolment { get; set; }
    public string? target_size { get; set; }
    public string? study_status { get; set; }
    public string? primary_sponsor { get; set; }
    public string? secondary_sponsors { get; set; }
    public string? source_support { get; set; }
    public string? interventions { get; set; }
    public string? agemin { get; set; }
    public string? agemin_units { get; set; }
    public string? agemax { get; set; }
    public string? agemax_units { get; set; }
    public string? gender { get; set; }
    public string? inclusion_criteria { get; set; }
    public string? exclusion_criteria { get; set; }
    public string? primary_outcome { get; set; }
    public string? secondary_outcomes { get; set; }
    public string? bridging_flag { get; set; }
    public string? bridged_type { get; set; }
    public string? childs { get; set; }
    public string? type_enrolment { get; set; }
    public string? retrospective_flag { get; set; }
    public string? results_actual_enrollment { get; set; }
    public string? results_url_link { get; set; }
    public string? results_summary { get; set; }
    public string? results_date_posted { get; set; }
    public string? results_date_first_publication { get; set; }
    public string? results_url_protocol { get; set; }
    public string? ipd_plan { get; set; }
    public string? ipd_description { get; set; }
    public string? results_date_completed { get; set; }
    public string? results_yes_no { get; set; }
    public string? folder_name { get; set; }

    public string? design_string { get; set; }
    public string? phase_string { get; set; }

    public List<string>? country_list { get; set; }
    public List<Secondary_Id>? secondary_ids { get; set; }
    public List<StudyFeature>? study_features { get; set; }
    public List<StudyCondition>? condition_list { get; set; }
}

public class Secondary_Id
{
    public string? source_field { get; set; }
    public string? sec_id { get; set; }
    public string? processed_id { get; set; }
    public int? sec_id_source { get; set; }

    public bool? ShouldSerializesec_id_source()
    {
        return sec_id_source.HasValue;
    }

    public Secondary_Id(string? _source_field, string? _sec_id,
                        string? _processed_id, int? _sec_id_source)
    {
        source_field = _source_field;
        sec_id = _sec_id;
        processed_id = _processed_id;
        sec_id_source = _sec_id_source;
    }

    public Secondary_Id()
    { }
}


public class SecIdBase
{
    public string? processed_id { get; set; }
    public int? sec_id_source { get; set; }

    public SecIdBase(string? _processed_id, int? _sec_id_source)
    {
        processed_id = _processed_id;
        sec_id_source = _sec_id_source;
    }

    public SecIdBase()
    { }
}


public class StudyFeature
{
    public int? ftype_id { get; set; }
    public string? ftype { get; set; }
    public int? fvalue_id { get; set; }
    public string? fvalue { get; set; }

    public StudyFeature(int? _ftype_id, string? _ftype,
                        int? _fvalue_id, string? _fvalue)
    {
        ftype_id = _ftype_id;
        ftype = _ftype;
        fvalue_id = _fvalue_id;
        fvalue = _fvalue;
    }

    public StudyFeature()
    { }
}

public class StudyCondition
{
    public string? condition { get; set; }
    public string? code { get; set; }
    public string? code_system { get; set; }

    public StudyCondition(string? _condition)
    {
        condition = _condition;
    }

    public StudyCondition(string? _condition,
                           string? _code, string? _code_system)
    {
        condition = _condition;
        code = _code;
        code_system = _code_system;
    }

    public StudyCondition()
    { }
}

