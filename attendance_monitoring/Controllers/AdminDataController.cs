using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response.AdminData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace attendance_monitoring.Controllers;

[Authorize(Policy = "AdminPolicy")]
[ApiController]
[Route("api/admin-data")]
public class AdminDataController(IAdminDataService adminDataService, ILogger<AdminDataController> logger) : ControllerBase
{
    private Dictionary<string, string?> GetImportFilters()
    {
        if (HttpContext?.Request?.HasFormContentType != true)
        {
            return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        }

        return Request.Form.Keys
            .Where(key => !string.Equals(key, "file", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(key => key, key => (string?)Request.Form[key].ToString(), StringComparer.OrdinalIgnoreCase);
    }

    [HttpPost("{entity}/import-preview")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(AdminDataPreviewResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AdminDataPreviewResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminDataPreviewResponseDto>> ImportPreview(string entity, [FromForm] FileUploadRequest request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            return BadRequest(new AdminDataPreviewResponseDto
            {
                Success = false,
                Entity = entity,
                CanImport = false,
                FileIssues = [new AdminDataIssueDto { Code = "file_required", Severity = "error", Message = "A non-empty file is required." }],
            });
        }

        logger.LogInformation("Previewing bulk import for entity {Entity}", entity);
        var filters = GetImportFilters();
        var response = await adminDataService.PreviewImportAsync(entity, request.File, filters, cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }

    [HttpPost("{entity}/import")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(AdminDataImportResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AdminDataImportResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminDataImportResponseDto>> Import(string entity, [FromForm] FileUploadRequest request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            return BadRequest(new AdminDataImportResponseDto
            {
                Success = false,
                Entity = entity,
                FailedRows = 1,
                FileIssues = [new AdminDataIssueDto { Code = "file_required", Severity = "error", Message = "A non-empty file is required." }],
            });
        }

        logger.LogInformation("Committing bulk import for entity {Entity}", entity);
        var filters = GetImportFilters();
        var response = await adminDataService.ImportAsync(entity, request.File, User, filters, cancellationToken).ConfigureAwait(false);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpGet("{entity}/template")]
    public async Task<IActionResult> DownloadTemplate(string entity, [FromQuery] string format = "csv", CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Downloading template for entity {Entity} in format {Format}", entity, format);
        var file = await adminDataService.GenerateTemplateAsync(entity, format, cancellationToken).ConfigureAwait(false);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpGet("{entity}/export")]
    public async Task<IActionResult> Export(string entity, [FromQuery] string format = "csv", CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Exporting data for entity {Entity} in format {Format}", entity, format);
        var filters = Request.Query.ToDictionary(pair => pair.Key, pair => (string?)pair.Value.ToString(), StringComparer.OrdinalIgnoreCase);
        var file = await adminDataService.ExportAsync(entity, format, filters, cancellationToken).ConfigureAwait(false);
        return File(file.Content, file.ContentType, file.FileName);
    }
}
