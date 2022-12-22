using Dapper;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataHarvester
{

    class TransferSDDataBuilder
    {
        private int _source_id;
        private string _db_conn;
        private ISource _source;

        public TransferSDDataBuilder(ISource source)
        {
            _source = source;
            _source_id = source.id;
            _db_conn = source.db_conn;
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

                if (_source.has_study_topics) DeleteData(_source_id, "study_topics");
                if (_source.has_study_features) DeleteData(_source_id, "study_features");
                if (_source.has_study_contributors) DeleteData(_source_id, "study_contributors");
                if (_source.has_study_references) DeleteData(_source_id, "study_references");
                if (_source.has_study_relationships) DeleteData(_source_id, "study_relationships");
                if (_source.has_study_links) DeleteData(_source_id, "study_links");
                if (_source.has_study_ipd_available) DeleteData(_source_id, "study_ipd_available");
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

                if (_source.has_object_datasets) DeleteData(_source_id, "object_datasets");
                if (_source.has_object_dates) DeleteData(_source_id, "object_dates");
                if (_source.has_object_relationships) DeleteData(_source_id, "object_relationships");
                if (_source.has_object_rights) DeleteData(_source_id, "object_rights");
                if (_source.has_object_pubmed_set)
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

            if (_source.has_study_topics) stt.TransferStudyTopics();
            if (_source.has_study_features) stt.TransferStudyFeatures();
            if (_source.has_study_contributors) stt.TransferStudyContributors();
            if (_source.has_study_references) stt.TransferStudyReferences();
            if (_source.has_study_relationships) stt.TransferStudyRelationships();
            if (_source.has_study_links) stt.TransferStudyLinks();
            if (_source.has_study_ipd_available) stt.TransferStudyIPDAvaiable();

        }


        public void TransferObjectData()
        {
            ObjectTablesTransferrer ott = new ObjectTablesTransferrer(_source_id, _db_conn);

            ott.TransferDataObjects();
            ott.TransferObjectInstances();
            ott.TransferObjectTitles();
            ott.TransferObjectHashes();
           
            // these are database dependent		

            if (_source.has_object_datasets) ott.TransferObjectDatasets();
            if (_source.has_object_dates) ott.TransferObjectDates();
            if (_source.has_object_relationships) ott.TransferObjectRelationships();
            if (_source.has_object_rights) ott.TransferObjectRights();

            if (_source.has_object_pubmed_set)
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
            int res = 0;
            string sql_string = @"Delete from sdcomp." + table_name + @" 
            where source_id = " + source_id.ToString();
            
            using (var conn = new NpgsqlConnection(_db_conn))
            {
                return res = conn.Execute(sql_string);
            }
        }

    }
}
