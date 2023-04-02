using System.Xml.Serialization;
namespace MDR_Harvester.Euctr;

/*public class Euctr_Record
{

    public string? sd_sid { get; set; }
    public string? sponsor_id { get; set; }
    public string? sponsor_name { get; set; }
    public string? member_state { get; set; }
    public string? start_date { get; set; }
    public string? inclusion_criteria { get; set; }
    public string? exclusion_criteria { get; set; }
    public string? trial_status { get; set; }
    public string? medical_condition { get; set; }
    public string? population_age { get; set; }
    public string? gender { get; set; }
    public string? minage { get; set; }
    public string? maxage { get; set; }
    public string? details_url { get; set; }
    public string? results_url { get; set; }
    public string? results_version { get; set; }
    public string? results_first_date { get; set; }
    public string? results_revision_date { get; set; }
    public string? results_summary_link { get; set; }
    public string? results_summary_name { get; set; }
    public string? results_pdf_link { get; set; }
    public string? entered_in_db { get; set; }

    public List<MeddraTerm>? meddra_terms { get; set; }
    public List<DetailLine>? identifiers { get; set; }
    public List<DetailLine>? sponsors { get; set; }
    public List<ImpLine>? imps { get; set; }
    public List<DetailLine>? features { get; set; }
    public List<Country>? countries { get; set; }

    public Euctr_Record(string? _sd_sid)
    {
        sd_sid = _sd_sid;
    }
    
    public Euctr_Record()
    { }
}
*/
public class Euctr_Record
{
    public string sd_sid { get; set; } = null!;
    public string? study_type { get; set; }
    public string? sponsors_id { get; set; }
    public string? sponsor_name { get; set; }

    public string? date_registration { get; set; }
    public string? start_date { get; set; }
    public string? member_state { get; set; }
    public string? primary_objectives { get; set; }
    public string? primary_endpoints { get; set; }
    public string? trial_status { get; set; }
    public string? recruitment_status { get; set; }

    public string? scientific_title { get; set; }
    public string? public_title { get; set; }
    public string? acronym { get; set; }
    public string? scientific_acronym { get; set; }

    public string? target_size { get; set; }
    public string? results_actual_enrolment { get; set; }
    public string? minage { get; set; }
    public string? maxage { get; set; }
    public string? gender { get; set; }
    public string? inclusion_criteria { get; set; }
    public string? exclusion_criteria { get; set; }

    public string? medical_condition { get; set; }
    public string? population_age { get; set; }

    public string? search_url { get; set; }
    public string? details_url { get; set; }

    public string? results_url { get; set; }
    public string? results_version { get; set; }
    public string? results_date_posted { get; set; }
    public string? results_revision_date { get; set; }
    public string? results_summary_link { get; set; }
    public string? results_summary_name { get; set; }
    public string? results_pdf_link { get; set; }
    public string? results_url_protocol { get; set; }

    public string? results_IPD_plan { get; set; }
    public string? results_IPD_description { get; set; }

    public List<EMACountry>? countries { get; set; }
    public List<EMAIdentifier>? identifiers { get; set; }
    public List<EMAFeature>? features { get; set; }
    public List<EMACondition>? conditions { get; set; }
    public List<EMAImp>? imp_topics { get; set; }
    public List<EMAOrganisation>? organisations { get; set; }
    public List<MeddraTerm>? meddra_terms { get; set; }

}



public class MeddraTerm
{
    public string? version { get; set; }
    public string? soc_term { get; set; }
    public string? code { get; set; }
    public string? term { get; set; }
    public string? level { get; set; }
}


public class DetailLine
{
    public string? item_code { get; set; }
    public string? item_name { get; set; }
    public int item_number { get; set; }

    [XmlArray("values")]
    [XmlArrayItem("value")]
    public List<item_value>? item_values { get; set; }
}

public class ImpLine
{
    public int imp_number { get; set; }
    public string? item_code { get; set; }
    public string? item_name { get; set; }
    public int item_number { get; set; }

