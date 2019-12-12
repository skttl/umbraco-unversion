using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlServerCe;
using System.Linq;
using System.Reflection;
using NPoco;
//using log4net;
//using umbraco;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Web;

namespace Our.Umbraco.UnVersion.Services
{
    public class UnVersionService : IUnVersionService
    {
        //private readonly static ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IUnVersionConfig _config;
        private readonly bool _catchSqlExceptions;

        private IUmbracoContextFactory _contextFactory;
        private IContentService _contentService;

        
        public UnVersionService(IUnVersionConfig config, bool catchSqlExceptions)
        {
            _config = config;
            _catchSqlExceptions = catchSqlExceptions;
        }

        public void FooBarTesting()
        {
            IContent content;
            IContentService cs = null;
        }

        public void UnVersion(IContent content)
        {

            var configEntries = new List<UnVersionConfigEntry>();

            if (_config.ConfigEntries.ContainsKey(content.ContentType.Alias))
                configEntries.AddRange(_config.ConfigEntries[content.ContentType.Alias]);

            if (_config.ConfigEntries.ContainsKey("$_ALL"))
                configEntries.AddRange(_config.ConfigEntries["$_ALL"]);

            if (configEntries.Count <= 0)
            {
                //if (Logger.IsDebugEnabled)
                //    Logger.Debug("No unversion configuration found for type " + content.ContentType.Alias);

                return;
            }

            foreach (var configEntry in configEntries)
            {
                var isValid = true;

                // Check the RootXPath if configured
                if (!String.IsNullOrEmpty(configEntry.RootXPath))
                {
                    // TODO: Fix in some otherway
                    if (content.Level > 1 && content.ParentId > 0)
                    {
                        var ids = GetNodeIdsFromXpath(configEntry.RootXPath);
                        isValid = ids.Contains(content.ParentId);
                    }
                }

                if (!isValid)
                    continue;

                // Getting all versions (guess nobody keeps over 20 000 versions of a content node)
                var allVersions = _contentService.GetVersionsSlim(content.Id, 0, 20000).ToList();

                if(!allVersions.Any())
                    continue;

                var versionIdsToDelete = GetVersionsToDelete(allVersions, configEntry, DateTime.Now);

                //TODO: Remove more stuff

                /*
                    readerIndex++;
                    var daysDiff = (DateTime.Now - versionDate).Days;
                    if (published || newest || (daysDiff < configEntry.MaxDays && readerIndex <= configEntry.MaxCount))
                 */

                //TODO: Get all version, order by date, remove all version with count above the limit and with date older than the limit.





                //var connStr = ConfigurationManager.ConnectionStrings["umbracoDbDSN"];

                //using (var conn = connStr.ProviderName.Contains("SqlServerCe")
                //    ? (IDbConnection)new SqlCeConnection(connStr.ConnectionString)
                //    : (IDbConnection)new SqlConnection(connStr.ConnectionString))
                //{

                //    conn.Open();

                //    var vesionsToKeep = VersionsToKeep(content.Id, configEntry, conn);
                //    var versionsToKeepString = string.Join(",", vesionsToKeep);

                //    //if (Logger.IsDebugEnabled)
                //    //    Logger.Debug("Keeping versions " + versionsToKeepString);

                //    var sqlStrings = new List<string> {
                //        string.Format(@"
                //                    DELETE
                //                    FROM	cmsPreviewXml
                //                    WHERE	nodeId = {0} AND versionId NOT IN ({1})",
                //        content.Id,
                //        versionsToKeepString),

                //        string.Format(@"
                //                    DELETE
                //                    FROM	cmsPropertyData
                //                    WHERE	contentNodeId = {0} AND versionId  NOT IN ({1})",
                //        content.Id,
                //        versionsToKeepString),


                //        string.Format(@"
                //                    DELETE
                //                    FROM	cmsContentVersion
                //                    WHERE	contentId = {0} AND versionId  NOT IN ({1})",
                //        content.Id,
                //        versionsToKeepString),

                //        string.Format(@"
                //                    DELETE
                //                    FROM	cmsDocument 
                //                    WHERE	nodeId = {0} AND versionId  NOT IN ({1})",
                //        content.Id,
                //        versionsToKeepString)
                //    };

                //    foreach (var sqlString in sqlStrings)
                //    {
                //        ExecuteSql(sqlString, conn);
                //    }

                //    conn.Close();
                //}
            }
        }

