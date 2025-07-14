namespace OneIncTestApp.API.Services.Interfaces
{
    public interface IJobService
    {
        Task StartProcessing(string input, string connectionId, string tabId);
        Task<bool> CancelProcessing(string connectionId, string tabId);
    }
}