    [XmlArray("values")]
    [XmlArrayItem("value")]
    public List<item_value>? item_values { get; set; }
}

public class item_value
{
    [XmlText]
    public string? value { get; set; }

    public item_value(string _value)
    {
        value = _value;
    }

    public item_value()
    { }
}


public class file_record
{
    public int id { get; set; }
    public string? local_path { get; set; }

}


class IMP
{
    public int num { get; set; }
    public string? product_name { get; set; }
    public string? trade_name { get; set; }
    public string? inn { get; set; }

    public IMP(int _num)
    {
        num = _num;
    }
}


public class Country
{
    public string? name { get; set; }
    public string? status { get; set; }

    public Country()
    { }

    public Country(string? _name, string? _status)
    {
        name = _name;
        status = _status;
    }
}



public class EMACountry
{
    public string? country_name { get; set; }
    public string? status { get; set; }

    public EMACountry(string? _country_name, string? _status)
    {
        country_name = _country_name;
        status = _status;
    }
}


public class EMAIdentifier
{
    //public string? utrn { get; set; } 
    //public Secondary_id[]? secondary_ids { get; set; }
    
    public int? identifier_type_id { get; set; }
    public string? identifier_type { get; set; }
    public string? identifier_value { get; set; }
    public int? identifier_org_id { get; set; }
    public string? identifier_org { get; set; }

    public EMAIdentifier(int? identifierTypeId, string? identifierType, 
                        string? identifierValue, int? identifierOrgId, string? identifierOrg)
    {
        identifier_type_id = identifierTypeId;
        identifier_type = identifierType;
        identifier_value = identifierValue;
        identifier_org_id = identifierOrgId;
        identifier_org = identifierOrg;
    }
}

public class EMAFeature
{
    public int? feature_id { get; set; }
    public string? feature_name { get; set; }
    public int? feature_value_id { get; set; }
    public string? feature_value_name { get; set; }

    public EMAFeature(int? featureId, string? featureName, int? featureValueId, 
                              string? featureValueName)
    {
        feature_id = featureId;
        feature_name = featureName;
        feature_value_id = featureValueId;
        feature_value_name = featureValueName;
    }
    
    //public string? study_design { get; set; }
    //public string? phaseField { get; set; }
}

public class EMACondition
{
    //public string? hc_freetext { get; set; }
    //public string[]? health_condition_keyword { get; set; }
    public string? condition_name { get; set; }
    public int? condition_ct_id { get; set; }
    public string? condition_ct { get; set; }
    public string? condition_ct_code { get; set; }

    public EMACondition(string? conditionName)
    {
        condition_name = conditionName;
    }
}

public class EMAImp
{
    //public string? i_freetext { get; set; }
    //public Intervention_code? intervention_code { get; set; }
    //public Intervention_keyword? intervention_keyword { get; set; }

    
    public int? imp_num { get; set; }
    public string? trade_name { get; set; }
    public string? product_name { get; set; }
    public string? inn { get; set; }
    public string? cas_number { get; set; }

    public EMAImp(int? _imp_num)
    {
        imp_num = _imp_num;
    }

    public EMAImp(int? _imp_num, string? _trade_name, string? _product_name, 
                  string? _inn, string? _cas_number)
    {
        imp_num = _imp_num;
        trade_name = _trade_name;
        product_name = _product_name;
        inn = _inn;
        cas_number = _cas_number;
    }

}

public class EMAOrganisation
{
    //public string? primary_sponsor { get; set; }
    //public string[]? secondary_sponsor { get; set; }
    //public string[]? source_support { get; set; }

    public int? org_role_id { get; set; }
    public string? org_role { get; set; }
    public string? org_name { get; set; }    
    
    public EMAOrganisation(int? orgRoleId, string? orgRole, string? orgName)
    {
         org_role_id = orgRoleId;
         org_role = orgRole;
         org_name = orgName;
    }
}