        /// <summary>
        /// Iterates a list of IContent versions and returns items to be removed based on a configEntry.
        /// </summary>
        /// <param name="versions"></param>
        /// <param name="configEntry"></param>
        /// <param name="currentDateTime"></param>
        /// <returns></returns>
        public List<int> GetVersionsToDelete(List<IContent> versions, UnVersionConfigEntry configEntry, DateTime currentDateTime)
        {
            List<int> versionIdsToDelete = new List<int>();

            int iterationCount = 0;

            foreach (var version in versions)
            {
                iterationCount++;

                // If we have a maxCount and the current iteration is above that max-count
                if (configEntry.MaxCount > 0 && iterationCount > configEntry.MaxCount)
                {
                    versionIdsToDelete.Add(version.VersionId);
                    // no need to compare dates since we've already added this version for deletion
                    continue;
                }

                // If we have a max days and the current version is older
                if (configEntry.MaxDays > 0 && configEntry.MaxDays != int.MaxValue)
                {
                    // TODO: Mock out DateTime now.
                    var dateRemoveBefore = currentDateTime.AddDays(0 - configEntry.MaxDays);
                    if (version.UpdateDate < dateRemoveBefore)
                    {
                        versionIdsToDelete.Add(version.VersionId);
                    }
                }

            }

            return versionIdsToDelete;

        }

        //void ExecuteSql(string sql, IDbConnection connection)
        //{
        //    //if (Logger.IsDebugEnabled)
        //    //    Logger.Debug(sql);

        //    var command = connection.CreateCommand();
        //    command.CommandType = CommandType.Text;
        //    command.CommandText = sql;

        //    if (_catchSqlExceptions)
        //    {
        //        try
        //        {
        //            command.ExecuteNonQuery();
        //        }
        //        catch (Exception ex)
        //        {
        //            //Logger.Warn("Executing " + sql, ex);
        //        }
        //    }
        //    else
        //    {
        //        command.ExecuteNonQuery();
        //    }
        //}

        private IEnumerable<string> VersionsToKeep(int contentId, UnVersionConfigEntry configEntry, IDbConnection connection)
        {
            // Get a list of all versions
            // TODO: Need to find a better way to do this, but SQL CE 4 doesn't allow sub queries
            var sql = string.Format(@"SELECT			
                                    cv.VersionId,
	                                cv.VersionDate,
	                                d.published,
	                                d.newest
                FROM				cmsContentVersion cv
                LEFT OUTER JOIN		cmsDocument d ON d.versionId = cv.VersionId
                WHERE				cv.ContentId = {0}
                ORDER BY            cv.VersionDate DESC",
                contentId);

            //if (Logger.IsDebugEnabled)
            //    Logger.Debug(sql);

            var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = sql;

            var versionsToKeep = new List<string>();
            var readerIndex = 0;

            try
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var versionId = reader.GetGuid(0);
                        var versionDate = reader.GetDateTime(1);
                        var published = !reader.IsDBNull(2) && reader.GetBoolean(2);
                        var newest = !reader.IsDBNull(3) && reader.GetBoolean(3);

                        readerIndex++;

                        var daysDiff = (DateTime.Now - versionDate).Days;
                        if (published || newest || (daysDiff < configEntry.MaxDays && readerIndex <= configEntry.MaxCount))
                            versionsToKeep.Add("'" + versionId.ToString("D") + "'");
                    }

                    reader.Close();
                    reader.Dispose();
                }
            }
            catch (Exception ex)
            {
                //Logger.Warn(ex);

                if (!_catchSqlExceptions)
                    throw ex;
            }

            return versionsToKeep;
        }

        private List<int> GetNodeIdsFromXpath(string xpath)
        {
            using (var ctx = _contextFactory.EnsureUmbracoContext())
            {
                var nodes = ctx.UmbracoContext.ContentCache.GetByXPath(xpath);

                if(nodes == null)
                    return new List<int>();

                return nodes.Select(x => x.Id).ToList();
            }

        }
    }
}
