using System;
using Our.Umbraco.UnVersion.Services;

namespace Our.Umbraco.UnVersion
{
    public class UnVersionContext
    {
        public IUnVersionService UnVersionService { get; internal set; }

        #region Singleton

        private static readonly Lazy<UnVersionContext> _instance = new Lazy<UnVersionContext>(() => new UnVersionContext());

        public static UnVersionContext Instance
        {
            get { return _instance.Value; }
        }

        private UnVersionContext() { }

        #endregion
    }
}