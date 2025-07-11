namespace OneIncTestApp.Hub
{
    public class ProcessingHub : Microsoft.AspNetCore.SignalR.Hub
    {
        private readonly ILogger<ProcessingHub> _logger;

        public ProcessingHub(ILogger<ProcessingHub> logger)
        {
            _logger = logger;
        }
    }
}
