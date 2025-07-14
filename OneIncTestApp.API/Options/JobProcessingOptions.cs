namespace OneIncTestApp.Options
{
    public class JobProcessingOptions
    {
        public int MinDelayMilliseconds { get; set; } = 1000;
        public int MaxDelayMilliseconds { get; set; } = 5000;
        public int MaxParallelOperations { get; set; } = 5;
    }
}