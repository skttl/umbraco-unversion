using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Core;
using Umbraco.Core.Composing;

namespace Our.Umbraco.UnVersion
{
    public class UnVersionComposer : IUserComposer
    {
        public void Compose(Composition composition)
        {
            composition.Components().Append<UnVersionComponent>();
        }
    }
}