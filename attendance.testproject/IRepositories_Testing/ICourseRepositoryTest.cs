using attendance_monitoring.Classes;
using attendance_monitoring.IRepository;
using Moq;
using Xunit;

namespace attendance.testproject.IRepositories_Testing;

public class CourseRepositoryTest
{
    private readonly Mock<ICourseRepository> _mockCourseRepository;

    public CourseRepositoryTest()
    {
        _mockCourseRepository = new Mock<ICourseRepository>();
    }

    [Fact]
    public async Task GetAllCoursesAsync_ReturnsAllCourses()
    {
        // Arrange
        var courses = new List<Course>
        {
            new Course { Id = 1, Name = "Computer Science" },
            new Course { Id = 2, Name = "Information Technology" }
        };
        _mockCourseRepository.Setup(r => r.GetAllCoursesAsync()).ReturnsAsync(courses);

        // Act
        var result = await _mockCourseRepository.Object.GetAllCoursesAsync();

        // Assert
        Assert.Equal(2, result.Count());
        _mockCourseRepository.Verify(r => r.GetAllCoursesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetCourseByIdAsync_ReturnsCourse_WhenExists()
    {
        // Arrange
        var course = new Course { Id = 1, Name = "Computer Science" };
        _mockCourseRepository.Setup(r => r.GetCourseByIdAsync(1)).ReturnsAsync(course);

        // Act
        var result = await _mockCourseRepository.Object.GetCourseByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Computer Science", result.Name);
    }

    [Fact]
    public async Task GetCourseByIdAsync_ReturnsNull_WhenNotExists()
    {
        // Arrange
        _mockCourseRepository.Setup(r => r.GetCourseByIdAsync(999)).ReturnsAsync((Course?)null);

        // Act
        var result = await _mockCourseRepository.Object.GetCourseByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateCourse_ReturnsCreatedCourse()
    {
        // Arrange
        var newCourse = new Course { Name = "Data Science" };
        var created = new Course { Id = 3, Name = "Data Science" };
        _mockCourseRepository.Setup(r => r.CreateCourse(It.IsAny<Course>())).ReturnsAsync(created);

        // Act
        var result = await _mockCourseRepository.Object.CreateCourse(newCourse);

        // Assert
        Assert.Equal(3, result.Id);
        Assert.Equal("Data Science", result.Name);
    }

    [Fact]
    public async Task UpdateCourseAsync_ReturnsUpdatedCourse()
    {
        // Arrange
        var updated = new Course { Id = 1, Name = "CS Updated" };
        _mockCourseRepository.Setup(r => r.UpdateCourseAsync(It.IsAny<Course>())).ReturnsAsync(updated);

        // Act
        var result = await _mockCourseRepository.Object.UpdateCourseAsync(updated);

        // Assert
        Assert.Equal("CS Updated", result.Name);
    }

    [Fact]
    public async Task DeleteCourseAsync_ReturnsTrue_WhenDeleted()
    {
        // Arrange
        _mockCourseRepository.Setup(r => r.DeleteCourseAsync(1)).ReturnsAsync(true);

        // Act
        var result = await _mockCourseRepository.Object.DeleteCourseAsync(1);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task DeleteCourseAsync_ReturnsFalse_WhenNotFound()
    {
        // Arrange
        _mockCourseRepository.Setup(r => r.DeleteCourseAsync(999)).ReturnsAsync(false);

        // Act
        var result = await _mockCourseRepository.Object.DeleteCourseAsync(999);

        // Assert
        Assert.False(result);
    }
}