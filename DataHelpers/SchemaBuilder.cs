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

            study_table_builder.create_table_studies();
            study_table_builder.create_table_study_identifiers();
            study_table_builder.create_table_study_titles();

            // these are database dependent
            if (_source.has_study_topics is true) study_table_builder.create_table_study_topics();
            if (_source.has_study_features is true) study_table_builder.create_table_study_features();
            if (_source.has_study_contributors is true) study_table_builder.create_table_study_contributors();
            if (_source.has_study_references is true) study_table_builder.create_table_study_references();
            if (_source.has_study_relationships is true) study_table_builder.create_table_study_relationships();
            if (_source.has_study_links is true) study_table_builder.create_table_study_links();
            if (_source.has_study_countries is true) study_table_builder.create_table_study_countries();
            if (_source.has_study_locations is true) study_table_builder.create_table_study_locations();
            if (_source.has_study_conditions is true) study_table_builder.create_table_study_conditions();
            if (_source.has_study_iec is true) study_table_builder.create_table_study_iec();
            if (_source.has_study_ipd_available is true) study_table_builder.create_table_ipd_available();

            _loggingHelper.LogLine("Study tables recreated");
        }

        // object tables - these common to all databases

        object_table_builder.create_table_data_objects();
        object_table_builder.create_table_object_instances();
        object_table_builder.create_table_object_titles();

        // these are database dependent		

        if (_source.has_object_datasets is true) object_table_builder.create_table_object_datasets();
        if (_source.has_object_dates is true) object_table_builder.create_table_object_dates();
        if (_source.has_object_relationships is true) object_table_builder.create_table_object_relationships();
        if (_source.has_object_rights is true) object_table_builder.create_table_object_rights();
        if (_source.has_object_pubmed_set is true)
        {
            object_table_builder.create_table_journal_details();
            object_table_builder.create_table_object_contributors();
            object_table_builder.create_table_object_topics();
            object_table_builder.create_table_object_comments();
            object_table_builder.create_table_object_descriptions();
            object_table_builder.create_table_object_identifiers();
            object_table_builder.create_table_object_db_links();
            object_table_builder.create_table_object_publication_types();
        }

        _loggingHelper.LogLine("Object tables recreated");
    }

}

