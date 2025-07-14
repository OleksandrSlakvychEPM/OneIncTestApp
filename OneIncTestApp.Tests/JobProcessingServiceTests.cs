using Moq;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OneIncTestApp.Hub;
using OneIncTestApp.Infrastructure;
using OneIncTestApp.Models;
using OneIncTestApp.Options;
using OneIncTestApp.Services;
using Xunit;

public class JobProcessingServiceTests
{
    private readonly Mock<IJobQueue> _jobQueueMock;
    private readonly Mock<IHubContext<ProcessingHub>> _hubContextMock;
    private readonly Mock<ISingleClientProxy> _clientProxyMock;
    private readonly Mock<ILogger<JobProcessingService>> _loggerMock;

    public JobProcessingServiceTests()
    {
        _jobQueueMock = new Mock<IJobQueue>();
        _hubContextMock = new Mock<IHubContext<ProcessingHub>>();
        _clientProxyMock = new Mock<ISingleClientProxy>();
        _loggerMock = new Mock<ILogger<JobProcessingService>>();

        // Mock the HubContext to return the mocked ClientProxy
        var clientsMock = new Mock<IHubClients>();
        clientsMock.Setup(c => c.Client(It.IsAny<string>())).Returns(_clientProxyMock.Object);
        _hubContextMock.Setup(h => h.Clients).Returns(clientsMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldProcessJobAndSendMessages()
    {
        // Arrange
        var job = new Job
        {
            Id = "job1",
            ConnectionId = "connection1",
            Input = "Hello",
            CancellationTokenSource = new CancellationTokenSource()
        };

        // Mock the job queue to return the job and then stop returning jobs
        _jobQueueMock.SetupSequence(q => q.TryDequeue(out job))
            .Returns(true)
            .Returns(false);

        _jobQueueMock.Setup(q => q.WaitForJobAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Mock the delay function to avoid actual delays in the test
        Task MockDelay(int milliseconds, CancellationToken token) => Task.CompletedTask;

        // Mock the options for JobProcessingOptions
        var mockOptions = new Mock<IOptions<JobProcessingOptions>>();
        mockOptions.Setup(o => o.Value).Returns(new JobProcessingOptions
        {
            MinDelayMilliseconds = 500,
            MaxDelayMilliseconds = 1000,
            MaxParallelOperations = 5
        });

        var service = new JobProcessingService(
            _jobQueueMock.Object,
            _hubContextMock.Object,
            _loggerMock.Object,
            mockOptions.Object,
            MockDelay);

        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.CancelAfter(2000); // Stop the service after 2 seconds

        // Act
        await service.StartAsync(cancellationTokenSource.Token);

        // Assert
        _clientProxyMock.Verify(c => c.SendCoreAsync(
            "ProcessingOutputLength",
            It.Is<object[]>(args => (int)args[0] == 17), // "Hello" => "H1e1l2o1/SGVsbG8="
            It.IsAny<CancellationToken>()), Times.Once);

        _clientProxyMock.Verify(c => c.SendCoreAsync(
            "ReceiveCharacter",
            It.Is<object[]>(args => args.Length == 1 && args[0] is char),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);

        _clientProxyMock.Verify(c => c.SendCoreAsync(
            "ProcessingComplete",
            It.IsAny<object[]>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldHandleJobCancellation()
    {
        // Arrange
        var job = new Job
        {
            Id = "job1",
            ConnectionId = "connection1",
            Input = "Hello",
            CancellationTokenSource = new CancellationTokenSource()
        };

        // Simulate job cancellation
        job.CancellationTokenSource.Cancel();

        _jobQueueMock.SetupSequence(q => q.TryDequeue(out job))
            .Returns(true)
            .Returns(false);

        _jobQueueMock.Setup(q => q.WaitForJobAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Mock the options for JobProcessingOptions
        var mockOptions = new Mock<IOptions<JobProcessingOptions>>();
        mockOptions.Setup(o => o.Value).Returns(new JobProcessingOptions
        {
            MinDelayMilliseconds = 500,
            MaxDelayMilliseconds = 1000,
            MaxParallelOperations = 5
        });

        var service = new JobProcessingService(
            _jobQueueMock.Object,
            _hubContextMock.Object,
            _loggerMock.Object,
            mockOptions.Object);

        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.CancelAfter(1000); // Stop the service after 1 second

        // Act
        await service.StartAsync(cancellationTokenSource.Token);

        // Assert
        _clientProxyMock.Verify(c => c.SendCoreAsync(
            "ProcessingCancelled",
            It.Is<object[]>(args => args.Length == 0),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldLimitParallelism()
    {
        // Arrange
        var job1 = new Job
        {
            Id = "job1",
            ConnectionId = "connection1",
            Input = "Job1",
            CancellationTokenSource = new CancellationTokenSource()
        };

        var job2 = new Job
        {
            Id = "job2",
            ConnectionId = "connection2",
            Input = "Job2",
            CancellationTokenSource = new CancellationTokenSource()
        };

        _jobQueueMock.Setup(q => q.WaitForJobAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var jobs = new Queue<Job>(new[] { job1, job2});

        _jobQueueMock.Setup(q => q.TryDequeue(out It.Ref<Job>.IsAny))
            .Returns((out Job job) =>
            {
                if (jobs.Count > 0)
                {
                    job = jobs.Dequeue();
                    return true;
                }

                job = null;
                return false;
            });

        var mockOptions = new Mock<IOptions<JobProcessingOptions>>();
        mockOptions.Setup(o => o.Value).Returns(new JobProcessingOptions
        {
            MinDelayMilliseconds = 500,
            MaxDelayMilliseconds = 1000,
            MaxParallelOperations = 2
        });

        Task MockDelay(int milliseconds, CancellationToken token) => Task.Delay(1, token);

        var service = new JobProcessingService(
            _jobQueueMock.Object,
            _hubContextMock.Object,
            _loggerMock.Object,
            mockOptions.Object,
            MockDelay);

        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.CancelAfter(10000);

        // Act
        await service.StartAsync(cancellationTokenSource.Token);

        // Assert
        _clientProxyMock.Verify(c => c.SendCoreAsync(
            "ProcessingOutputLength",
            It.IsAny<object[]>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));

        _clientProxyMock.Verify(c => c.SendCoreAsync(
            "ProcessingComplete",
            It.IsAny<object[]>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}