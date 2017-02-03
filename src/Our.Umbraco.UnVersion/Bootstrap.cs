using System.IO;
using System.Web;
using Our.Umbraco.UnVersion.Services;
using Umbraco.Core;
using Umbraco.Core.Events;
using Umbraco.Core.Models;
using Umbraco.Core.Publishing;
using Umbraco.Core.Services;

namespace Our.Umbraco.UnVersion
{
    public class Bootstrap : ApplicationEventHandler
    {
        protected override void ApplicationStarted(UmbracoApplicationBase umbracoApplication,
            ApplicationContext applicationContext)
        {
            // Init config
            var appPath = umbracoApplication.Server.MapPath("~/");
            var configFilePath = Path.Combine(appPath, @"config\unVersion.config");
            var config = new UnVersionConfig(configFilePath);

            // Init context
            UnVersionContext.Instance.UnVersionService = new UnVersionService(config, true);

            // Hookup event listener
            ContentService.Published += ContentServicePublished;
        }

        void ContentServicePublished(IPublishingStrategy sender, PublishEventArgs<IContent> e)
        {
            if (HttpContext.Current == null)
                return;

            foreach (var entity in e.PublishedEntities)
            {
                UnVersionContext.Instance.UnVersionService.UnVersion(entity);
            }
        }
    }
}