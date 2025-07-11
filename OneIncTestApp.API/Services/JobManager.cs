using System.Collections.Concurrent;
using OneIncTestApp.API.Services.Interfaces;
using OneIncTestApp.Models;

namespace OneIncTestApp.Services
{
    public class JobManager : IJobManager
    {
        private readonly ConcurrentDictionary<string, Job> _jobs = new();
        private readonly ILogger<JobManager> _logger;

        public JobManager(ILogger<JobManager> logger)
        {
            _logger = logger;
        }

        public void StartJob(Job job)
        {
            _jobs[job.ConnectionId] = job;
            _logger.LogInformation($"Job Manager StartJob {job.Id}.");
        }

        public bool TryGetJob(string connectionId, out Job job)
        {
            return _jobs.TryGetValue(connectionId, out job);
        }

        public void CancelJob(string connectionId)
        {
            if (_jobs.TryRemove(connectionId, out var job))
            {
                job.CancellationTokenSource.Cancel();
                job.CancellationTokenSource.Dispose();
                _logger.LogInformation($"Job Manager CancelJob {job.Id}.");
            }

            _logger.LogInformation($"Any jobs are found for connection {connectionId}.");
        }
    }
}
