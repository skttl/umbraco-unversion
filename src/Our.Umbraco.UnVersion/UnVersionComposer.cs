using System;
using System.Collections.Generic;
using System.Linq;
using Our.Umbraco.UnVersion.Services;
using Umbraco.Core;
using Umbraco.Core.Composing;

namespace Our.Umbraco.UnVersion
{
    public class UnVersionComposer : IUserComposer
    {
        public void Compose(Composition composition)
        {
            composition.Components().Append<UnVersionComponent>();
            composition.RegisterUniqueFor<IUnVersionService, UnVersionService>();
        }
    }
}