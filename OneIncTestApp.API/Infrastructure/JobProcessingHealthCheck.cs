using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace OneIncTestApp.Infrastructure
{
    public class JobProcessingHealthCheck : IHealthCheck
    {
        private readonly IJobQueue _jobQueue;

        public JobProcessingHealthCheck(IJobQueue jobQueue)
        {
            _jobQueue = jobQueue;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var jobQueueCount = _jobQueue.Count;

            if (jobQueueCount == 0)
            {
                return Task.FromResult(HealthCheckResult.Healthy("The job processing service is healthy. No pending jobs."));
            }
            else if (jobQueueCount < 10)
            {
                return Task.FromResult(HealthCheckResult.Degraded($"The job processing service is running, but there are {jobQueueCount} pending jobs."));
            }
            else
            {
                return Task.FromResult(HealthCheckResult.Unhealthy($"The job processing service is overloaded with {jobQueueCount} pending jobs."));
            }
        }
    }
}