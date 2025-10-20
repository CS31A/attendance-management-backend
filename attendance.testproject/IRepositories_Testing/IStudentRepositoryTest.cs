using attendance_monitoring.Classes;
using attendance_monitoring.IRepository;
using Moq;
using Xunit;

namespace attendance.testproject.IRepositories_Testing;

/// <summary>
/// Tests for IStudentRepository implementation
/// </summary>
public class StudentRepositoryTest
{
    private readonly Mock<IStudentRepository> _mockStudentRepository;

    public StudentRepositoryTest()
    {
        _mockStudentRepository = new Mock<IStudentRepository>();
    }

    [Fact]
    public async Task CreateStudent_ReturnsCreatedStudent()
    {
        // Arrange
        var newStudent = new Student
        {
            Firstname = "Alice",
            Lastname = "Williams",
            Email = "alice.williams@example.com"
        };
        var createdStudent = new Student
        {
            Id = 4,
            Firstname = "Alice",
            Lastname = "Williams",
            Email = "alice.williams@example.com",
            IsDeleted = false
        };
        _mockStudentRepository
            .Setup(r => r.CreateStudent(It.IsAny<Student>()))
            .ReturnsAsync(createdStudent);

        // Act
        var result = await _mockStudentRepository.Object.CreateStudent(newStudent);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.Id);
        Assert.Equal("Alice", result.Firstname);
        Assert.Equal("alice.williams@example.com", result.Email);
        Assert.False(result.IsDeleted);

        _mockStudentRepository.Verify(r => r.CreateStudent(It.IsAny<Student>()), Times.Once);
    }
}