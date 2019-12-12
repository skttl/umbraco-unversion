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
        private readonly IUnVersionService _unVersionService;

        public UnVersionComponent(IUnVersionService unVersionService)
        {
            _unVersionService = unVersionService;
        }

        public void Initialize()
        {
            ContentService.Published += ContentServicePublished;
        }

        private void ContentServicePublished(IContentService sender, ContentPublishedEventArgs e)
        {
            if (HttpContext.Current == null)
                return;

            foreach (var entity in e.PublishedEntities)
            {
                _unVersionService.UnVersion(entity);
            }
        }

        public void Terminate()
        {
            
        }
    }
}