using attendance_monitoring.Classes;
using attendance_monitoring.IRepository;
using Moq;
using Xunit;

namespace attendance.testproject.IRepositories_Testing;

/// <summary>
/// Tests for ISubjectRepository implementation
/// </summary>
public class SubjectRepositoryTest
{
    private readonly Mock<ISubjectRepository> _mockSubjectRepository;

    public SubjectRepositoryTest()
    {
        _mockSubjectRepository = new Mock<ISubjectRepository>();
    }

    [Fact]
    public async Task GetAllSubjectsAsync_ReturnsAllSubjects()
    {
        // Arrange
        var expectedSubjects = new List<Subject>
        {
            new Subject { Id = 1, Code = "CS101", Name = "Introduction to Programming"},
            new Subject { Id = 2, Code = "CS102", Name = "Data Structures"}
        };
        _mockSubjectRepository
            .Setup(r => r.GetAllSubjectsAsync())
            .ReturnsAsync(expectedSubjects);

        // Act
        var result = await _mockSubjectRepository.Object.GetAllSubjectsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.Equal("CS101", result.First().Code);
        Assert.Equal("Data Structures", result.Last().Name);

        _mockSubjectRepository.Verify(r => r.GetAllSubjectsAsync(), Times.Once);
    }

    [Fact]
    public async Task GetSubjectByIdAsync_ReturnsSubject_WhenSubjectExists()
    {
        // Arrange
        var expectedSubject = new Subject
        {
            Id = 1,
            Code = "CS101",
            Name = "Introduction to Programming"
        };
        _mockSubjectRepository
            .Setup(r => r.GetSubjectByIdAsync(1))
            .ReturnsAsync(expectedSubject);

        // Act
        var result = await _mockSubjectRepository.Object.GetSubjectByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("CS101", result.Code);
        Assert.Equal("Introduction to Programming", result.Name);

        _mockSubjectRepository.Verify(r => r.GetSubjectByIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetSubjectByIdAsync_ReturnsNull_WhenSubjectDoesNotExist()
    {
        // Arrange
        _mockSubjectRepository
            .Setup(r => r.GetSubjectByIdAsync(999))
            .ReturnsAsync((Subject?)null);

        // Act
        var result = await _mockSubjectRepository.Object.GetSubjectByIdAsync(999);

        // Assert
        Assert.Null(result);

        _mockSubjectRepository.Verify(r => r.GetSubjectByIdAsync(999), Times.Once);
    }

    [Fact]
    public async Task CreateSubject_ReturnsCreatedSubject()
    {
        // Arrange
        var newSubject = new Subject
        {
            Code = "CS103",
            Name = "Algorithms"
        };
        var createdSubject = new Subject
        {
            Id = 3,
            Code = "CS103",
            Name = "Algorithms"
        };
        _mockSubjectRepository
            .Setup(r => r.CreateSubject(It.IsAny<Subject>()))
            .ReturnsAsync(createdSubject);

        // Act
        var result = await _mockSubjectRepository.Object.CreateSubject(newSubject);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Id);
        Assert.Equal("CS103", result.Code);
        Assert.Equal("Algorithms", result.Name);

        _mockSubjectRepository.Verify(r => r.CreateSubject(It.IsAny<Subject>()), Times.Once);
    }

    [Fact]
    public async Task UpdateSubjectAsync_ReturnsUpdatedSubject()
    {
        // Arrange
        var subjectToUpdate = new Subject
        {
            Id = 1,
            Code = "CS101",
            Name = "Advanced Programming"
        };
        _mockSubjectRepository
            .Setup(r => r.UpdateSubjectAsync(It.IsAny<Subject>()))
            .ReturnsAsync(subjectToUpdate);

        // Act
        var result = await _mockSubjectRepository.Object.UpdateSubjectAsync(subjectToUpdate);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Advanced Programming", result.Name);

        _mockSubjectRepository.Verify(r => r.UpdateSubjectAsync(It.IsAny<Subject>()), Times.Once);
    }

    [Fact]
    public async Task DeleteSubjectAsync_ReturnsTrue_WhenSubjectDeleted()
    {
        // Arrange
        _mockSubjectRepository
            .Setup(r => r.DeleteSubjectAsync(1))
            .ReturnsAsync(true);

        // Act
        var result = await _mockSubjectRepository.Object.DeleteSubjectAsync(1);

        // Assert
        Assert.True(result);

        _mockSubjectRepository.Verify(r => r.DeleteSubjectAsync(1), Times.Once);
    }

    [Fact]
    public async Task DeleteSubjectAsync_ReturnsFalse_WhenSubjectNotFound()
    {
        // Arrange
        _mockSubjectRepository
            .Setup(r => r.DeleteSubjectAsync(999))
            .ReturnsAsync(false);

        // Act
        var result = await _mockSubjectRepository.Object.DeleteSubjectAsync(999);

        // Assert
        Assert.False(result);

        _mockSubjectRepository.Verify(r => r.DeleteSubjectAsync(999), Times.Once);
    }

    [Fact]
    public async Task GetSubjectByCodeAsync_ReturnsSubject_WhenCodeExists()
    {
        // Arrange
        var expectedSubject = new Subject
        {
            Id = 1,
            Code = "CS101",
            Name = "Introduction to Programming"
        };
        _mockSubjectRepository
            .Setup(r => r.GetSubjectByCodeAsync("CS101"))
            .ReturnsAsync(expectedSubject);

        // Act
        var result = await _mockSubjectRepository.Object.GetSubjectByCodeAsync("CS101");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("CS101", result.Code);
        Assert.Equal("Introduction to Programming", result.Name);

        _mockSubjectRepository.Verify(r => r.GetSubjectByCodeAsync("CS101"), Times.Once);
    }

    [Fact]
    public async Task GetSubjectByCodeAsync_ReturnsNull_WhenCodeDoesNotExist()
    {
        // Arrange
        _mockSubjectRepository
            .Setup(r => r.GetSubjectByCodeAsync("INVALID"))
            .ReturnsAsync((Subject?)null);

        // Act
        var result = await _mockSubjectRepository.Object.GetSubjectByCodeAsync("INVALID");

        // Assert
        Assert.Null(result);

        _mockSubjectRepository.Verify(r => r.GetSubjectByCodeAsync("INVALID"), Times.Once);
    }

    [Fact]
    public async Task SaveChangesAsync_CallsSaveMethod()
    {
        // Arrange
        _mockSubjectRepository
            .Setup(r => r.SaveChangesAsync());

        // Act
        await _mockSubjectRepository.Object.SaveChangesAsync();

        // Assert
        _mockSubjectRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
}