using Moq;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using OneIncTestApp.Services;
using OneIncTestApp.Models;
using OneIncTestApp.API.Services.Interfaces;
using OneIncTestApp.Hub;
using OneIncTestApp.Infrastructure;

public class JobServiceTests
{
    private readonly Mock<IJobQueue> _jobQueueMock;
    private readonly Mock<IJobManager> _jobManagerMock;
    private readonly Mock<IHubContext<ProcessingHub>> _hubContextMock;
    private readonly Mock<ISingleClientProxy> _clientProxyMock;
    private readonly Mock<ILogger<JobService>> _loggerMock;
    private readonly JobService _jobService;

    public JobServiceTests()
    {
        _jobQueueMock = new Mock<IJobQueue>();
        _jobManagerMock = new Mock<IJobManager>();
        _hubContextMock = new Mock<IHubContext<ProcessingHub>>();
        _clientProxyMock = new Mock<ISingleClientProxy>();
        _loggerMock = new Mock<ILogger<JobService>>();

        var clientsMock = new Mock<IHubClients>();
        clientsMock.Setup(c => c.Client(It.IsAny<string>())).Returns(_clientProxyMock.Object);
        _hubContextMock.Setup(h => h.Clients).Returns(clientsMock.Object);

        _jobService = new JobService(_jobQueueMock.Object, _jobManagerMock.Object, _hubContextMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task StartProcessing_ShouldAddJobToQueue()
    {
        // Arrange
        var connectionId = "connection1";
        var input = "Hello";

        var jobQueueMock = new Mock<IJobQueue>();
        var jobManagerMock = new Mock<IJobManager>();
        var hubContextMock = new Mock<IHubContext<ProcessingHub>>();
        var clientProxyMock = new Mock<ISingleClientProxy>();
        var loggerMock = new Mock<ILogger<JobService>>();

        // Mock SignalR behavior
        var clientsMock = new Mock<IHubClients>();
        clientsMock.Setup(c => c.Client(It.IsAny<string>())).Returns(clientProxyMock.Object);
        hubContextMock.Setup(h => h.Clients).Returns(clientsMock.Object);

        var jobService = new JobService(jobQueueMock.Object, jobManagerMock.Object, hubContextMock.Object, loggerMock.Object);

        // Act
        await jobService.StartProcessing(input, connectionId);

        // Assert
        jobQueueMock.Verify(q => q.Enqueue(It.IsAny<Job>()), Times.Once);
        clientProxyMock.Verify(c => c.SendCoreAsync(
            "JobStarted",
            It.Is<object[]>(args => args.Length == 1 && args[0] is string),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelProcessing_ShouldSendProcessingCancelled()
    {
        // Arrange
        var connectionId = "connection1";
        var jobQueueMock = new Mock<IJobQueue>();
        var jobManagerMock = new Mock<IJobManager>();
        var hubContextMock = new Mock<IHubContext<ProcessingHub>>();
        var clientProxyMock = new Mock<ISingleClientProxy>();
        var loggerMock = new Mock<ILogger<JobService>>();

        // Mock SignalR behavior
        var clientsMock = new Mock<IHubClients>();
        clientsMock.Setup(c => c.Client(It.IsAny<string>())).Returns(clientProxyMock.Object);
        hubContextMock.Setup(h => h.Clients).Returns(clientsMock.Object);

        var jobService = new JobService(jobQueueMock.Object, jobManagerMock.Object, hubContextMock.Object, loggerMock.Object);

        var job = new Job
        {
            Id = "job1",
            ConnectionId = connectionId,
            Input = "Hello",
            CancellationTokenSource = new CancellationTokenSource()
        };

        jobManagerMock.Setup(m => m.TryGetJob(connectionId, out job)).Returns(true);

        // Act
        var result = await jobService.CancelProcessing(connectionId);

        // Assert
        Assert.True(result);
        clientProxyMock.Verify(c => c.SendCoreAsync(
            "ProcessingCancelled",
            It.Is<object[]>(args => args.Length == 0),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}