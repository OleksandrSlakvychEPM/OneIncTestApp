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
            Input = "Hello, World!"
        };

        // Act
        _jobManager.StartJob(job);

        // Assert
        Assert.True(_jobManager.TryGetJob("connection1", out var retrievedJob));
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
            Input = "Hello, World!",
            CancellationTokenSource = new CancellationTokenSource()
        };

        _jobManager.StartJob(job);

        // Act
        _jobManager.CancelJob("connection1");

        // Assert
        Assert.False(_jobManager.TryGetJob("connection1", out _));
    }

    [Fact]
    public void TryGetJob_ShouldReturnFalseForNonExistentJob()
    {
        // Act
        var result = _jobManager.TryGetJob("nonexistent", out var job);

        // Assert
        Assert.False(result);
        Assert.Null(job);
    }
}