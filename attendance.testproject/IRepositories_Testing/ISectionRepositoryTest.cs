using attendance_monitoring.Classes;
using attendance_monitoring.IRepository;
using Moq;
using Xunit;

namespace attendance.testproject.IRepositories_Testing;

/// <summary>
/// Tests for ISectionRepository implementation
/// </summary>
public class SectionRepositoryTest
{
    private readonly Mock<ISectionRepository> _mockSectionRepository;

    public SectionRepositoryTest()
    {
        _mockSectionRepository = new Mock<ISectionRepository>();
    }

    [Fact]
    public async Task GetSectionByIdAsync_ReturnsSection_WhenSectionExists()
    {
        // Arrange
        var expectedSection = new Section
        {
            Id = 1,
            Name = "Section A"
        };
        _mockSectionRepository
            .Setup(r => r.GetSectionByIdAsync(1))
            .ReturnsAsync(expectedSection);

        // Act
        var result = await _mockSectionRepository.Object.GetSectionByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Section A", result.Name);

        _mockSectionRepository.Verify(r => r.GetSectionByIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetSectionByIdAsync_ReturnsNull_WhenSectionDoesNotExist()
    {
        // Arrange
        _mockSectionRepository
            .Setup(r => r.GetSectionByIdAsync(999))
            .ReturnsAsync((Section?)null);

        // Act
        var result = await _mockSectionRepository.Object.GetSectionByIdAsync(999);

        // Assert
        Assert.Null(result);

        _mockSectionRepository.Verify(r => r.GetSectionByIdAsync(999), Times.Once);
    }

    [Fact]
    public async Task GetAllSectionsAsync_ReturnsAllSections()
    {
        // Arrange
        var expectedSections = new List<Section>
        {
            new Section { Id = 1, Name = "Section A"},
            new Section { Id = 2, Name = "Section B"},
            new Section { Id = 3, Name = "Section C"}
        };
        _mockSectionRepository
            .Setup(r => r.GetAllSectionsAsync())
            .ReturnsAsync(expectedSections);

        // Act
        var result = await _mockSectionRepository.Object.GetAllSectionsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
        Assert.Equal("Section A", result.First().Name);
        Assert.Equal("Section C", result.Last().Name);

        _mockSectionRepository.Verify(r => r.GetAllSectionsAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllSectionsAsync_ReturnsEmptyList_WhenNoSectionsExist()
    {
        // Arrange
        var expectedSections = new List<Section>();
        _mockSectionRepository
            .Setup(r => r.GetAllSectionsAsync())
            .ReturnsAsync(expectedSections);

        // Act
        var result = await _mockSectionRepository.Object.GetAllSectionsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);

        _mockSectionRepository.Verify(r => r.GetAllSectionsAsync(), Times.Once);
    }

    [Fact]
    public async Task GetActiveStudentsBySectionIdAsync_ReturnsActiveStudents()
    {
        // Arrange
        var expectedStudents = new List<Student>
        {
            new Student { Id = 1, Firstname = "John", Lastname = "Doe", Email = "john.doe@example.com", IsDeleted = false },
            new Student { Id = 2, Firstname = "Jane", Lastname = "Smith", Email = "jane.smith@example.com", IsDeleted = false }
        };
        _mockSectionRepository
            .Setup(r => r.GetActiveStudentsBySectionIdAsync(1))
            .ReturnsAsync(expectedStudents);

        // Act
        var result = await _mockSectionRepository.Object.GetActiveStudentsBySectionIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, student => Assert.False(student.IsDeleted));
        Assert.Equal("John", result.First().Firstname);
        Assert.Equal("jane.smith@example.com", result.Last().Email);

        _mockSectionRepository.Verify(r => r.GetActiveStudentsBySectionIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetActiveStudentsBySectionIdAsync_ReturnsEmptyList_WhenNoActiveStudents()
    {
        // Arrange
        var expectedStudents = new List<Student>();
        _mockSectionRepository
            .Setup(r => r.GetActiveStudentsBySectionIdAsync(1))
            .ReturnsAsync(expectedStudents);

        // Act
        var result = await _mockSectionRepository.Object.GetActiveStudentsBySectionIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);

        _mockSectionRepository.Verify(r => r.GetActiveStudentsBySectionIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetAllStudentsBySectionIdAsync_ReturnsAllStudents_IncludingDeleted()
    {
        // Arrange
        var expectedStudents = new List<Student>
        {
            new Student { Id = 1, Firstname = "John", Lastname = "Doe", Email = "john.doe@example.com", IsDeleted = false },
            new Student { Id = 2, Firstname = "Jane", Lastname = "Smith", Email = "jane.smith@example.com", IsDeleted = false },
            new Student { Id = 3, Firstname = "Bob", Lastname = "Johnson", Email = "bob.johnson@example.com", IsDeleted = true }
        };
        _mockSectionRepository
            .Setup(r => r.GetAllStudentsBySectionIdAsync(1))
            .ReturnsAsync(expectedStudents);

        // Act
        var result = await _mockSectionRepository.Object.GetAllStudentsBySectionIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
        Assert.Contains(result, s => s.IsDeleted);
        Assert.Contains(result, s => !s.IsDeleted);
        Assert.Equal("John", result.First().Firstname);

        _mockSectionRepository.Verify(r => r.GetAllStudentsBySectionIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetAllStudentsBySectionIdAsync_ReturnsEmptyList_WhenNoStudentsInSection()
    {
        // Arrange
        var expectedStudents = new List<Student>();
        _mockSectionRepository
            .Setup(r => r.GetAllStudentsBySectionIdAsync(999))
            .ReturnsAsync(expectedStudents);

        // Act
        var result = await _mockSectionRepository.Object.GetAllStudentsBySectionIdAsync(999);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);

        _mockSectionRepository.Verify(r => r.GetAllStudentsBySectionIdAsync(999), Times.Once);
    }

    [Fact]
    public async Task CreateSectionAsync_ReturnsCreatedSection()
    {
        // Arrange
        var newSection = new Section
        {
            Name = "Section D"
        };
        var createdSection = new Section
        {
            Id = 4,
            Name = "Section D"
        };
        _mockSectionRepository
            .Setup(r => r.CreateSectionAsync(It.IsAny<Section>()))
            .ReturnsAsync(createdSection);

        // Act
        var result = await _mockSectionRepository.Object.CreateSectionAsync(newSection);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.Id);
        Assert.Equal("Section D", result.Name);

        _mockSectionRepository.Verify(r => r.CreateSectionAsync(It.IsAny<Section>()), Times.Once);
    }

    [Fact]
    public async Task UpdateSectionAsync_ReturnsUpdatedSection_WhenSectionExists()
    {
        // Arrange
        var sectionToUpdate = new Section
        {
            Id = 1,
            Name = "Section A Updated"
        };
        _mockSectionRepository
            .Setup(r => r.UpdateSectionAsync(1, It.IsAny<Section>()))
            .ReturnsAsync(sectionToUpdate);

        // Act
        var result = await _mockSectionRepository.Object.UpdateSectionAsync(1, sectionToUpdate);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Section A Updated", result.Name);

        _mockSectionRepository.Verify(r => r.UpdateSectionAsync(1, It.IsAny<Section>()), Times.Once);
    }

    [Fact]
    public async Task UpdateSectionAsync_ReturnsNull_WhenSectionDoesNotExist()
    {
        // Arrange
        var sectionToUpdate = new Section
        {
            Id = 999,
            Name = "Non-existent Section"
        };
        _mockSectionRepository
            .Setup(r => r.UpdateSectionAsync(999, It.IsAny<Section>()))
            .ReturnsAsync((Section?)null);

        // Act
        var result = await _mockSectionRepository.Object.UpdateSectionAsync(999, sectionToUpdate);

        // Assert
        Assert.Null(result);

        _mockSectionRepository.Verify(r => r.UpdateSectionAsync(999, It.IsAny<Section>()), Times.Once);
    }

    [Fact]
    public async Task DeleteSectionAsync_ReturnsTrue_WhenSectionDeleted()
    {
        // Arrange
        _mockSectionRepository
            .Setup(r => r.DeleteSectionAsync(1))
            .ReturnsAsync(true);

        // Act
        var result = await _mockSectionRepository.Object.DeleteSectionAsync(1);

        // Assert
        Assert.True(result);

        _mockSectionRepository.Verify(r => r.DeleteSectionAsync(1), Times.Once);
    }

    [Fact]
    public async Task DeleteSectionAsync_ReturnsFalse_WhenSectionNotFound()
    {
        // Arrange
        _mockSectionRepository
            .Setup(r => r.DeleteSectionAsync(999))
            .ReturnsAsync(false);

        // Act
        var result = await _mockSectionRepository.Object.DeleteSectionAsync(999);

        // Assert
        Assert.False(result);

        _mockSectionRepository.Verify(r => r.DeleteSectionAsync(999), Times.Once);
    }
}