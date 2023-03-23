using Dapper.Contrib.Extensions;
using Npgsql;
using PostgreSQLCopyHelper;

namespace MDR_Harvester;

public class StorageDataLayer : IStorageDataLayer
{
    private readonly IStudyCopyHelpers _sch ;
    private readonly IObjectCopyHelpers _och;
    private string? db_conn;

    public StorageDataLayer(IStudyCopyHelpers sch, IObjectCopyHelpers och)
    {
        _sch = sch;
        _och = och;
    }

    public void StoreFullStudy(Study s, Source source)
    {
        db_conn = source.db_conn;
        using NpgsqlConnection conn = new(db_conn);
        conn.Open();

        // Store study.

        StudyInDB st_db = new StudyInDB(s);
        conn.Insert(st_db);

        // Store study attributes
        // These common to all databases.

        if (s.identifiers?.Count > 0)
        {
            _sch.studyIdentifiersHelper.SaveAll(conn, s.identifiers);
        }

        if (s.titles?.Count > 0)
        {
            _sch.studyTitlesHelper.SaveAll(conn, s.titles);
        }

        // These are database dependent.

        if (source.has_study_topics is true && s.topics?.Count > 0)
        {
            _sch.studyTopicsHelper.SaveAll(conn, s.topics);
        }

        if (source.has_study_features is true && s.features?.Count > 0)
        {
            _sch.studyFeaturesHelper.SaveAll(conn, s.features);
        }

        if (source.has_study_conditions is true && s.conditions?.Count > 0)
        {
            _sch.studyConditionsHelper.SaveAll(conn, s.conditions);
        }

        if (source.has_study_iec is true && s.iec?.Count > 0)
        {
            if (source.study_iec_storage_type! == "Single Table")
            {
                _sch.studyIECHelper.SaveAll(conn, s.iec);
            }
            else
            {
                StoreIEC(conn, source.study_iec_storage_type!, s.iec, s.study_start_year);
            }
        }

        if (source.has_study_organisations is true && s.organisations?.Count > 0)
        {
            _sch.studyOrganisationsHelper.SaveAll(conn, s.organisations);
        }

        if (source.has_study_people is true && s.people?.Count > 0)
        {
            _sch.studyPeopleHelper.SaveAll(conn, s.people);
        }

        if (source.has_study_references is true && s.references?.Count > 0)
        {
            _sch.studyReferencesHelper.SaveAll(conn, s.references);
        }

        if (source.has_study_relationships is true && s.relationships?.Count > 0)
        {
            _sch.studyRelationshipsHelper.SaveAll(conn, s.relationships);
        }

        if (source.has_study_countries is true && s.countries?.Count > 0)
        {
            _sch.studyCountriesHelper.SaveAll(conn, s.countries);
        }

        if (source.has_study_locations is true && s.sites?.Count > 0)
        {
            _sch.studyLocationsHelper.SaveAll(conn, s.sites);
        }

        if (source.has_study_links is true && s.studylinks?.Count > 0)
        {
            _sch.studyLinksHelper.SaveAll(conn, s.studylinks);
        }

        if (source.has_study_ipd_available is true && s.ipd_info?.Count > 0)
        {
            _sch.studyAvailIPDHelper.SaveAll(conn, s.ipd_info);
        }

        // Store linked data objects.

        if (s.data_objects?.Count > 0)
        {
            _och.dataObjectsHelper.SaveAll(conn, s.data_objects);
        }

        // Store data object attributes - these common to all databases.

        if (s.object_instances?.Count > 0)
        {
            _och.objectInstancesHelper.SaveAll(conn, s.object_instances);
        }

        if (s.object_titles?.Count > 0)
        {
            _och.objectTitlesHelper.SaveAll(conn, s.object_titles);
        }

        // These are database dependent.	

        if (source.has_object_datasets is true && s.object_datasets?.Count > 0)
        {
            _och.objectDatasetsHelper.SaveAll(conn, s.object_datasets);
        }

        if (source.has_object_dates is true && s.object_dates?.Count > 0)
        {
            _och.objectDatesHelper.SaveAll(conn, s.object_dates);
        }
        conn.Close();
    }


