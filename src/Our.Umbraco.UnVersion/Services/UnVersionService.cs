using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Umbraco.Core.IO;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Web;

namespace Our.Umbraco.UnVersion.Services
{
    public class UnVersionService : IUnVersionService
    {
        private IUnVersionConfig _unVersionConfig;
        private IUmbracoContextFactory _contextFactory;
        private IContentService _contentService;
        private ILogger _logger;

        
        public UnVersionService(IUmbracoContextFactory contextFactory, IContentService contentService, ILogger logger)
        {
            _contextFactory = contextFactory;
            _contentService = contentService;
            _logger = logger;
        }

        public void UnVersion(IContent content)
        {

            var configEntries = new List<UnVersionConfigEntry>();
            var config = GetUnVersionConfig();

            if (config.ConfigEntries.ContainsKey(content.ContentType.Alias))
                configEntries.AddRange(config.ConfigEntries[content.ContentType.Alias]);

            if (config.ConfigEntries.ContainsKey(UnVersionConfig.AllDocumentTypesKey))
                configEntries.AddRange(config.ConfigEntries[UnVersionConfig.AllDocumentTypesKey]);

            if (configEntries.Count <= 0)
            {
                _logger.Debug<UnVersionService>("No UnVersion configuration found for type " + content.ContentType.Alias);
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

                foreach (var vid in versionIdsToDelete)
                {
                    _contentService.DeleteVersion(content.Id, vid, false);
                }

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
                    var dateRemoveBefore = currentDateTime.AddDays(0 - configEntry.MaxDays);
                    if (version.UpdateDate < dateRemoveBefore)
                    {
                        versionIdsToDelete.Add(version.VersionId);
                    }
                }

            }

            return versionIdsToDelete;

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

        private IUnVersionConfig GetUnVersionConfig()
        {
            if (_unVersionConfig == null)
            {
                using (var ctx = _contextFactory.EnsureUmbracoContext())
                {
                    var configFilePath = ctx.UmbracoContext.HttpContext.Server.MapPath(Path.Combine(SystemDirectories.Config, "\\unVersion.config"));
                    _unVersionConfig = new UnVersionConfig(configFilePath);
                }
            }

            return _unVersionConfig;
        }
    }
}
