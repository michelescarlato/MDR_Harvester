namespace MDR_Harvester.Euctr;

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
    public List<Identifier>? identifiers { get; set; }
    public List<EMAFeature>? features { get; set; }
    public List<EMACondition>? conditions { get; set; }
    public List<EMAImp>? imp_topics { get; set; }
    public List<EMAOrganisation>? organisations { get; set; }
    public List<MeddraTerm>? meddra_terms { get; set; }

    public Euctr_Record()
    { }
}


public class MeddraTerm
{
    public string? version { get; set; }
    public string? soc_term { get; set; }
    public string? code { get; set; }
    public string? term { get; set; }
    public string? level { get; set; }

    public MeddraTerm()
    { }
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
    
    public EMACountry()
    { }
}

/*
public class EMAIdentifier
{
    public int? identifier_type_id { get; set; }
    public string? identifier_type { get; set; }
    public string? identifier_value { get; set; }
    public int? source_id { get; set; }
    public string? source { get; set; }

    public EMAIdentifier(int? identifierTypeId, string? identifierType, 
                        string? identifierValue, int? _source_id, string? _source)
    {
        identifier_type_id = identifierTypeId;
        identifier_type = identifierType;
        identifier_value = identifierValue;
        source_id = _source_id;
        source = _source;
    }
    
    public EMAIdentifier()
    { }
}
*/

public class Identifier
{
    public int? identifier_type_id { get; set; }
    public string? identifier_type { get; set; }
    public string? identifier_value { get; set; }
    public int? identifier_org_id { get; set; }
    public string? identifier_org { get; set; }

    public Identifier(int? identifierTypeId, string? identifierType, 
        string? identifierValue, int? identifierOrgId, string? identifierOrg)
    {
        identifier_type_id = identifierTypeId;
        identifier_type = identifierType;
        identifier_value = identifierValue;
        identifier_org_id = identifierOrgId;
        identifier_org = identifierOrg;
    }
    
    public Identifier()
    { }
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
    
    public EMAFeature()
    { }
}

public class EMACondition
{
    public string? condition_name { get; set; }
    public int? condition_ct_id { get; set; }
    public string? condition_ct { get; set; }
    public string? condition_ct_code { get; set; }

    public EMACondition(string? conditionName)
    {
        condition_name = conditionName;
    }
    
    public EMACondition()
    { }
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

    public EMAImp()
    { }
    
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
    
    public EMAOrganisation()
    { }
}
