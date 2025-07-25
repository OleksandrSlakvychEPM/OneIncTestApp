﻿using Microsoft.AspNetCore.SignalR;
using OneIncTestApp.API.Services.Interfaces;
using OneIncTestApp.Hub;
using OneIncTestApp.Infrastructure;
using OneIncTestApp.Models;

namespace OneIncTestApp.Services
{
    public class JobService : IJobService
    {
        private readonly IJobQueue _jobQueue;
        private readonly IJobManager _jobManager;
        private readonly IHubContext<ProcessingHub> _hubContext;
        private readonly ILogger<JobService> _logger;

        public JobService(IJobQueue jobQueue, IJobManager jobManager, IHubContext<ProcessingHub> hubContext, ILogger<JobService> logger)
        {
            _jobQueue = jobQueue;
            _jobManager = jobManager;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task StartProcessing(string input, string connectionId, string tabId)
        {
            var jobId = Guid.NewGuid().ToString();
            var cts = new CancellationTokenSource();

            var job = new Job
            {
                Id = jobId,
                Input = input,
                ConnectionId = connectionId,
                TabId = tabId,
                CancellationTokenSource = cts
            };

            _jobManager.StartJob(job);
            _jobQueue.Enqueue(job);

            _logger.LogInformation($"Job {jobId} added to the queue for client {connectionId} and tab {tabId}.");

            await _hubContext.Clients.Client(connectionId).SendAsync("JobStarted", jobId);
        }

        public async Task<bool> CancelProcessing(string connectionId, string tabId)
        {
            if (!_jobManager.TryGetJob(connectionId, tabId, out var job)) return false;
            _jobManager.CancelJob(connectionId, tabId);
            await _hubContext.Clients.Client(connectionId).SendAsync("ProcessingCancelled");
            _logger.LogInformation($"Client {connectionId} requested cancellation for job on tab {tabId}.");

            return true;
        }
    }
}
