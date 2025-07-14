using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using OneIncTestApp.Hub;
using OneIncTestApp.Infrastructure;
using OneIncTestApp.Models;
using OneIncTestApp.Options;

namespace OneIncTestApp.Services
{
    public class JobProcessingService : BackgroundService
    {
        private readonly IJobQueue _jobQueue;
        private readonly IHubContext<ProcessingHub> _hubContext;
        private readonly ILogger<JobProcessingService> _logger;
        private readonly JobProcessingOptions _options;
        private readonly Func<int, CancellationToken, Task> _delayFunc;
        private readonly SemaphoreSlim _parallelismSemaphore;

        public JobProcessingService(
            IJobQueue jobQueue,
            IHubContext<ProcessingHub> hubContext,
            ILogger<JobProcessingService> logger,
            IOptions<JobProcessingOptions> options,
            Func<int, CancellationToken, Task> delayFunc = null)
        {
            _jobQueue = jobQueue;
            _hubContext = hubContext;
            _logger = logger;
            _options = options.Value;
            _delayFunc = delayFunc ?? Task.Delay;

            // Limit the number of concurrent jobs (e.g., max 5 jobs at a time)
            _parallelismSemaphore = new SemaphoreSlim(_options.MaxParallelOperations);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Job Processing Service is starting.");

            var runningTasks = new List<Task>();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Wait for a job to be available in the queue
                    await _jobQueue.WaitForJobAsync(stoppingToken);

                    // Dequeue the job
                    if (_jobQueue.TryDequeue(out var job))
                    {
                        _logger.LogInformation($"Processing job {job.Id}...");

                        // Limit parallelism using SemaphoreSlim
                        await _parallelismSemaphore.WaitAsync(stoppingToken);

                        // Start processing the job in a new task
                        var task = Task.Run(async () =>
                        {
                            try
                            {
                                await ProcessJobAsync(job, job.CancellationTokenSource.Token);
                            }
                            finally
                            {
                                // Release the semaphore when processing is complete
                                _parallelismSemaphore.Release();
                            }
                        }, stoppingToken);

                        runningTasks.Add(task);

                        // Remove completed tasks from the list
                        runningTasks.RemoveAll(t => t.IsCompleted);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Job Processing Service is stopping.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while processing a job.");
                }
            }

            // Wait for all running tasks to complete before shutting down
            await Task.WhenAll(runningTasks);
        }

        private async Task ProcessJobAsync(Job job, CancellationToken cancellationToken)
        {
            try
            {
                var charCounts = job.Input
                    .GroupBy(c => c)
                    .OrderBy(g => g.Key)
                    .Select(g => $"{g.Key}{g.Count()}");

                var countsString = string.Join("", charCounts);

                var base64Encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(job.Input));

                var result = $"{countsString}/{base64Encoded}";

                await _hubContext.Clients.Client(job.ConnectionId).SendAsync("ProcessingOutputLength", result.Length, cancellationToken);

                foreach (var character in result)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogInformation($"Job {job.Id} was cancelled.");
                        await _hubContext.Clients.Client(job.ConnectionId).SendAsync("ProcessingCancelled", cancellationToken);
                        break;
                    }

                    await _delayFunc(new Random().Next(_options.MinDelayMilliseconds, _options.MaxDelayMilliseconds), cancellationToken);

                    await _hubContext.Clients.Client(job.ConnectionId).SendAsync("ReceiveCharacter", character, cancellationToken);
                }

                if (!cancellationToken.IsCancellationRequested)
                {
                    await _hubContext.Clients.Client(job.ConnectionId).SendAsync("ProcessingComplete", cancellationToken);
                    _logger.LogInformation($"Job {job.Id} completed.");
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation($"Job {job.Id} was cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while processing job {job.Id}.");
            }

            _logger.LogInformation("Job Processing Service has stopped.");
        }
    }
}
