using Dapper;
using Npgsql;

namespace MDR_Harvester;

class TransferSDDataBuilder
{
    private readonly int _source_id;
    private readonly string _db_conn = "";
    private readonly ISource _source;

    public TransferSDDataBuilder(ISource source)
    {
        _source = source;
        if (source.id.HasValue)
        {
            // as it always should have..
            _source_id = (int)source.id!;           
            _db_conn = source.db_conn ?? "";
        }
    }


    public void DeleteExistingStudyData()
    {
        int study_num = DeleteData(_source_id, "studies");
        if (study_num > 0)
        {
            DeleteData(_source_id, "study_identifiers");
            DeleteData(_source_id, "study_titles");
            DeleteData(_source_id, "study_hashes");

            // these are database dependent

            if (_source.has_study_topics is true) DeleteData(_source_id, "study_topics");
            if (_source.has_study_features is true) DeleteData(_source_id, "study_features");
            if (_source.has_study_contributors is true) DeleteData(_source_id, "study_contributors");
            if (_source.has_study_references is true) DeleteData(_source_id, "study_references");
            if (_source.has_study_relationships is true) DeleteData(_source_id, "study_relationships");
            if (_source.has_study_links is true) DeleteData(_source_id, "study_links");
            if (_source.has_study_ipd_available is true) DeleteData(_source_id, "study_ipd_available");
            if (_source.has_study_countries is true) DeleteData(_source_id, "study_ipd_available");
            if (_source.has_study_locations is true) DeleteData(_source_id, "study_ipd_available");
            if (_source.has_study_conditions is true) DeleteData(_source_id, "study_ipd_available");
            if (_source.has_study_iec is true) DeleteData(_source_id, "study_ipd_available");
        }
    }


    public void DeleteExistingObjectData()
    {
        int object_num = DeleteData(_source_id, "data_objects");
        if (object_num > 0)
        {
            DeleteData(_source_id, "object_instances");
            DeleteData(_source_id, "object_titles");
            DeleteData(_source_id, "object_hashes");

            // these are database dependent		

            if (_source.has_object_datasets is true) DeleteData(_source_id, "object_datasets");
            if (_source.has_object_dates is true) DeleteData(_source_id, "object_dates");
            if (_source.has_object_relationships is true) DeleteData(_source_id, "object_relationships");
            if (_source.has_object_rights is true) DeleteData(_source_id, "object_rights");
            if (_source.has_object_pubmed_set is true)
            {
                DeleteData(_source_id, "object_contributors");
                DeleteData(_source_id, "object_topics");
                DeleteData(_source_id, "object_comments");
                DeleteData(_source_id, "object_descriptions");
                DeleteData(_source_id, "object_identifiers");
                DeleteData(_source_id, "object_db_links");
                DeleteData(_source_id, "object_publication_types");
            }
        }
    }


    public void TransferStudyData()
    {
        StudyTablesTransferrer stt = new StudyTablesTransferrer(_source_id, _db_conn);

        stt.TransferStudies();
        stt.TransferStudyIdentifiers();
        stt.TransferStudyTitles();
        stt.TransferStudyHashes();

        // these are database dependent

        if (_source.has_study_topics is true) stt.TransferStudyTopics();
        if (_source.has_study_features is true) stt.TransferStudyFeatures();
        if (_source.has_study_contributors is true) stt.TransferStudyContributors();
        if (_source.has_study_references is true) stt.TransferStudyReferences();
        if (_source.has_study_relationships is true) stt.TransferStudyRelationships();
        if (_source.has_study_links is true) stt.TransferStudyLinks();
        if (_source.has_study_ipd_available is true) stt.TransferStudyIPDAvaiable();

    }


    public void TransferObjectData()
    {
        ObjectTablesTransferrer ott = new ObjectTablesTransferrer(_source_id, _db_conn);

        ott.TransferDataObjects();
        ott.TransferObjectInstances();
        ott.TransferObjectTitles();
        ott.TransferObjectHashes();
       
        // these are database dependent		

        if (_source.has_object_datasets is true) ott.TransferObjectDatasets();
        if (_source.has_object_dates is true) ott.TransferObjectDates();
        if (_source.has_object_relationships is true) ott.TransferObjectRelationships();
        if (_source.has_object_rights is true) ott.TransferObjectRights();

        if (_source?.has_object_pubmed_set is true)
        {
            ott.TransferObjectContributors();
            ott.TransferObjectTopics();
            ott.TransferObjectComments();
            ott.TransferObjectDescriptions();
            ott.TransferObjectidentifiers();
            ott.TransferObjectDBLinks();
            ott.TransferObjectPublicationTypes();
        }
    }

    private int DeleteData(int source_id, string table_name)
    {
        string sql_string = $@"Delete from sdcomp.{table_name} 
                               where source_id = {source_id}";
        using var conn = new NpgsqlConnection(_db_conn);
        return conn.Execute(sql_string);
    }

}

