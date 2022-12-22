
namespace DataHarvester
{
    public class SchemaBuilder
    {
        private ISource _source;
        private LoggingHelper _logger;
        private StudyTableBuilder study_tablebuilder;
        private ObjectTableBuilder object_tablebuilder;

        public SchemaBuilder(ISource source, LoggingHelper logger)
        {
            _source = source;
            _logger = logger;
            study_tablebuilder = new StudyTableBuilder(source.db_conn);
            object_tablebuilder = new ObjectTableBuilder(source.db_conn);
        }


        public void RecreateTables()
        {
            if (_source.has_study_tables)
            {
                // these common to all databases

                study_tablebuilder.create_table_studies();
                study_tablebuilder.create_table_study_identifiers();
                study_tablebuilder.create_table_study_titles();

                // these are database dependent
                if (_source.has_study_topics) study_tablebuilder.create_table_study_topics();
                if (_source.has_study_features) study_tablebuilder.create_table_study_features();
                if (_source.has_study_contributors) study_tablebuilder.create_table_study_contributors();
                if (_source.has_study_references) study_tablebuilder.create_table_study_references();
                if (_source.has_study_relationships) study_tablebuilder.create_table_study_relationships();
                if (_source.has_study_links) study_tablebuilder.create_table_study_links();
                if (_source.has_study_countries) study_tablebuilder.create_table_study_countries();
                if (_source.has_study_locations) study_tablebuilder.create_table_study_locations();
                if (_source.has_study_ipd_available) study_tablebuilder.create_table_ipd_available();

                _logger.LogLine("Study tables recreated");
            }

            // object tables - these common to all databases

            object_tablebuilder.create_table_data_objects();
            object_tablebuilder.create_table_object_instances();
            object_tablebuilder.create_table_object_titles();

            // these are database dependent		

            if (_source.has_object_datasets) object_tablebuilder.create_table_object_datasets();
            if (_source.has_object_dates) object_tablebuilder.create_table_object_dates();
            if (_source.has_object_relationships) object_tablebuilder.create_table_object_relationships();
            if (_source.has_object_rights) object_tablebuilder.create_table_object_rights();
            if (_source.has_object_pubmed_set)
            {
                object_tablebuilder.create_table_journal_details();
                object_tablebuilder.create_table_object_contributors();
                object_tablebuilder.create_table_object_topics();
                object_tablebuilder.create_table_object_comments();
                object_tablebuilder.create_table_object_descriptions();
                object_tablebuilder.create_table_object_identifiers();
                object_tablebuilder.create_table_object_db_links();
                object_tablebuilder.create_table_object_publication_types();
            }

            _logger.LogLine("Object tables recreated");
        }

    }
}

