using System.IO;
using System.Web;
using Our.Umbraco.UnVersion.Services;
using Umbraco.Core.Composing;
using Umbraco.Core.Events;
using Umbraco.Core.IO;
using Umbraco.Core.Services;
using Umbraco.Core.Services.Implement;
using Umbraco.Web;

namespace Our.Umbraco.UnVersion
{
    public class UnVersionComponent : IComponent
    {
        private readonly IUmbracoContextFactory _contextFactory;

        public UnVersionComponent(IUmbracoContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public void Initialize()
        {
            using (var ctx = _contextFactory.EnsureUmbracoContext())
            {
                
                var configFilePath = ctx.UmbracoContext.HttpContext.Server.MapPath(Path.Combine(SystemDirectories.Config, "\\unVersion.config"));
                var config = new UnVersionConfig(configFilePath);

                // Init context
                UnVersionContext.Instance.UnVersionService = new UnVersionService(config, true);

                ContentService.Published += ContentServicePublished;
            }
            
        }

        private void ContentServicePublished(IContentService sender, ContentPublishedEventArgs e)
        {
            if (HttpContext.Current == null)
                return;

            foreach (var entity in e.PublishedEntities)
            {
                UnVersionContext.Instance.UnVersionService.UnVersion(entity);
            }
        }

        public void Terminate()
        {
            
        }
    }
}