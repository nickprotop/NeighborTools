using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToolsSharing.Core.Interfaces.GDPR;

namespace ToolsSharing.API.Controllers.GDPR;

[ApiController]
[Route("api/gdpr/export")]
[Authorize]
public class DataExportController : ControllerBase
{
    private readonly IDataExportService _exportService;
    private readonly IDataProcessingLogger _dataLogger;
    private readonly ILogger<DataExportController> _logger;

    public DataExportController(
        IDataExportService exportService,
        IDataProcessingLogger dataLogger,
        ILogger<DataExportController> logger)
    {
        _exportService = exportService;
        _dataLogger = dataLogger;
        _logger = logger;
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> ExportUserData(int userId)
    {
        try
        {
            var userData = await _exportService.GenerateUserDataExportAsync(userId);

            // Log the export activity
            await _dataLogger.LogDataProcessingAsync(new DataProcessingActivity
            {
                UserId = userId,
                ActivityType = "data_export",
                DataCategories = new[] { "personal_data", "financial_data", "usage_data" }.ToList(),
                ProcessingPurpose = "Export user data for GDPR data portability request",
                LegalBasis = Core.Entities.GDPR.LegalBasis.LegalObligation,
                DataSources = new[] { "user_database" }.ToList(),
                RetentionPeriod = "Temporary export file",
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? ""
            });

            return Ok(userData);
        }
        catch (ArgumentException)
        {
            return NotFound(new { Error = "User not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export data for user {UserId}", userId);
            return StatusCode(500, new { Error = "Failed to export user data" });
        }
    }

    [HttpGet("download/{requestId}")]
    public async Task<IActionResult> DownloadExportFile(Guid requestId)
    {
        try
        {
            var filePath = await _exportService.ExportUserDataAsync(requestId);
            
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new { Error = "Export file not found" });
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            var fileName = Path.GetFileName(filePath);

            return File(fileBytes, "application/json", fileName);
        }
        catch (ArgumentException)
        {
            return NotFound(new { Error = "Request not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download export file for request {RequestId}", requestId);
            return StatusCode(500, new { Error = "Failed to download export file" });
        }
    }
}