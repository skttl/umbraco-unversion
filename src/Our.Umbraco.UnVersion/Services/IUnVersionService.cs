using System.Collections;
using System.Collections.Generic;
using Umbraco.Core.Models;

namespace Our.Umbraco.UnVersion.Services
{
    public interface IUnVersionService
    {
        void UnVersion(IContent content);
        void UnVersion(IEnumerable<IContent> content);
    }
}