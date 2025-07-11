namespace OneIncTestApp.API.Services.Interfaces
{
    public interface IJobService
    {
        Task StartProcessing(string input, string connectionId);
        Task<bool> CancelProcessing(string connectionId);
    }
}
