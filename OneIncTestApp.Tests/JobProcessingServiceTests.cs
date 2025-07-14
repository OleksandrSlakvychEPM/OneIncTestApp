using Moq;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using OneIncTestApp.Services;
using OneIncTestApp.Models;
using OneIncTestApp.Hub;
using OneIncTestApp.Infrastructure;
using Microsoft.Extensions.Options;
using OneIncTestApp.Options;

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

        _jobQueueMock.SetupSequence(q => q.TryDequeue(out job))
            .Returns(true)
            .Returns(false);

        _jobQueueMock.Setup(q => q.WaitForJobAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Task MockDelay(int milliseconds, CancellationToken token) => Task.CompletedTask;
        var mockOptions = new Mock<IOptions<JobProcessingOptions>>();
        mockOptions.Setup(o => o.Value).Returns(new JobProcessingOptions
        {
            MinDelayMilliseconds = 500,
            MaxDelayMilliseconds = 1000
        });

        var service = new JobProcessingService(_jobQueueMock.Object, _hubContextMock.Object, _loggerMock.Object, mockOptions.Object, MockDelay);

        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.CancelAfter(2000);

        // Act
        await service.StartAsync(cancellationTokenSource.Token);

        // Assert
        _clientProxyMock.Verify(c => c.SendCoreAsync(
            "ProcessingOutputLength",
            It.Is<object[]>(args => (int)args[0] == 17),
            It.IsAny<CancellationToken>()), Times.Once);

        _clientProxyMock.Verify(c => c.SendCoreAsync(
            "ReceiveCharacter",
            It.Is<object[]>(args => args.Length == 1 && args[0] is char),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
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

        await job.CancellationTokenSource.CancelAsync();

        _jobQueueMock.SetupSequence(q => q.TryDequeue(out job))
            .Returns(true)
            .Returns(false);

        _jobQueueMock.Setup(q => q.WaitForJobAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var mockOptions = new Mock<IOptions<JobProcessingOptions>>();
        mockOptions.Setup(o => o.Value).Returns(new JobProcessingOptions
        {
            MinDelayMilliseconds = 500,
            MaxDelayMilliseconds = 1000
        });

        var service = new JobProcessingService(_jobQueueMock.Object, _hubContextMock.Object, _loggerMock.Object, mockOptions.Object);

        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.CancelAfter(1000);

        // Act
        await service.StartAsync(cancellationTokenSource.Token);

        // Assert
        _clientProxyMock.Verify(c => c.SendCoreAsync(
            "ProcessingCancelled",
            It.Is<object[]>(args => args.Length == 0),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}