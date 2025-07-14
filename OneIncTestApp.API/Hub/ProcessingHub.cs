using OneIncTestApp.API.Services.Interfaces;

namespace OneIncTestApp.Hub
{
    public class ProcessingHub : Microsoft.AspNetCore.SignalR.Hub
    {
        private readonly ILogger<ProcessingHub> _logger;
        private readonly IJobManager _jobManager;

        public ProcessingHub(ILogger<ProcessingHub> logger, IJobManager jobManager)
        {
            _logger = logger;
            _jobManager = jobManager;
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionId = Context.ConnectionId;
            _jobManager.CancelAllJobs(connectionId);
            await base.OnDisconnectedAsync(exception);
        }
    }
}
