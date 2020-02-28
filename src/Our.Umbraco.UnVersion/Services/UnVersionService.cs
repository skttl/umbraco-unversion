using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlServerCe;
using Umbraco.Core.Models;
using Umbraco.Core.Logging;
using Umbraco.Web;
using System.Linq;
using Umbraco.Core.Services;

namespace Our.Umbraco.UnVersion.Services
{
    public class UnVersionService : IUnVersionService
    {
        private readonly ILogger _logger;
        private readonly IUmbracoContextFactory _context;
        private readonly IContentService _contentService;
        private readonly IUnVersionConfig _config;

        public UnVersionService(IUnVersionConfig config, ILogger logger, IUmbracoContextFactory context, IContentService contentService)
        {
            _logger = logger;
            _config = config;
            _context = context;
            _contentService = contentService;
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
                _logger.Debug<UnVersionService>("No unversion configuration found for type " + content.ContentType.Alias);
                return;
            }

            foreach (var configEntry in configEntries)
            {
                var isValid = true;

                // Check the RootXPath if configured
                if (!String.IsNullOrEmpty(configEntry.RootXPath))
                {
                    if (content.Level > 1 && content.ParentId > 0)
                    {
                        var ids = GetNodeIdsFromXpath(configEntry.RootXPath);
                        isValid = ids.Contains(content.ParentId);
                    }
                }

                if (!isValid)
                    continue;

                if (configEntry.MaxDays < int.MaxValue)
                {
                    _contentService.DeleteVersions(content.Id, DateTime.Now.AddDays(-configEntry.MaxDays));
                }

                if (configEntry.MaxCount < int.MaxValue)
                {
                    var versionIds = _contentService.GetVersionIds(content.Id, configEntry.MaxCount + 1);
                    if (versionIds.Count() > configEntry.MaxCount)
                    {
                        _contentService.DeleteVersion(content.Id, versionIds.Last(), true);
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
                    var path = string.Concat(SystemDirectories.Config, "/unVersion.config");
                    var configFilePath = ctx.UmbracoContext.HttpContext.Server.MapPath(path);
                    _unVersionConfig = new UnVersionConfig(configFilePath, _logger);
                }
            }

            return _unVersionConfig;
        }
    }
}
