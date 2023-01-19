using Dapper.Contrib.Extensions;
using Microsoft.Extensions.Configuration;
using Npgsql;
using PostgreSQLCopyHelper;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MDR_Harvester
{
    public class StorageDataLayer : IStorageDataLayer
    {
        private string? db_conn;

        public void StoreFullStudy(Study s, ISource source)
        {
            db_conn = source.db_conn;
            StudyCopyHelpers sch = new StudyCopyHelpers();
            ObjectCopyHelpers och = new ObjectCopyHelpers();

            // store study
            StudyInDB st_db = new StudyInDB(s);
            using (var conn = new NpgsqlConnection(db_conn))
            {
                conn.Insert<StudyInDB>(st_db);
            }

            // store study attributes
            // these common to all databases

            if (s.identifiers?.Count > 0)
            {
                using (var conn = new NpgsqlConnection(db_conn))
                {
                    conn.Open();
                    sch.study_ids_helper.SaveAll(conn, s.identifiers);
                }
            }

            if (s.titles?.Count > 0)
            {
                using (var conn = new NpgsqlConnection(db_conn))
                {
                    conn.Open();
                    sch.study_titles_helper.SaveAll(conn, s.titles);
                }
            }

            // these are database dependent

            if (source.has_study_topics == true && s.topics?.Count > 0)
            {
                using (var conn = new NpgsqlConnection(db_conn))
                {
                    conn.Open();
                    sch.study_topics_helper.SaveAll(conn, s.topics);
                }
            }

            if (source.has_study_features == true && s.features?.Count > 0)
            {
                using (var conn = new NpgsqlConnection(db_conn))
                {
                    conn.Open();
                    sch.study_features_helper.SaveAll(conn, s.features);
                }
            }

            if (source.has_study_conditions == true && s.conditions?.Count > 0)
            {
                using (var conn = new NpgsqlConnection(db_conn))
                {
                    conn.Open();
                    sch.study_conditions_helper.SaveAll(conn, s.conditions);
                }
            }

            if (source.has_study_iec == true && s.iec?.Count > 0)
            {
                using (var conn = new NpgsqlConnection(db_conn))
                {
                    conn.Open();
                    sch.study_iec_helper.SaveAll(conn, s.iec);
                }
            }

            if (source.has_study_contributors == true && s.contributors?.Count > 0)
            {
                using (var conn = new NpgsqlConnection(db_conn))
                {
                    conn.Open();
                    sch.study_contributors_helper.SaveAll(conn, s.contributors);
                }
            }

            if (source.has_study_references == true && s.references?.Count > 0)
            {
                using (var conn = new NpgsqlConnection(db_conn))
                {
                    conn.Open();
                    sch.study_references_helper.SaveAll(conn, s.references);
                }
            }

            if (source.has_study_relationships == true && s.relationships?.Count > 0)
            {
                using (var conn = new NpgsqlConnection(db_conn))
                {
                    conn.Open();
                    sch.study_relationships_helper.SaveAll(conn, s.relationships);
                }
            }

            if (source.has_study_countries == true && s.countries?.Count > 0)
            {
                using (var conn = new NpgsqlConnection(db_conn))
                {
                    conn.Open();
                    sch.study_countries_helper.SaveAll(conn, s.countries);
                }
            }

            if (source.has_study_locations == true && s.sites?.Count > 0)
            {
                using (var conn = new NpgsqlConnection(db_conn))
                {
                    conn.Open();
                    sch.study_locations_helper.SaveAll(conn, s.sites);
                }
            }


            if (source.has_study_links == true && s.studylinks?.Count > 0)
            {
                using (var conn = new NpgsqlConnection(db_conn))
                {
                    conn.Open();
                    sch.study_links_helper.SaveAll(conn, s.studylinks);
                }
            }

            if (source.has_study_ipd_available == true && s.ipd_info?.Count > 0)
            {
                using (var conn = new NpgsqlConnection(db_conn))
                {
                    conn.Open();
                    sch.study_avail_ipd_helper.SaveAll(conn, s.ipd_info);
                }
            }


            // store linked data objects 

            if (s.data_objects?.Count > 0)
            {
                using (var conn = new NpgsqlConnection(db_conn))
                {
                    conn.Open();
                    och.data_objects_helper.SaveAll(conn, s.data_objects);
                }
            }
            
            // store data object attributes
            // these common to all databases

            if (s.object_instances?.Count > 0)
            {
                using (var conn = new NpgsqlConnection(db_conn))
                {
                    conn.Open();
                    och.object_instances_helper.SaveAll(conn, s.object_instances);
                }
            }

            if (s.object_titles?.Count > 0)
            {
                using (var conn = new NpgsqlConnection(db_conn))
                {
                    conn.Open();
                    och.object_titles_helper.SaveAll(conn, s.object_titles);
                }
            }
            
            // these are database dependent		
            
            if (source.has_object_datasets == true && s.object_datasets?.Count > 0)
            {
                using (var conn = new NpgsqlConnection(db_conn))
                {
                    conn.Open();
                    och.object_datasets_helper.SaveAll(conn, s.object_datasets);
                }
            }

            if (source.has_object_dates == true && s.object_dates?.Count > 0)
            {
                using (var conn = new NpgsqlConnection(db_conn))
                {
                    conn.Open();
                    och.object_dates_helper.SaveAll(conn, s.object_dates);
                }
            }
        }


        public void StoreFullObject(FullDataObject r, ISource source)
        {
            db_conn = source.db_conn;
            ObjectCopyHelpers och = new ObjectCopyHelpers();

            DataObject d = new DataObject(r);
            using (var conn = new NpgsqlConnection(db_conn))
            {
                conn.Insert<DataObject>(d);
            }

            if (r.object_instances?.Count > 0)
            {
                using (var conn = new NpgsqlConnection(db_conn))
                {
                    conn.Open();
                    och.object_instances_helper.SaveAll(conn, r.object_instances);
                }
            }

            if (r.object_titles?.Count > 0)
            {
                using (var conn = new NpgsqlConnection(db_conn))
                {
                    conn.Open();
                    och.object_titles_helper.SaveAll(conn, r.object_titles);
                }
            }

            // these are database dependent		

            if (source.has_object_dates == true && r.object_dates?.Count > 0)
            {
                using (var conn = new NpgsqlConnection(db_conn))
                {
                    conn.Open();
                    och.object_dates_helper.SaveAll(conn, r.object_dates);
                }
            }

            if (source.has_object_relationships == true && r.object_relationships?.Count > 0)
            {
                using (var conn = new NpgsqlConnection(db_conn))
                {
                    conn.Open();
                    och.object_relationships_helper.SaveAll(conn, r.object_relationships);
                }
            }

            if (source.has_object_rights == true && r.object_rights?.Any() == true)
            {
                using (var conn = new NpgsqlConnection(db_conn))
                {
                    conn.Open();
                    och.object_rights_helper.SaveAll(conn, r.object_rights);
                }
            }

            if (source.has_object_pubmed_set == true)
            {
                if (r.object_contributors?.Any() == true)
                {
                    using (var conn = new NpgsqlConnection(db_conn))
                    {
                        conn.Open();
                        och.object_contributors_helper.SaveAll(conn, r.object_contributors);
                    }
                }

                if (r.object_topics?.Any() == true)
                {
                    using (var conn = new NpgsqlConnection(db_conn))
                    {
                        conn.Open();
                        och.object_topics_helper.SaveAll(conn, r.object_topics);
                    }
                }

                if (r.object_comments?.Any() == true)
                {
                    using (var conn = new NpgsqlConnection(db_conn))
                    {
                        conn.Open();
                        och.object_comments_helper.SaveAll(conn, r.object_comments);
                    }
                }

                if (r.object_descriptions?.Any() == true)
                {
                    using (var conn = new NpgsqlConnection(db_conn))
                    {
                        conn.Open();
                        och.object_descriptions_helper.SaveAll(conn, r.object_descriptions);
                    }
                }

                if (r.object_identifiers?.Any() == true)
                {
                    using (var conn = new NpgsqlConnection(db_conn))
                    {
                        conn.Open();
                        och.object_identifiers_helper.SaveAll(conn, r.object_identifiers);
                    }
                }

                if (r.object_db_ids?.Any() == true)
                {
                    using (var conn = new NpgsqlConnection(db_conn))
                    {
                        conn.Open();
                        och.object_db_links_helper.SaveAll(conn, r.object_db_ids);
                    }
                }

                if (r.object_pubtypes?.Any() == true)
                {
                    using (var conn = new NpgsqlConnection(db_conn))
                    {
                        conn.Open();
                        och.publication_types_helper.SaveAll(conn, r.object_pubtypes);
                    }
                }


                if (r.journal_details != null)
                {
                    using (var conn = new NpgsqlConnection(db_conn))
                    {
                        conn.Insert<JournalDetails>(r.journal_details);
                    }
                }

            }
        }
    }
}


