using System.Collections.Concurrent;
using OneIncTestApp.API.Services.Interfaces;
using OneIncTestApp.Models;

namespace OneIncTestApp.Services
{
    public class JobManager : IJobManager
    {
        private readonly ConcurrentDictionary<(string ConnectionId, string TabId), Job> _jobs = new();
        private readonly ILogger<JobManager> _logger;

        public JobManager(ILogger<JobManager> logger)
        {
            _logger = logger;
        }

        public void StartJob(Job job)
        {
            _jobs[(job.ConnectionId, job.TabId)] = job;
            _logger.LogInformation($"Job Manager StartJob {job.Id} for tab {job.TabId}.");
        }

        public bool TryGetJob(string connectionId, string tabId, out Job job)
        {
            return _jobs.TryGetValue((connectionId, tabId), out job);
        }

        public void CancelJob(string connectionId, string tabId)
        {
            if (_jobs.TryRemove((connectionId, tabId), out var job))
            {
                job.CancellationTokenSource.Cancel();
                job.CancellationTokenSource.Dispose();
                _logger.LogInformation($"Job Manager CancelJob {job.Id} for tab {tabId}.");
            }
            else
            {
                _logger.LogInformation($"No jobs found for connection {connectionId} and tab {tabId}.");
            }
        }

        public void CancelAllJobs(string connectionId)
        {
            var jobsToCancel = _jobs.Where(kvp => kvp.Key.ConnectionId == connectionId).ToList();

            foreach (var jobEntry in jobsToCancel)
            {
                if (_jobs.TryRemove(jobEntry.Key, out var job))
                {
                    job.CancellationTokenSource.Cancel();
                    job.CancellationTokenSource.Dispose();

                    _logger.LogInformation($"Job Manager CancelJob {job.Id} for tab {job.TabId}.");
                }
            }

            if (!jobsToCancel.Any())
            {
                _logger.LogInformation($"No jobs found for connection {connectionId}.");
            }
        }
    }
}
