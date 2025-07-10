namespace OneIncTestApp.Models
{
    public class Job
    {
        public string Id { get; set; }
        public string Input { get; set; }
        public string ConnectionId { get; set; } // SignalR connection ID
        public CancellationTokenSource CancellationTokenSource { get; set; }
    }
}
