using System.IO;
using System.Web;
using Our.Umbraco.UnVersion.Services;
using Umbraco.Core.Events;
using Umbraco.Core.Services;
using Umbraco.Core;
using Umbraco.Core.Composing;

namespace Our.Umbraco.UnVersion
{
    public class Bootstrap : IUserComposer
    {
        public void Compose(Composition composition)
        {
            composition.Register<IUnVersionConfig, UnVersionConfig>(Lifetime.Singleton);
            composition.Register<IUnVersionService, UnVersionService>(Lifetime.Singleton);
        }
    }
}