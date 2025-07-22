using Microsoft.Extensions.Options;
using OneIncTestApp.Models;
using OneIncTestApp.Options;
using System.Collections.Concurrent;

namespace OneIncTestApp.Infrastructure
{
    public interface IJobQueue
    {
        void Enqueue(Job job);
        bool TryDequeue(out Job? job);
        Task WaitForJobAsync(CancellationToken cancellationToken);
        int Count { get; }
    }

    public class JobQueue : IJobQueue
    {
        private readonly JobQueueOptions _options;
        private readonly ConcurrentQueue<Job> _jobs = new();
        private readonly SemaphoreSlim _signal;

        public JobQueue(IOptions<JobQueueOptions> options)
        {
            _options = options.Value;

            _signal = new SemaphoreSlim(_options.MaxQueueSize);
        }

        public void Enqueue(Job job)
        {
            if (job is null)
            {
                throw new ArgumentNullException(nameof(job));
            }

            if (_jobs.Count >= _options.MaxQueueSize)
            {
                throw new InvalidOperationException("Job queue is full.");
            }

            _jobs.Enqueue(job);

            _signal.Release();
        }

        public bool TryDequeue(out Job? job)
        {
            var dequeued = _jobs.TryDequeue(out job);

            if (dequeued)
            {
                _signal.Wait();
            }

            return dequeued;
        }

        public async Task WaitForJobAsync(CancellationToken cancellationToken)
        {
            await _signal.WaitAsync(cancellationToken);
        }

        public int Count => _jobs.Count;
    }
}
