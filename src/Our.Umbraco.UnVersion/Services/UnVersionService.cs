using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Persistence;
using Umbraco.Core.Scoping;
using Umbraco.Core.Services;
using Umbraco.Web;

namespace Our.Umbraco.UnVersion.Services
{
	public class UnVersionService : IUnVersionService
    {
        private readonly ILogger _logger;
        private readonly IUmbracoContextFactory _context;
        private readonly IContentService _contentService;
		private IUnVersionConfig _config;
        private readonly IUmbracoContextFactory _umbracoContextFactory;
        private readonly IScopeProvider _scopeProvider;

        public UnVersionService(IUnVersionConfig config, ILogger logger, IUmbracoContextFactory context, IContentService contentService, IUmbracoContextFactory umbracoContextFactory, IScopeProvider scopeProvider)
        {
            _logger = logger;
            _config = config;
            _context = context;
            _contentService = contentService;
            _umbracoContextFactory = umbracoContextFactory;
            _scopeProvider = scopeProvider;
		}

        public void UnVersion(IContent content)
        {

            var configEntries = new List<UnVersionConfigEntry>();

            if (_config.VersionConfigEntries.ContainsKey(content.ContentType.Alias))
                configEntries.AddRange(_config.VersionConfigEntries[content.ContentType.Alias]);

            if (_config.VersionConfigEntries.ContainsKey(UnVersionConfig.AllDocumentTypesKey))
                configEntries.AddRange(_config.VersionConfigEntries[UnVersionConfig.AllDocumentTypesKey]);

            if (configEntries.Count <= 0)
            {
                _logger.Debug<UnVersionService>("No unversion configuration found for type {alias}", content.ContentType.Alias);
                return;
            }

            foreach (var configEntry in configEntries.Where(x => x.Type == UnVersionConfigEntryType.Version).ToList())
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
                {
                    _logger.Debug<UnVersionService>("Configuration invalid, rootXPath must be {rootXPath}", configEntry.RootXPath);
                    continue;
                }

                var allVersions = _contentService.GetVersionsSlim(content.Id, 0, int.MaxValue).ToList();

                if (!allVersions.Any())
                {
                    _logger.Debug<UnVersionService>("No versions of content {contentId} found", content.Id);
                    continue;
                }

                var versionIdsToDelete = GetVersionsToDelete(allVersions, configEntry, DateTime.Now);

                foreach (var vid in versionIdsToDelete)
                {
                    _logger.Debug<UnVersionService>("Deleting version {versionId} of content {contentId}", vid, content.Id);
                    _contentService.DeleteVersion(content.Id, vid, false);
                }

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


            _logger.Debug<UnVersionService>("Getting versions for config entry. {alias}, {maxCount}, {maxDays}, {rootXpath}", configEntry.DocTypeAlias, configEntry.MaxCount, configEntry.MaxDays, configEntry.RootXPath);

            foreach (var version in versions)
            {
                iterationCount++;
                _logger.Debug<UnVersionService>("Comparing version {versionId}, iterationCount is {iterationCount}", version.VersionId, iterationCount);

                // If we have a maxCount and the current iteration is above that max-count
                if (configEntry.MaxCount > 0 && iterationCount > configEntry.MaxCount)
                {
                    _logger.Debug<UnVersionService>("Remove version {versionId}, because iterationCount is {iterationCount} and max count is {maxCount}", version.VersionId, iterationCount, configEntry.MaxCount);
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
                        _logger.Debug<UnVersionService>("Remove version {versionId}, because version is updated {updateDate} and max days is {maxDays} (cutoff: {dateRemoveBefore})", version.VersionId, version.UpdateDate, configEntry.MaxDays, dateRemoveBefore);
                        versionIdsToDelete.Add(version.VersionId);
                    }
                }

            }

            return versionIdsToDelete;

        }

        private List<int> GetNodeIdsFromXpath(string xpath)
        {
            using (var ctx = _context.EnsureUmbracoContext())
            {
                var nodes = ctx.UmbracoContext.Content.GetByXPath(xpath);

                if (nodes == null)
                    return new List<int>();

                return nodes.Select(x => x.Id).ToList();
            }
        }

        /// <summary>
        /// Based on configEntries, deletes items from trash if too old or too many.
        /// </summary>
        public void CleanUpTrash()
        {
            _logger.Debug<CleanUpTrashTask>("CleanupTrashTask PerformRun start");

            using (var scope = _scopeProvider.CreateScope())
            {
                using (var umbracoContextReference = _umbracoContextFactory.EnsureUmbracoContext())
                {
                    foreach (var cfg in _config.TrashConfigEntries)
                    {
                        var query = scope.SqlContext.Query<IContent>();

                        if (cfg.DocTypeAlias != UnVersionConfig.AllDocumentTypesKey)
                        {
                            // The query does *not* like it when asked to match ContentType.Alias directly, so we get the ID instead
                            var contentType = umbracoContextReference.UmbracoContext.Content.GetContentType(cfg.DocTypeAlias);

                            if (contentType != null)
                            {
                                query = query.Where(x => x.ContentTypeId.Equals(contentType.Id));
                            }
                        }

                        if (cfg.MaxDays < int.MaxValue)
                        {
                            // doesn't matter if config says -10 or 10, we always need a negative number
                            var alwaysNegative = -Math.Abs(cfg.MaxDays); 

                            var minDate = DateTime.Now.AddDays(alwaysNegative);

                            query = query.Where(x => x.UpdateDate < minDate);
                        }

                        // We'll not handle more than 100 nodes per cycle - if we don't handle it now, we'll get to it eventually
                        var nodesToDelete = _contentService.GetPagedContentInRecycleBin(
                            0,
                            Math.Min(cfg.MaxCount, 100),
                            out _,
                            query,
                            Ordering.By(nameof(IContent.UpdateDate))
                        );

                        foreach (var content in nodesToDelete)
                        {
                            _logger.Debug<CleanUpTrashTask>("Deleting content {contentId} from Recycle Bin", content.Id);

                            _contentService.Delete(content);
                        }
                    }
                }
                scope.Complete();
            }
        }
	}
}
