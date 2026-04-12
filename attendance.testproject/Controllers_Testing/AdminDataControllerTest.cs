using attendance_monitoring.Controllers;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response.AdminData;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace attendance.testproject.Controllers_Testing;

public class AdminDataControllerTest
{
    private readonly Mock<IAdminDataService> _service = new();
    private readonly Mock<ILogger<AdminDataController>> _logger = new();

    private AdminDataController CreateController() => new(_service.Object, _logger.Object);

    [Fact]
    public async Task PreviewImport_WithMissingFile_ReturnsBadRequest()
    {
        var controller = CreateController();

        var result = await controller.ImportPreview("users", new FileUploadRequest { File = null! }, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var payload = Assert.IsType<AdminDataPreviewResponseDto>(badRequest.Value);
        Assert.False(payload.Success);
        Assert.False(payload.CanImport);
        Assert.Contains(payload.FileIssues, issue => issue.Code == "file_required");
    }

    [Fact]
    public async Task PreviewImport_WithValidFile_ReturnsOk()
    {
        var controller = CreateController();
        var file = CreateFormFile("users.csv", "username,email\nalpha,alpha@example.com\n");
        var request = new FileUploadRequest { File = file };
        var preview = new AdminDataPreviewResponseDto
        {
            Success = true,
            Entity = "users",
            Format = "csv",
            FileName = "users.csv",
            TotalRows = 1,
            ReadyRows = 1,
            CanImport = true,
        };

        _service.Setup(s => s.PreviewImportAsync("users", file, It.IsAny<IReadOnlyDictionary<string, string?>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(preview);

        var result = await controller.ImportPreview("users", request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<AdminDataPreviewResponseDto>(ok.Value);
        Assert.True(payload.Success);
        Assert.Equal(1, payload.ReadyRows);
    }

    [Fact]
    public async Task Import_WithFailedResponse_ReturnsBadRequest()
    {
        var controller = CreateController();
        var file = CreateFormFile("courses.csv", "name\nCourse Name\n");
        var request = new FileUploadRequest { File = file };
        var response = new AdminDataImportResponseDto
        {
            Success = false,
            Entity = "courses",
            Format = "csv",
            FileName = "courses.csv",
            TotalRows = 1,
            FailedRows = 1,
        };

        _service.Setup(s => s.ImportAsync("courses", file, It.IsAny<System.Security.Claims.ClaimsPrincipal>(), It.IsAny<IReadOnlyDictionary<string, string?>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await controller.Import("courses", request, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var payload = Assert.IsType<AdminDataImportResponseDto>(badRequest.Value);
        Assert.False(payload.Success);
        Assert.Equal(1, payload.FailedRows);
    }

    [Fact]
    public async Task DownloadTemplate_ReturnsFileResult()
    {
        var controller = CreateController();
        var fileDto = new AdminDataFileDto
        {
            Content = [1, 2, 3],
            ContentType = "text/csv",
            FileName = "users-template.csv",
        };

        _service.Setup(s => s.GenerateTemplateAsync("users", "csv", It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileDto);

        var result = await controller.DownloadTemplate("users", "csv", CancellationToken.None);

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/csv", fileResult.ContentType);
        Assert.Equal("users-template.csv", fileResult.FileDownloadName);
        Assert.Equal(fileDto.Content, fileResult.FileContents);
    }

    private static FormFile CreateFormFile(string fileName, string content)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) ? "text/csv" : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }
}
