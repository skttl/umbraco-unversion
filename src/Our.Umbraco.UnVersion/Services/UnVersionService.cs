using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlServerCe;
using System.Reflection;
using log4net;
using umbraco;
using Umbraco.Core.Models;

namespace Our.Umbraco.UnVersion.Services
{
    public class UnVersionService : IUnVersionService
    {
        private readonly static ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IUnVersionConfig _config;
        private readonly bool _catchSqlExceptions;

        public UnVersionService(IUnVersionConfig config, bool catchSqlExceptions)
        {
            _config = config;
            _catchSqlExceptions = catchSqlExceptions;
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
                if (Logger.IsDebugEnabled)
                    Logger.Debug("No unversion configuration found for type " + content.ContentType.Alias);

                return;
            }

            foreach (var configEntry in configEntries)
            {
                var isValid = true;

                if (!String.IsNullOrEmpty(configEntry.RootXPath))
                {
                    if (content.Level > 1 && content.Parent() != null)
                    {
                        var ids = GetNodeIdsFromXpath(configEntry.RootXPath);
                        isValid = ids.Contains(content.ParentId);
                    }
                }

                if (!isValid)
                    continue;

                var connStr = ConfigurationManager.ConnectionStrings["umbracoDbDSN"];

                var conn = connStr.ProviderName.Contains("SqlServerCe")
                    ? (IDbConnection)new SqlCeConnection(connStr.ConnectionString)
                    : (IDbConnection)new SqlConnection(connStr.ConnectionString);
           
                conn.Open();

                var vesionsToKeep = VersionsToKeep( content.Id, configEntry, conn);
                var versionsToKeepString = string.Join( ",", vesionsToKeep );

                if (Logger.IsDebugEnabled)
                    Logger.Debug("Keeping versions " + versionsToKeepString);

                var sqlStrings = new List<string> {
                    string.Format(@"
                                DELETE
                                FROM	cmsPreviewXml
                                WHERE	nodeId = {0} AND versionId NOT IN ({1})",
                    content.Id,
                    versionsToKeepString),

                    string.Format(@"
                                DELETE
                                FROM	cmsPropertyData
                                WHERE	contentNodeId = {0} AND versionId  NOT IN ({1})",
                    content.Id,
                    versionsToKeepString),


                    string.Format(@"
                                DELETE
                                FROM	cmsContentVersion
                                WHERE	contentId = {0} AND versionId  NOT IN ({1})",
                    content.Id,
                    versionsToKeepString),

                    string.Format(@"
                                DELETE
                                FROM	cmsDocument 
                                WHERE	nodeId = {0} AND versionId  NOT IN ({1})",
                    content.Id,
                    versionsToKeepString)
                };

                foreach (var sqlString in sqlStrings)
                {
                    ExecuteSql(sqlString, conn);
                }

                conn.Close();
                conn.Dispose();
            }
        }


        void ExecuteSql(string sql, IDbConnection connection)
        {
            if (Logger.IsDebugEnabled)
                Logger.Debug(sql);

            var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = sql;

            if (_catchSqlExceptions)
            {
                try
                {
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Logger.Warn("Executing " + sql, ex);
                }
            }
            else
            {
                command.ExecuteNonQuery();
            }
        }

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

            if (Logger.IsDebugEnabled)
                Logger.Debug(sql);

            var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = sql;

            var versionsToKeep = new List<string>();
            var readerIndex = 0;

            try
            {
                var reader = command.ExecuteReader();

                while (reader.Read()) 
                { 
                    var versionId = reader.GetGuid(0);
                    var versionDate = reader.GetDateTime(1);
                    var published = reader.GetBoolean(2);
                    var newest = reader.GetBoolean(3);

                    readerIndex++;

                    var daysDiff = (DateTime.Now - versionDate).Days;
                    if (published || newest || (daysDiff < configEntry.MaxDays && readerIndex <= configEntry.MaxCount))
                        versionsToKeep.Add("'" + versionId.ToString("D") + "'");
                }

                reader.Close();
                reader.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Warn(ex);

                if (!_catchSqlExceptions)
                    throw ex;
            }

            return versionsToKeep;
        }

        private List<int> GetNodeIdsFromXpath(string xpath)
        {
            var ids = new List<int>();
            var nodes = library.GetXmlNodeByXPath(xpath);

            while (nodes.MoveNext())
                ids.Add(Convert.ToInt32(nodes.Current.GetAttribute("id", "")));

            return ids;
        }
    }
}