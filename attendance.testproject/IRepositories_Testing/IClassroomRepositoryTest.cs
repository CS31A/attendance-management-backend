using attendance_monitoring.Classes;
using attendance_monitoring.IRepository;
using Moq;
using Xunit;

namespace attendance.testproject.IRepositories_Testing;

public class ClassroomRepositoryTest
{
    private readonly Mock<IClassroomRepository> _mockClassroomRepository;

    public ClassroomRepositoryTest()
    {
        _mockClassroomRepository = new Mock<IClassroomRepository>();
    }

    [Fact]
    public async Task UpdateClassroomAsync_ReturnsUpdatedClassroom()
    {
        // Arrange
        var updated = new Classroom { Id = 1, Name = "Room 101 Updated" };
        _mockClassroomRepository.Setup(r => r.UpdateClassroomAsync(It.IsAny<Classroom>())).ReturnsAsync(updated);

        // Act
        var result = await _mockClassroomRepository.Object.UpdateClassroomAsync(updated);

        // Assert
        Assert.Equal("Room 101 Updated", result.Name);
    }
}