using Our.Umbraco.UnVersion.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Services.Implement;

namespace Our.Umbraco.UnVersion
{
    public class UnVersionComponent : IComponent
    {
        private readonly IUnVersionService unVersionService;

        public UnVersionComponent(IUnVersionService _unVersionService)
        {
            unVersionService = _unVersionService;
        }

        public void Initialize()
        {
            ContentService.Published += ContentService_Published;
        }

        private void ContentService_Published(global::Umbraco.Core.Services.IContentService sender, global::Umbraco.Core.Events.ContentPublishedEventArgs e)
        {
            foreach (var content in e.PublishedEntities) {
                unVersionService.UnVersion(content);
            }
        }

        public void Terminate()
        {
        }
    }
}
