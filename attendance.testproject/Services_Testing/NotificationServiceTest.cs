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
        Assert.Equal(3, sentNotifications.Count);
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
        Assert.Equal(3, sentNotifications.Count);
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

    [Fact]
    public async Task BroadcastDeviceStatusUpdateAsync_SendsDeviceStatusUpdateToAdminGroup()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var lastSeenAt = DateTime.UtcNow;
        var hubContext = new Mock<IHubContext<NotificationHub>>();
        var clients = new Mock<IHubClients>();
        var groupProxy = new Mock<IClientProxy>();
        var service = CreateBroadcastService(hubContext, clients, groupProxy);

        // Act
        await service.BroadcastDeviceStatusUpdateAsync(deviceId, lastSeenAt);

        // Assert
        clients.Verify(client => client.Group("role:Admin"), Times.Once);
        groupProxy.Verify(
            proxy => proxy.SendCoreAsync(
                "DeviceStatusUpdate",
                It.Is<object?[]>(args =>
                    args.Length == 1 &&
                    args[0] != null),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task BroadcastDeviceStatusUpdateAsync_IncludesDeviceIdAndLastSeenAtInPayload()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var lastSeenAt = new DateTime(2026, 4, 28, 1, 0, 0, DateTimeKind.Utc);
        var hubContext = new Mock<IHubContext<NotificationHub>>();
        var clients = new Mock<IHubClients>();
        var groupProxy = new Mock<IClientProxy>();

        hubContext.SetupGet(context => context.Clients).Returns(clients.Object);
        clients.Setup(client => client.Group("role:Admin")).Returns(groupProxy.Object);
        groupProxy
            .Setup(proxy => proxy.SendCoreAsync(
                It.IsAny<string>(),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, object?[], CancellationToken>((_, args, _) =>
            {
                Assert.Single(args);
                var payload = args[0];
                Assert.NotNull(payload);
                var payloadJson = System.Text.Json.JsonSerializer.Serialize(payload);
                Assert.Contains(deviceId.ToString(), payloadJson);
                Assert.Contains(lastSeenAt.ToString("O"), payloadJson);
                return Task.CompletedTask;
            });

        var service = new NotificationService(
            hubContext.Object,
            Mock.Of<IUserConnectionManager>(),
            Mock.Of<INotificationPreferenceService>(),
            Mock.Of<IQrCodeRepository>(),
            Mock.Of<ISessionRepository>(),
            Mock.Of<IStudentRepository>(),
            Mock.Of<ILogger<NotificationService>>());

        // Act
        await service.BroadcastDeviceStatusUpdateAsync(deviceId, lastSeenAt);
    }

    [Fact]
    public async Task BroadcastDeviceStatusUpdateAsync_DoesNotThrow_WhenHubThrows()
    {
        // Arrange
        var hubContext = new Mock<IHubContext<NotificationHub>>();
        var clients = new Mock<IHubClients>();
        var groupProxy = new Mock<IClientProxy>();
        groupProxy
            .Setup(proxy => proxy.SendCoreAsync(
                It.IsAny<string>(),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SignalR connection lost"));
        var service = CreateBroadcastService(hubContext, clients, groupProxy);

        // Act & Assert (should not throw)
        await service.BroadcastDeviceStatusUpdateAsync(Guid.NewGuid(), DateTime.UtcNow);
    }

    [Fact]
    public async Task BroadcastDeviceStatusUpdateAsync_DoesNotUseConnectionManager()
    {
        // Arrange
        var hubContext = new Mock<IHubContext<NotificationHub>>();
        var clients = new Mock<IHubClients>();
        var groupProxy = new Mock<IClientProxy>();
        var connectionManager = new Mock<IUserConnectionManager>(MockBehavior.Strict);
        var service = new NotificationService(
            hubContext.Object,
            connectionManager.Object,
            Mock.Of<INotificationPreferenceService>(),
            Mock.Of<IQrCodeRepository>(),
            Mock.Of<ISessionRepository>(),
            Mock.Of<IStudentRepository>(),
            Mock.Of<ILogger<NotificationService>>());

        hubContext.SetupGet(context => context.Clients).Returns(clients.Object);
        clients.Setup(client => client.Group("role:Admin")).Returns(groupProxy.Object);
        groupProxy
            .Setup(proxy => proxy.SendCoreAsync(
                It.IsAny<string>(),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await service.BroadcastDeviceStatusUpdateAsync(Guid.NewGuid(), DateTime.UtcNow);

        // Assert - Strict mock would throw if any connection manager method was called
        connectionManager.Verify(
            manager => manager.IsOnlineAsync(It.IsAny<string>()),
            Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task SendToUserAsync_InvalidUserId_DoesNotQueryConnectionManager(string? userId)
    {
        // Arrange
        var hubContext = new Mock<IHubContext<NotificationHub>>();
        var connectionManager = new Mock<IUserConnectionManager>(MockBehavior.Strict);
        var service = new NotificationService(
            hubContext.Object,
            connectionManager.Object,
            Mock.Of<INotificationPreferenceService>(),
            Mock.Of<IQrCodeRepository>(),
            Mock.Of<ISessionRepository>(),
            Mock.Of<IStudentRepository>(),
            Mock.Of<ILogger<NotificationService>>());

        var notification = new NotificationDto
        {
            Title = "Session Started",
            Message = "Test notification",
            Type = "Info",
            Category = "Session"
        };

        // Act
        await service.SendToUserAsync(userId!, notification);

        // Assert
        connectionManager.Verify(
            manager => manager.IsOnlineAsync(It.IsAny<string>()),
            Times.Never);
    }

    private static NotificationService CreateBroadcastService(
        Mock<IHubContext<NotificationHub>> hubContext,
        Mock<IHubClients> clients,
        Mock<IClientProxy> groupProxy)
    {
        hubContext.SetupGet(context => context.Clients).Returns(clients.Object);
        clients.Setup(client => client.Group("role:Admin")).Returns(groupProxy.Object);
        groupProxy
            .Setup(proxy => proxy.SendCoreAsync(
                "DeviceStatusUpdate",
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return new NotificationService(
            hubContext.Object,
            Mock.Of<IUserConnectionManager>(),
            Mock.Of<INotificationPreferenceService>(),
            Mock.Of<IQrCodeRepository>(),
            Mock.Of<ISessionRepository>(),
            Mock.Of<IStudentRepository>(),
            Mock.Of<ILogger<NotificationService>>());
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
