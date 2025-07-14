using OneIncTestApp.Models;

namespace OneIncTestApp.API.Services.Interfaces
{
    public interface IJobManager
    {
        void StartJob(Job job);
        void CancelJob(string connectionId, string tabId);
        void CancelAllJobs(string connectionId);
        bool TryGetJob(string connectionId, string tabId, out Job job);
    }
}
