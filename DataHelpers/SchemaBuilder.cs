namespace MDR_Harvester;

public class SchemaBuilder
{
    private readonly ILoggingHelper _loggingHelper;
    private readonly StudyTableBuilder study_table_builder;
    private readonly ObjectTableBuilder object_table_builder;
    private readonly Source _source;

    public SchemaBuilder(Source source, ILoggingHelper loggingHelper)
    {
        _source = source;
        _loggingHelper = loggingHelper;
        study_table_builder = new StudyTableBuilder(source.db_conn);
        object_table_builder = new ObjectTableBuilder(source.db_conn);
    }


    public void RecreateTables()
    {
        if (_source.has_study_tables is true)
        {
            // these common to all databases

            study_table_builder.create_table_studies("sd");
            study_table_builder.create_table_study_identifiers("sd");
            study_table_builder.create_table_study_titles("sd");

            // these are database dependent
            if (_source.has_study_topics is true) study_table_builder.create_table_study_topics("sd");
            if (_source.has_study_conditions is true) study_table_builder.create_table_study_conditions("sd");
            if (_source.has_study_features is true) study_table_builder.create_table_study_features("sd");
            if (_source.has_study_people is true) study_table_builder.create_table_study_people("sd");
            if (_source.has_study_organisations is true) study_table_builder.create_table_study_organisations("sd");
            if (_source.has_study_references is true) study_table_builder.create_table_study_references("sd");
            if (_source.has_study_relationships is true) study_table_builder.create_table_study_relationships("sd");
            if (_source.has_study_links is true) study_table_builder.create_table_study_links("sd");
            if (_source.has_study_countries is true) study_table_builder.create_table_study_countries("sd");
            if (_source.has_study_locations is true) study_table_builder.create_table_study_locations("sd");
            if (_source.has_study_ipd_available is true) study_table_builder.create_table_ipd_available("sd");
            if (_source.has_study_iec is true)
            {
                if (_source.study_iec_storage_type == "Single Table")
                {
                    study_table_builder.create_table_study_iec("sd");
                }
                if (_source.study_iec_storage_type == "By Year Groupings")
                {
                    study_table_builder.create_table_study_iec_by_year_groups("sd");
                }
                if (_source.study_iec_storage_type == "By Years")
                {
                    study_table_builder.create_table_study_iec_by_years("sd");
                }
            }
            _loggingHelper.LogLine("Study tables recreated");
        }

        // object tables - these common to all databases

        object_table_builder.create_table_data_objects("sd");
        object_table_builder.create_table_object_instances("sd");
        object_table_builder.create_table_object_titles("sd");

        // these are database dependent		

        if (_source.has_object_datasets is true) object_table_builder.create_table_object_datasets("sd");
        if (_source.has_object_dates is true) object_table_builder.create_table_object_dates("sd");
        if (_source.has_object_relationships is true) object_table_builder.create_table_object_relationships("sd");
        if (_source.has_object_rights is true) object_table_builder.create_table_object_rights("sd");
        if (_source.has_object_pubmed_set is true)
        {
            object_table_builder.create_table_journal_details("sd");
            object_table_builder.create_table_object_people("sd");
            object_table_builder.create_table_object_organisations("sd");
            object_table_builder.create_table_object_topics("sd");
            object_table_builder.create_table_object_comments("sd");
            object_table_builder.create_table_object_descriptions("sd");
            object_table_builder.create_table_object_identifiers("sd");
            object_table_builder.create_table_object_db_links("sd");
            object_table_builder.create_table_object_publication_types("sd");
        }
        _loggingHelper.LogLine("Object tables recreated");
    }

}

