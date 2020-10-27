using Our.Umbraco.UnVersion.Services;
using Umbraco.Core.Logging;
using Umbraco.Web.Scheduling;

namespace Our.Umbraco.UnVersion
{
	public class CleanUpTrashTask : RecurringTaskBase
	{
		private readonly IUnVersionService _unVersionService;

		public CleanUpTrashTask(
            IBackgroundTaskRunner<RecurringTaskBase> runner, 
            IUnVersionService unVersionService,
            int delayMilliseconds, 
            int periodMilliseconds
        )
            : base(runner, delayMilliseconds, periodMilliseconds)
		{
			_unVersionService = unVersionService;
		}

        public override bool PerformRun()
		{
            _unVersionService.CleanUpTrash();

			return true;
		}

		public override bool IsAsync => false;
	}
}
