using System.Text.Json;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;

namespace attendance.testproject.Services_Testing;

public class SessionDtoSerializationTest
{
    private static readonly JsonSerializerOptions ApiJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact]
    public void SessionResponseDto_SerializesRowVersion_AsBase64()
    {
        var dto = new SessionResponseDto
        {
            Id = 1,
            ScheduleId = 2,
            RowVersion = [1, 2, 3, 4]
        };

        var json = JsonSerializer.Serialize(dto, ApiJsonOptions);
        var roundTripped = JsonSerializer.Deserialize<SessionResponseDto>(json, ApiJsonOptions);

        Assert.Contains("\"rowVersion\":\"AQIDBA==\"", json);
        Assert.NotNull(roundTripped);
        Assert.NotNull(roundTripped.RowVersion);
        Assert.Equal(dto.RowVersion, roundTripped.RowVersion);
    }

    [Fact]
    public void SessionMutationDtos_RoundTripRowVersion_AsBase64()
    {
        var startJson = JsonSerializer.Serialize(new StartSession { RowVersion = [1, 2, 3, 4] }, ApiJsonOptions);
        var endJson = JsonSerializer.Serialize(new EndSession { RowVersion = [1, 2, 3, 4] }, ApiJsonOptions);
        var cancelJson = JsonSerializer.Serialize(new CancelSession { Reason = "Room issue", RowVersion = [1, 2, 3, 4] }, ApiJsonOptions);
        var roomJson = JsonSerializer.Serialize(new UpdateSessionRoom { ActualRoomId = 5, RowVersion = [1, 2, 3, 4] }, ApiJsonOptions);

        Assert.Contains("\"rowVersion\":\"AQIDBA==\"", startJson);
        Assert.Equal([1, 2, 3, 4], JsonSerializer.Deserialize<StartSession>(startJson, ApiJsonOptions)!.RowVersion);
        Assert.Equal([1, 2, 3, 4], JsonSerializer.Deserialize<EndSession>(endJson, ApiJsonOptions)!.RowVersion);
        Assert.Equal([1, 2, 3, 4], JsonSerializer.Deserialize<CancelSession>(cancelJson, ApiJsonOptions)!.RowVersion);
        Assert.Equal([1, 2, 3, 4], JsonSerializer.Deserialize<UpdateSessionRoom>(roomJson, ApiJsonOptions)!.RowVersion);
    }
}
