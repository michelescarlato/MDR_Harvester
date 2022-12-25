using Dapper;
using Npgsql;
using System.Collections.Generic;

namespace MDR_Harvester
{
    class ExpectedDataBuilder
    {
        string _db_conn;

        public ExpectedDataBuilder(string db_conn)
        {
            _db_conn = db_conn;
        }


        public void Execute_SQL(string sql_string)
        {
            using (var conn = new NpgsqlConnection(_db_conn))
            {
                conn.Execute(sql_string);
            }
        }


        private void LoadStudyData(string study_id)
        {
            string sp_call = "call expected.study_" + study_id + "();";
            using (var conn = new NpgsqlConnection(_db_conn))
            {
                conn.Execute(sp_call);
            }
        }


        private void LoadObjectData(string object_id)
        {
            // Used for Pubmed data

            string sp_call = "call expected.object_" + object_id + "();";
            using (var conn = new NpgsqlConnection(_db_conn))
            {
                conn.Execute(sp_call);
            }
        }

        public void InitialiseTestStudiesList()
        {
            string sql_string = @"DROP TABLE IF EXISTS expected.source_studies;
            create table expected.source_studies as
            select * from mon_sf.source_data_studies
            where for_testing = true;";

            Execute_SQL(sql_string);

            // Initialise expected studies table with registry ids from source studies table

            sql_string = @"insert into expected.studies(sd_sid)
            select sd_id from 
            expected.source_studies 
            order by source_id, sd_id;";

            Execute_SQL(sql_string);
        }

         
        public void InitialiseTestPubMedObjectsList()
        {
            string sql_string = @"DROP TABLE IF EXISTS expected.source_objects;
            create table expected.source_objects as
            select * from mon_sf.source_data_objects
            where for_testing = true;";

            Execute_SQL(sql_string);

            // Initialise expected objects table with pmid ids from source studies table
            // study related objects to be added later)

            sql_string = @"insert into expected.data_objects(sd_oid)
            select sd_id from 
            expected.source_objects 
            order by source_id, sd_id;";

            Execute_SQL(sql_string);
        }

        public void LoadInitialInputTables()
        {
            // clinicaltrials.gov studies

            LoadStudyData("nct00002516");
            LoadStudyData("nct00023244");
            LoadStudyData("nct00051350");

            LoadStudyData("nct00094302");
            LoadStudyData("nct00200967");
            LoadStudyData("nct00433329");

            LoadStudyData("nct01727258");
            LoadStudyData("nct01973660");
            LoadStudyData("nct02243202");

            LoadStudyData("nct02318992");
            LoadStudyData("nct02441309");
            LoadStudyData("nct02449174");

            LoadStudyData("nct02562716");
            LoadStudyData("nct02609386");
            LoadStudyData("nct02798978");
            LoadStudyData("nct02922075");
            LoadStudyData("nct03050593");
            LoadStudyData("nct03076619");

            LoadStudyData("nct03167125");
            LoadStudyData("nct03226236");
            LoadStudyData("nct03631199");

            LoadStudyData("nct03786900");
            LoadStudyData("nct04406714");
            LoadStudyData("nct04419571");

            // biolincc studies

            LoadStudyData("acrn_bags");
            LoadStudyData("acrn_large");
            LoadStudyData("baby_hug");
            LoadStudyData("omni_heart");
            LoadStudyData("topcat");

            // yoda studies

            LoadStudyData("y_nct02243202");
            LoadStudyData("y_30_49");
            LoadStudyData("y_gal_mvd_301");
            LoadStudyData("y_nct01727258");
            LoadStudyData("y_nct00433329");

            // euctr studies

            LoadStudyData("2004_001569_16");
            LoadStudyData("2009_011622_34");
            LoadStudyData("2012_000615_84");
            LoadStudyData("2013_001036_22");
            LoadStudyData("2015_000556_14");
            LoadStudyData("2018_001547_32");

            // isctrn studies

            LoadStudyData("isrctn00075564");
            LoadStudyData("isrctn16535250");
            LoadStudyData("isrctn59589587");
            LoadStudyData("isrctn82138287");
            LoadStudyData("isrctn88368130");

            // WHO studies

            LoadStudyData("actrn12616000771459");
            LoadStudyData("actrn12620001103954");
            LoadStudyData("chictr_ooc_16010171");
            LoadStudyData("chictr_poc_17010431");
            LoadStudyData("ctri_2017_03_008228");
            LoadStudyData("ctri_2019_06_019509");
            LoadStudyData("drks00011324");
            LoadStudyData("jprn_jrcts012180017");
            LoadStudyData("jprn_umin000024722");
            LoadStudyData("jprn_umin000028075");
            LoadStudyData("lbctr2019070214");
            LoadStudyData("nl8683");
            LoadStudyData("ntr1437");
            LoadStudyData("per_015_19");
            LoadStudyData("tctr20161221005");

            // pubmed objects
            LoadObjectData("16287956");
            LoadObjectData("27056882");
            LoadObjectData("32739049");
            LoadObjectData("32739569");
            LoadObjectData("32740235");

        }

    }
}
