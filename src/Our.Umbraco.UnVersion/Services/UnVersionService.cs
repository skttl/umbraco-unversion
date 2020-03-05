using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.SqlServerCe;
using System.Linq;
using System.Net.Mime;
using System.Reflection;
using log4net;
using umbraco;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Persistence;

namespace Our.Umbraco.UnVersion.Services
{
    public class UnVersionService : IUnVersionService
    {
        private readonly static ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IUnVersionConfig _config;
        private readonly bool _catchSqlExceptions;
        private readonly string _ceStatementEnd = $"{Environment.NewLine}GO{Environment.NewLine}";

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

            if (!configEntries.Any())
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

                var versionsToKeep = VersionsToKeep(content.Id, configEntry).Select(x => $"'{x.ToString("D")}'");
                var versionsToKeepString = string.Join(",", versionsToKeep);

                if (Logger.IsDebugEnabled)
                    Logger.Debug("Keeping versions " + versionsToKeepString);

                var commands = new List<string>() {
                    @"DELETE FROM cmsPreviewXml WHERE nodeId = {0} AND versionId NOT IN ({1});",
                    @"DELETE FROM cmsPropertyData WHERE contentNodeId = {0} AND versionId NOT IN ({1});",
                    @"DELETE FROM cmsContentVersion WHERE contentId = {0} AND versionId NOT IN ({1});",
                    @"DELETE FROM cmsDocument WHERE nodeId = {0} AND versionId NOT IN ({1});",
                };

                if (ApplicationContext.Current.DatabaseContext.DatabaseProvider == DatabaseProviders.SqlServerCE) {
                    foreach (var command in commands) {
                        var formattedCmd = string.Format(command, content.Id, versionsToKeepString);
                        ApplicationContext.Current.DatabaseContext.Database.Execute(formattedCmd);
                    }
                } else {
                    // BH: Every other provider supports multiple statements per execution, this enables bulk execution.
                    var bulkCommand = string.Format(string.Join(string.Empty, commands), content.Id, versionsToKeepString);
                    ApplicationContext.Current.DatabaseContext.Database.Execute(bulkCommand);
                }
            }
        }

        private IEnumerable<Guid> VersionsToKeep(int contentId, UnVersionConfigEntry configEntry)
        {
            // Get a list of all versions
            // TODO: Need to find a better way to do this, but SQL CE 4 doesn't allow sub queries
            var sql = string.Format(@"SELECT            
                                    cv.VersionId,
                                    cv.VersionDate,
                                    d.published,
                                    d.newest
                FROM                cmsContentVersion cv
                LEFT OUTER JOIN        cmsDocument d ON d.versionId = cv.VersionId
                WHERE                cv.ContentId = {0}
                ORDER BY            cv.VersionDate DESC",
                contentId);

            if (Logger.IsDebugEnabled)
                Logger.Debug(sql);

            var versionsToKeep = new List<Guid>();

            try
            {
                var versions = ApplicationContext.Current.DatabaseContext.Database.Fetch<VersionsModel>(sql);
                var readerIndex = 0;
                foreach (var version in versions) {
                    var daysDiff = (DateTime.Now - version.VersionDate).Days;
                    if (version.Published || version.Newest || (daysDiff < configEntry.MaxDays && readerIndex <= configEntry.MaxCount))
                        versionsToKeep.Add(version.VersionID);

                    readerIndex++;
                }
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
