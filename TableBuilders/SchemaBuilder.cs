
namespace MDR_Harvester;

public class SchemaBuilder
{
    private ISource _source;
    private ILoggingHelper _loggingHelper;
    private StudyTableBuilder study_tablebuilder;
    private ObjectTableBuilder object_tablebuilder;

    public SchemaBuilder(ISource source, ILoggingHelper loggingHelper)
    {
        _source = source;
        _loggingHelper = loggingHelper;
        study_tablebuilder = new StudyTableBuilder(source.db_conn);
        object_tablebuilder = new ObjectTableBuilder(source.db_conn);
    }


    public void RecreateTables()
    {
        if (_source.has_study_tables == true)
        {
            // these common to all databases

            study_tablebuilder.create_table_studies();
            study_tablebuilder.create_table_study_identifiers();
            study_tablebuilder.create_table_study_titles();

            // these are database dependent
            if (_source.has_study_topics == true) study_tablebuilder.create_table_study_topics();
            if (_source.has_study_features == true) study_tablebuilder.create_table_study_features();
            if (_source.has_study_contributors == true) study_tablebuilder.create_table_study_contributors();
            if (_source.has_study_references == true) study_tablebuilder.create_table_study_references();
            if (_source.has_study_relationships == true) study_tablebuilder.create_table_study_relationships();
            if (_source.has_study_links == true) study_tablebuilder.create_table_study_links();
            if (_source.has_study_countries == true) study_tablebuilder.create_table_study_countries();
            if (_source.has_study_locations == true) study_tablebuilder.create_table_study_locations();
            if (_source.has_study_conditions == true) study_tablebuilder.create_table_study_conditions();
            if (_source.has_study_iec == true) study_tablebuilder.create_table_study_iec();
            if (_source.has_study_ipd_available == true) study_tablebuilder.create_table_ipd_available();

            _loggingHelper.LogLine("Study tables recreated");
        }

        // object tables - these common to all databases

        object_tablebuilder.create_table_data_objects();
        object_tablebuilder.create_table_object_instances();
        object_tablebuilder.create_table_object_titles();

        // these are database dependent		

        if (_source.has_object_datasets == true) object_tablebuilder.create_table_object_datasets();
        if (_source.has_object_dates == true) object_tablebuilder.create_table_object_dates();
        if (_source.has_object_relationships == true) object_tablebuilder.create_table_object_relationships();
        if (_source.has_object_rights == true) object_tablebuilder.create_table_object_rights();
        if (_source.has_object_pubmed_set == true)
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

        _loggingHelper.LogLine("Object tables recreated");
    }

}

