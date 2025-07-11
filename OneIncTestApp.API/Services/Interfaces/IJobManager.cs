using OneIncTestApp.Models;

namespace OneIncTestApp.API.Services.Interfaces
{
    public interface IJobManager
    {
        void StartJob(Job job);
        void CancelJob(string connectionId);
        bool TryGetJob(string connectionId, out Job job);
    }
}
