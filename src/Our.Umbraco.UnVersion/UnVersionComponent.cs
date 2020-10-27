using Our.Umbraco.UnVersion.Services;
using Umbraco.Core.Composing;
using Umbraco.Core.Logging;
using Umbraco.Core.Services.Implement;
using Umbraco.Web.Scheduling;

namespace Our.Umbraco.UnVersion
{
	public class UnVersionComponent : IComponent
    {
        private readonly IUnVersionService unVersionService;
        private BackgroundTaskRunner<IBackgroundTask> _trashCleanupRunner;

        public UnVersionComponent(IUnVersionService _unVersionService, ILogger logger)
        {
            unVersionService = _unVersionService;
            _trashCleanupRunner = new BackgroundTaskRunner<IBackgroundTask>("CleanUpTrash", logger);
        }

        public void Initialize()
        {
            ContentService.Published += ContentService_Published;

            int delayBeforeWeStart = 60000; // 60000ms = 1min
            int howOftenWeRepeat = 60000 * 60; // 1hr

            var task = new CleanUpTrashTask(_trashCleanupRunner, unVersionService, delayBeforeWeStart, howOftenWeRepeat);

            //As soon as we add our task to the runner it will start to run (after its delay period)
            _trashCleanupRunner.TryAdd(task);
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
