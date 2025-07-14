using Moq;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using OneIncTestApp.Services;
using OneIncTestApp.Models;
using OneIncTestApp.API.Services.Interfaces;
using OneIncTestApp.Hub;
using OneIncTestApp.Infrastructure;
using Xunit;

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
        var tabId = "tab1";
        var input = "Hello";

        // Act
        await _jobService.StartProcessing(input, connectionId, tabId);

        // Assert
        _jobQueueMock.Verify(q => q.Enqueue(It.Is<Job>(job =>
            job.ConnectionId == connectionId &&
            job.TabId == tabId &&
            job.Input == input)), Times.Once);

        _clientProxyMock.Verify(c => c.SendCoreAsync(
            "JobStarted",
            It.Is<object[]>(args => args.Length == 1 && args[0] is string),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelProcessing_ShouldSendProcessingCancelled()
    {
        // Arrange
        var connectionId = "connection1";
        var tabId = "tab1";

        var job = new Job
        {
            Id = "job1",
            ConnectionId = connectionId,
            TabId = tabId,
            Input = "Hello",
            CancellationTokenSource = new CancellationTokenSource()
        };

        _jobManagerMock.Setup(m => m.TryGetJob(connectionId, tabId, out job)).Returns(true);

        // Act
        var result = await _jobService.CancelProcessing(connectionId, tabId);

        // Assert
        Assert.True(result);
        _jobManagerMock.Verify(m => m.CancelJob(connectionId, tabId), Times.Once);

        _clientProxyMock.Verify(c => c.SendCoreAsync(
            "ProcessingCancelled",
            It.Is<object[]>(args => args.Length == 0),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelProcessing_ShouldReturnFalseIfJobDoesNotExist()
    {
        // Arrange
        var connectionId = "connection1";
        var tabId = "tab1";

        Job job = null;
        _jobManagerMock.Setup(m => m.TryGetJob(connectionId, tabId, out job)).Returns(false);

        // Act
        var result = await _jobService.CancelProcessing(connectionId, tabId);

        // Assert
        Assert.False(result);
        _clientProxyMock.Verify(c => c.SendCoreAsync(
            "ProcessingCancelled",
            It.IsAny<object[]>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}