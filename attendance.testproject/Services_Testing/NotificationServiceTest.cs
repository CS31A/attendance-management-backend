using attendance_monitoring.Classes;
using attendance_monitoring.Hubs;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Response;
using attendance_monitoring.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace attendance.testproject.Services_Testing;

public class NotificationServiceTest
{
    [Fact]
    public async Task NotifySessionStartedAsync_SendsReceiveNotificationToStudentsAndInstructorOnly()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var studentIds = new[] { "student-1", "student-2" };
        const string instructorId = "instructor-1";
        var sentNotifications = new List<(IReadOnlyList<string> Connections, NotificationDto Notification)>();
        var service = CreateNotificationService(sessionId, sentNotifications);

        // Act
        await service.NotifySessionStartedAsync(sessionId, studentIds, instructorId);

        // Assert
        Assert.Equal(["student-1-connection"], sentNotifications[0].Connections);
        Assert.Equal(["student-2-connection"], sentNotifications[1].Connections);
        Assert.Equal(["instructor-1-connection"], sentNotifications[2].Connections);
        Assert.All(sentNotifications, sent =>
        {
            Assert.Equal("Session Started", sent.Notification.Title);
            Assert.Equal("Session", sent.Notification.Category);
            Assert.NotNull(sent.Notification.Metadata);
        });
    }

    [Fact]
    public async Task NotifySessionEndedAsync_SendsReceiveNotificationToStudentsAndInstructorOnly()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var studentIds = new[] { "student-1", "student-2" };
        const string instructorId = "instructor-1";
        var sentNotifications = new List<(IReadOnlyList<string> Connections, NotificationDto Notification)>();
        var service = CreateNotificationService(sessionId, sentNotifications);

        // Act
        await service.NotifySessionEndedAsync(sessionId, studentIds, instructorId);

        // Assert
        Assert.Equal(["student-1-connection"], sentNotifications[0].Connections);
        Assert.Equal(["student-2-connection"], sentNotifications[1].Connections);
        Assert.Equal(["instructor-1-connection"], sentNotifications[2].Connections);
        Assert.All(sentNotifications, sent =>
        {
            Assert.Equal("Session Ended", sent.Notification.Title);
            Assert.Equal("Session", sent.Notification.Category);
            Assert.NotNull(sent.Notification.Metadata);
        });
    }

    [Fact]
    public async Task NotifyStudentCheckedInAsync_StillHonorsInstructorRealtimePreference()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var sentNotifications = new List<(IReadOnlyList<string> Connections, NotificationDto Notification)>();
        var service = CreateNotificationService(
            sessionId,
            sentNotifications,
            instructorRealtimePreference: false);

        // Act
        await service.NotifyStudentCheckedInAsync("student-1", "instructor-1", sessionId, "Present");

        // Assert
        var sent = Assert.Single(sentNotifications);
        Assert.Equal(["student-1-connection"], sent.Connections);
        Assert.Equal("Attendance Recorded", sent.Notification.Title);
    }

    private static NotificationService CreateNotificationService(
        Guid sessionId,
        List<(IReadOnlyList<string> Connections, NotificationDto Notification)> sentNotifications,
        bool instructorRealtimePreference = true)
    {
        var hubContext = new Mock<IHubContext<NotificationHub>>();
        var clients = new Mock<IHubClients>();
        var clientProxy = new Mock<IClientProxy>();
        var connectionManager = new Mock<IUserConnectionManager>();
        var preferenceService = new Mock<INotificationPreferenceService>();
        var qrCodeRepository = new Mock<IQrCodeRepository>();
        var sessionRepository = new Mock<ISessionRepository>();
        var studentRepository = new Mock<IStudentRepository>();
        var logger = new Mock<ILogger<NotificationService>>();

        hubContext.SetupGet(context => context.Clients).Returns(clients.Object);
        clients
            .Setup(client => client.Clients(It.IsAny<IReadOnlyList<string>>()))
            .Callback<IReadOnlyList<string>>(connections => sentNotifications.Add((connections, null!)))
            .Returns(clientProxy.Object);
        clientProxy
            .Setup(proxy => proxy.SendCoreAsync(
                "ReceiveNotification",
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, object?[], CancellationToken>((_, args, _) =>
            {
                var index = sentNotifications.Count - 1;
                sentNotifications[index] = (
                    sentNotifications[index].Connections,
                    Assert.IsType<NotificationDto>(args[0]));
            })
            .Returns(Task.CompletedTask);

        foreach (var userId in new[] { "student-1", "student-2", "instructor-1", "admin-1", "instructor-2" })
        {
            connectionManager.Setup(manager => manager.IsOnlineAsync(userId)).ReturnsAsync(true);
            connectionManager.Setup(manager => manager.GetConnectionsAsync(userId)).ReturnsAsync([$"{userId}-connection"]);
        }

        preferenceService
            .Setup(service => service.GetRealtimeCheckInAsync("instructor-1"))
            .ReturnsAsync(instructorRealtimePreference);

        sessionRepository
            .Setup(repository => repository.GetSessionByIdAsync(sessionId))
            .ReturnsAsync(new Session
            {
                Id = sessionId,
                Schedule = new Schedules
                {
                    Subject = new Subject { Name = "Algorithms" }
                }
            });

        studentRepository
            .Setup(repository => repository.GetStudentByUserIdAsync("student-1"))
            .ReturnsAsync(new Student
            {
                Firstname = "Ada",
                Lastname = "Lovelace",
                UserId = "student-1",
                Usn = Student.CreatePendingUsn()
            });

        return new NotificationService(
            hubContext.Object,
            connectionManager.Object,
            preferenceService.Object,
            qrCodeRepository.Object,
            sessionRepository.Object,
            studentRepository.Object,
            logger.Object);
    }
}