    private void StoreIEC(NpgsqlConnection conn, string storage_type, 
                        List<StudyIEC> iec, int? study_start_year)
    {
        string target_table = "";        
        if  (storage_type == "By Year Groupings")
        {
            if (study_start_year is null or < 2012)
            {
                target_table = "study_iec_pre12";
            }
            else if (study_start_year is >= 2013 and <= 2019)
            {
                target_table = "study_iec_13to19";
            }
            else
            {
                target_table = "study_iec_20on";
            }
        }
        else if (storage_type == "By Years")
        {
            if (study_start_year is > 2014 and <= 2030)
            {
                target_table = "study_iec_" + study_start_year.ToString()?[2..3];
            }
            else if (study_start_year is null or > 2030)
            {
                target_table = "study_iec_null";
            }
            else if (study_start_year < 2006)
            {
                target_table = "study_iec_pre06";
            }
            else if (study_start_year is >= 2006 and <= 2008)
            {
                target_table = "study_iec_0608";
            } 
            else if (study_start_year is 2009 or 2010)
            {
                target_table = "study_iec_0910";
            } 
            else if (study_start_year is 2011 or 2012)
            {
                target_table = "study_iec_1112";
            } 
            else if (study_start_year is 2013 or 2014)
            {
                target_table = "study_iec_1314";
            } 
        }

        if (target_table != "")
        {
            PostgreSQLCopyHelper<StudyIEC> studyIECHelper =
                new PostgreSQLCopyHelper<StudyIEC>("sd", target_table)
                    .MapVarchar("sd_sid", x => x.sd_sid)
                    .MapReal("seq_num", x => x.seq_num)
                    .MapInteger("iec_type_id", x => x.iec_type_id)
                    .MapVarchar("iec_type", x => x.iec_type)
                    .MapVarchar("split_type", x => x.split_type)
                    .MapVarchar("leader", x => x.leader)
                    .MapInteger("indent_level", x => x.indent_level)
                    .MapInteger("level_seq_num", x => x.level_seq_num)
                    .MapVarchar("iec_text", x => x.iec_text);

            studyIECHelper.SaveAll(conn, iec);
        }
    }


    public void StoreFullObject(FullDataObject r, Source source)
    {
        db_conn = source.db_conn;
        using NpgsqlConnection conn = new(db_conn);
        conn.Open();      
        
        DataObject d = new DataObject(r);
        conn.Insert(d);

        if (r.object_instances?.Count > 0)
        {
            _och.objectInstancesHelper.SaveAll(conn, r.object_instances);
        }

        if (r.object_titles?.Count > 0)
        {
            _och.objectTitlesHelper.SaveAll(conn, r.object_titles);
        }

        // these are database dependent		

        if (source.has_object_dates is true && r.object_dates?.Count > 0)
        {
            _och.objectDatesHelper.SaveAll(conn, r.object_dates);
        }

        if (source.has_object_relationships is true && r.object_relationships?.Count > 0)
        {
            _och.objectRelationshipsHelper.SaveAll(conn, r.object_relationships);
        }

        if (source.has_object_rights is true && r.object_rights?.Any() is true)
        {
            _och.objectRightsHelper.SaveAll(conn, r.object_rights);
        }

        if (source.has_object_pubmed_set is true)
        {
            if (r.object_organisations?.Count > 0)
            {
                _och.objectOrganisationsHelper.SaveAll(conn, r.object_organisations);
            }
            
            if (r.object_people?.Count > 0)
            {
                _och.objectPeopleHelper.SaveAll(conn, r.object_people);
            }

            if (r.object_topics?.Any() is true)
            {
                _och.objectTopicsHelper.SaveAll(conn, r.object_topics);
            }

            if (r.object_comments?.Any() is true)
            {
                _och.objectCommentsHelper.SaveAll(conn, r.object_comments);
            }

            if (r.object_descriptions?.Any() is true)
            {
                _och.objectDescriptionsHelper.SaveAll(conn, r.object_descriptions);
            }

            if (r.object_identifiers?.Any() is true)
            {
                _och.objectIdentifiersHelper.SaveAll(conn, r.object_identifiers);
            }

            if (r.object_db_ids?.Any() is true)
            {
                _och.objectDbLinksHelper.SaveAll(conn, r.object_db_ids);
            }

            if (r.object_pubtypes?.Any() is true)
            {
                _och.objectPubTypesHelper.SaveAll(conn, r.object_pubtypes);
            }

            if (r.journal_details != null)
            {
                conn.Insert(r.journal_details);
            }
        }
        
        conn.Close();  
    }
}