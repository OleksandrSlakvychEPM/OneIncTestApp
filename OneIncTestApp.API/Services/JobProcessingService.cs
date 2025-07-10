using Microsoft.AspNetCore.SignalR;
using OneIncTestApp.Hub;
using OneIncTestApp.Infra;
using OneIncTestApp.Models;

namespace OneIncTestApp.Services
{
    public class JobProcessingService : BackgroundService
    {
        private readonly IJobQueue _jobQueue;
        private readonly IHubContext<ProcessingHub> _hubContext;
        private readonly ILogger<JobProcessingService> _logger;

        public JobProcessingService(IJobQueue jobQueue, IHubContext<ProcessingHub> hubContext, ILogger<JobProcessingService> logger)
        {
            _jobQueue = jobQueue;
            _hubContext = hubContext;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Job Processing Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Wait for a job to be added to the queue
                    await _jobQueue.WaitForJobAsync(stoppingToken);

                    // Try to dequeue a job
                    if (_jobQueue.TryDequeue(out var job))
                    {
                        _logger.LogInformation($"Processing job {job.Id}...");

                        // Process the job
                        await ProcessJobAsync(job, job.CancellationTokenSource.Token);
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
        }

        private async Task ProcessJobAsync(Job job, CancellationToken cancellationToken)
        {
            try
            {
                // Step 1: Count unique characters and their occurrences
                var charCounts = job.Input
                    .GroupBy(c => c) // Group characters by their value
                    .OrderBy(g => g.Key) // Sort by character
                    .Select(g => $"{g.Key}{g.Count()}"); // Format as "character + count"

                // Step 2: Join the counts into a single string
                var countsString = string.Join("", charCounts); // e.g., " 1!1,1H1W1d1e1l3o2r1"

                // Step 3: Base64 encode the input string
                var base64Encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(job.Input)); // e.g., "SGVsbG8sIFdvcmxkIQ=="

                // Step 4: Combine the counts string and Base64 encoded string
                var result = $"{countsString}/{base64Encoded}"; // e.g., " 1!1,1H1W1d1e1l3o2r1/SGVsbG8sIFdvcmxkIQ=="

                await _hubContext.Clients.Client(job.ConnectionId).SendAsync("ProcessingOutputLength", result.Length, cancellationToken);

                foreach (var character in result)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogInformation($"Job {job.Id} was cancelled.");
                        await _hubContext.Clients.Client(job.ConnectionId).SendAsync("ProcessingCancelled");
                        break;
                    }

                    // Simulate random delay (1-5 seconds)
                    await Task.Delay(new Random().Next(1000, 5000), cancellationToken);

                    // Send character to the client via SignalR
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
        }
    }
}
