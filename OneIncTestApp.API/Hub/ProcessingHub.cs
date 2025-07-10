using Microsoft.AspNetCore.SignalR;
using OneIncTestApp.Infra;
using OneIncTestApp.Models;
using OneIncTestApp.Services;

namespace OneIncTestApp.Hub
{
    public class ProcessingHub : Microsoft.AspNetCore.SignalR.Hub
    {
        private readonly IJobQueue _jobQueue;
        private readonly IJobManager _jobManager;
        private readonly ILogger<ProcessingHub> _logger;

        public ProcessingHub(IJobQueue jobQueue, IJobManager jobManager, ILogger<ProcessingHub> logger)
        {
            _jobQueue = jobQueue;
            _jobManager = jobManager;
            _logger = logger;
        }

        public async Task StartProcessing(string input, string connectionId)
        {
            var jobId = Guid.NewGuid().ToString();
            var cts = new CancellationTokenSource();

            var job = new Job
            {
                Id = jobId,
                Input = input,
                ConnectionId = connectionId,
                CancellationTokenSource = cts
            };

            _jobManager.StartJob(job);

            _jobQueue.Enqueue(job);

            _logger.LogInformation($"Job {jobId} added to the queue by client {Context.ConnectionId}.");

            await Clients.Client(connectionId).SendAsync("JobStarted", jobId);
        }

        public async Task CancelProcessing()
        {
            // This implementation assumes you maintain a way to track jobs (e.g., a dictionary of active jobs).

            _jobManager.CancelJob(Context.ConnectionId);

            _logger.LogInformation($"Client {Context.ConnectionId} requested cancellation for job.");

            // Notify the client that cancellation has been requested
            await Clients.Caller.SendAsync("ProcessingCancelled");
        }
    }
}
