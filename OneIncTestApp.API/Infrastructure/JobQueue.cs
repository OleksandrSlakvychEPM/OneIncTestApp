using OneIncTestApp.Models;
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
        private readonly int _maxQueueSize = 1000;
        private readonly ConcurrentQueue<Job> _jobs = new();
        private readonly SemaphoreSlim _signal = new(0);

        public void Enqueue(Job job)
        {
            if (job is null)
            {
                throw new ArgumentNullException(nameof(job));
            }

            if (_jobs.Count >= _maxQueueSize)
            {
                throw new InvalidOperationException("Job queue is full.");
            }

            _jobs.Enqueue(job);
            _signal.Release();
        }

        public bool TryDequeue(out Job? job)
        {
            return _jobs.TryDequeue(out job);
        }

        public async Task WaitForJobAsync(CancellationToken cancellationToken)
        {
            await _signal.WaitAsync(cancellationToken);
        }

        public int Count => _jobs.Count;
    }
}
