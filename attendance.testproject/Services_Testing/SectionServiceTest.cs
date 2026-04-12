using attendance_monitoring.Exceptions;
using attendance_monitoring.IRepository;
using attendance_monitoring.Services;
using Microsoft.Extensions.Logging;

namespace attendance.testproject.Services_Testing;

public class SectionServiceTest
{
    private readonly Mock<ISectionRepository> _mockSectionRepository;
    private readonly Mock<ILogger<SectionService>> _mockLogger;
    private readonly SectionService _service;

    public SectionServiceTest()
    {
        _mockSectionRepository = new Mock<ISectionRepository>();
        _mockLogger = new Mock<ILogger<SectionService>>();
        _service = new SectionService(_mockSectionRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task HasSchedulesInSectionAsync_ReturnsRepositoryResult()
    {
        // Arrange
        const int sectionId = 9;
        _mockSectionRepository
            .Setup(repository => repository.HasSchedulesInSectionAsync(sectionId))
            .ReturnsAsync(true);

        // Act
        var result = await _service.HasSchedulesInSectionAsync(sectionId);

        // Assert
        Assert.True(result);
        _mockSectionRepository.Verify(repository => repository.HasSchedulesInSectionAsync(sectionId), Times.Once);
    }

    [Fact]
    public async Task HasSchedulesInSectionAsync_WrapsUnexpectedRepositoryFailures()
    {
        // Arrange
        const int sectionId = 9;
        var expectedException = new InvalidOperationException("Schedule lookup failed");
        _mockSectionRepository
            .Setup(repository => repository.HasSchedulesInSectionAsync(sectionId))
            .ThrowsAsync(expectedException);

        // Act
        var exception = await Assert.ThrowsAsync<EntityServiceException>(() => _service.HasSchedulesInSectionAsync(sectionId));

        // Assert
        Assert.Equal("Section", exception.EntityName);
        Assert.Equal($"HasSchedulesInSection: {sectionId}", exception.Operation);
        Assert.Equal("Error checking section dependencies", exception.Message);
        Assert.Same(expectedException, exception.InnerException);
    }
}
