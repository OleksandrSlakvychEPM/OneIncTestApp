using Moq;
using Microsoft.Extensions.Logging;
using OneIncTestApp.Models;
using OneIncTestApp.Services;

public class JobManagerTests
{
    private readonly JobManager _jobManager;
    private readonly Mock<ILogger<JobManager>> _loggerMock;

    public JobManagerTests()
    {
        _loggerMock = new Mock<ILogger<JobManager>>();
        _jobManager = new JobManager(_loggerMock.Object);
    }

    [Fact]
    public void StartJob_ShouldAddJobToDictionary()
    {
        // Arrange
        var job = new Job
        {
            Id = "job1",
            ConnectionId = "connection1",
            TabId = "tab1",
            Input = "Hello, World!"
        };

        // Act
        _jobManager.StartJob(job);

        // Assert
        Assert.True(_jobManager.TryGetJob("connection1", "tab1", out var retrievedJob));
        Assert.Equal(job, retrievedJob);
    }

    [Fact]
    public void CancelJob_ShouldRemoveJobFromDictionary()
    {
        // Arrange
        var job = new Job
        {
            Id = "job1",
            ConnectionId = "connection1",
            TabId = "tab1",
            Input = "Hello, World!",
            CancellationTokenSource = new CancellationTokenSource()
        };

        _jobManager.StartJob(job);

        // Act
        _jobManager.CancelJob("connection1", "tab1");

        // Assert
        Assert.False(_jobManager.TryGetJob("connection1", "tab1", out _));
    }

    [Fact]
    public void CancelAllJobs_ShouldRemoveAllJobsForConnectionId()
    {
        // Arrange
        var job1 = new Job
        {
            Id = "job1",
            ConnectionId = "connection1",
            TabId = "tab1",
            Input = "Hello, World!",
            CancellationTokenSource = new CancellationTokenSource()
        };

        var job2 = new Job
        {
            Id = "job2",
            ConnectionId = "connection1",
            TabId = "tab2",
            Input = "Hello, World!",
            CancellationTokenSource = new CancellationTokenSource()
        };

        var job3 = new Job
        {
            Id = "job3",
            ConnectionId = "connection2",
            TabId = "tab1",
            Input = "Hello, World!",
            CancellationTokenSource = new CancellationTokenSource()
        };

        _jobManager.StartJob(job1);
        _jobManager.StartJob(job2);
        _jobManager.StartJob(job3);

        // Act
        _jobManager.CancelAllJobs("connection1");

        // Assert
        Assert.False(_jobManager.TryGetJob("connection1", "tab1", out _));
        Assert.False(_jobManager.TryGetJob("connection1", "tab2", out _));
        Assert.True(_jobManager.TryGetJob("connection2", "tab1", out _));
    }

    [Fact]
    public void TryGetJob_ShouldReturnFalseForNonExistentJob()
    {
        // Act
        var result = _jobManager.TryGetJob("nonexistent", "tab1", out var job);

        // Assert
        Assert.False(result);
        Assert.Null(job);
    }
}