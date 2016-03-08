using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dapper;
using System.Data.SqlClient;
using System.Configuration;
using Models;

namespace DAL
{
    public class Committee
    {
        public Committee()
        {

        }

        private static List<Committee> _committeeCache = null;

        public static List<Committee> AllCommittees()
        {
            // If the committee cache is null, pull it from db and cache it
            if (_committeeCache == null)
            {
                using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["gnyhasql01"].ConnectionString))
                {
                    //TEST Code
//                    var results = conn.Query<Committee>(@"SELECT DISTINCT PluginId ListId, Name 
//                                                          FROM sysdba.v_mailing_labels vml
//                                                          LEFT JOIN [Workspace].[dbo].[ETAutomation_Committee_ETList_Map] map 
//                                                          ON VML.PluginId = map.CommitteeID
//                                                          WHERE map.SyncAction = 'include' or map.SyncAction IS NULL").ToList();
                    //PROD Code
                    var results = conn.Query<Committee>(@"SELECT DISTINCT PluginId ListId, Name 
                                                          FROM sysdba.v_mailing_labels vml
                                                          LEFT JOIN [DynamicsExtension].[dbo].[ETAutomation_Committee_ETList_Map] map 
                                                          ON VML.PluginId = map.CommitteeID
                                                          WHERE map.SyncAction = 'include' or map.SyncAction IS NULL").ToList();
                    _committeeCache = results;
                }
            }

            return _committeeCache;
        }

        public IEnumerable<CommitteeContact> GetContacts()
        {
            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["gnyhasql01"].ConnectionString))
            {
                return conn.Query<CommitteeContact>(@"SELECT ML.*, CAST(CASE WHEN pc.COUNTY = 'NYC' THEN 1 ELSE 0 END AS BIT) AS NYC 
                                                      FROM sysdba.v_mailing_labels ml LEFT JOIN sysdba.POSTALCODE pc ON ml.PostalCode = pc.POSTALCODE
                                                      WHERE ml.PluginId = @PluginId AND ml.Email IS NOT NULL AND LEN(ml.Email)>0", new { PluginId = ListId });
            }
        }

        public int UpdateCommittetoETListMap(string committeeName, Guid committeeID, string etListName, int etListID)
        {
            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DynamicsExtension"].ConnectionString))
            {
                return conn.Execute(@"UPDATE [dbo].[ETAutomation_Committee_ETList_Map]
                                    SET [CommitteeName] = @CommitteeName
                                    ,[ETListID] = @ID
                                    ,[ETListName] = @ListName
                                    WHERE [CommitteeID] = @CommitteeID", new { CommitteeID = committeeID, CommitteeName = committeeName, @ID = etListID, @ListName = etListName });
            }
        }

        public int AddNewCommitteeToMap(string committeeName, Guid committeeID)
        {
            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DynamicsExtension"].ConnectionString))
            {
                return
                    conn.Execute(
                        @"INSERT INTO [dbo].[ETAutomation_Committee_ETList_Map]
           ([CommitteeID]
           ,[CommitteeName])           
     VALUES
           (@CommitteeID
           ,@CommitteeName)",
                        new { CommitteeID = committeeID, CommitteeName = committeeName });
            }
        }


        public int GetETListIdByCommitteeID()
        {
            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DynamicsExtension"].ConnectionString))
            {
                return conn.Query<int>(@"SELECT ETListID from [dbo].[ETAutomation_Committee_ETList_Map]
                                      WHERE [CommitteeID] = @CommitteeID", new { CommitteeID = ListId.ToString() }).SingleOrDefault();
            }
        }

        public Guid ListId { get; set; }
        public string Name { get; set; }

    }
}
