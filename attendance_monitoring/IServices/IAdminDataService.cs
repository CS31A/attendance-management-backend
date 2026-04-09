using System.Security.Claims;
using attendance_monitoring.Models.DTO.Response.AdminData;
using Microsoft.AspNetCore.Http;

namespace attendance_monitoring.IServices;

public interface IAdminDataService
{
    Task<AdminDataPreviewResponseDto> PreviewImportAsync(string entity, IFormFile file, IReadOnlyDictionary<string, string?> filters, CancellationToken cancellationToken = default);
    Task<AdminDataImportResponseDto> ImportAsync(string entity, IFormFile file, ClaimsPrincipal user, IReadOnlyDictionary<string, string?> filters, CancellationToken cancellationToken = default);
    Task<AdminDataFileDto> GenerateTemplateAsync(string entity, string format, CancellationToken cancellationToken = default);
    Task<AdminDataFileDto> ExportAsync(string entity, string format, IReadOnlyDictionary<string, string?> filters, CancellationToken cancellationToken = default);
}
