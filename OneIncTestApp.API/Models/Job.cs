namespace OneIncTestApp.Models
{
    public class Job
    {
        public string Id { get; set; }
        public string Input { get; set; }
        public string ConnectionId { get; set; }
        public string TabId { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; set; }
    }
}
