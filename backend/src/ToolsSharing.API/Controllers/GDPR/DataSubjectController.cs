using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToolsSharing.Core.Entities.GDPR;
using ToolsSharing.Core.Interfaces.GDPR;

namespace ToolsSharing.API.Controllers.GDPR;

[ApiController]
[Route("api/gdpr/data-subject")]
[Authorize]
public class DataSubjectController : ControllerBase
{
    private readonly IDataSubjectRightsService _dataSubjectService;
    private readonly IDataProcessingLogger _dataLogger;
    private readonly ILogger<DataSubjectController> _logger;

    public DataSubjectController(
        IDataSubjectRightsService dataSubjectService,
        IDataProcessingLogger dataLogger,
        ILogger<DataSubjectController> logger)
    {
        _dataSubjectService = dataSubjectService;
        _dataLogger = dataLogger;
        _logger = logger;
    }

    [HttpPost("request")]
    public async Task<IActionResult> CreateDataRequest([FromBody] DataRequestDto request)
    {
        try
        {
            var dataRequest = await _dataSubjectService.CreateDataRequestAsync(
                request.UserId, 
                request.RequestType, 
                request.Details);

            // Log the request
            await _dataLogger.LogDataProcessingAsync(new DataProcessingActivity
            {
                UserId = request.UserId,
                ActivityType = "data_subject_request",
                DataCategories = new[] { "request_data" }.ToList(),
                ProcessingPurpose = $"Process GDPR data subject request: {request.RequestType}",
                LegalBasis = LegalBasis.LegalObligation,
                DataSources = new[] { "user_request" }.ToList(),
                RetentionPeriod = "7 years",
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? ""
            });

            return Ok(new { 
                Success = true, 
                RequestId = dataRequest.Id,
                Status = dataRequest.Status.ToString(),
                Message = "Data subject request created successfully" 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create data subject request for user {UserId}", request.UserId);
            return StatusCode(500, new { Error = "Failed to create data subject request" });
        }
    }

    [HttpGet("requests/{userId}")]
    public async Task<IActionResult> GetUserRequests(int userId)
    {
        try
        {
            var requests = await _dataSubjectService.GetUserDataRequestsAsync(userId);
            var response = requests.Select(r => new DataRequestResponseDto
            {
                Id = r.Id,
                RequestType = r.RequestType,
                Status = r.Status,
                RequestDate = r.RequestDate,
                CompletedDate = r.CompletionDate,
                Details = r.RequestDetails ?? string.Empty
            });

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get data requests for user {UserId}", userId);
            return StatusCode(500, new { Error = "Failed to retrieve data requests" });
        }
    }

    [HttpGet("request/{requestId}")]
    public async Task<IActionResult> GetRequest(Guid requestId)
    {
        try
        {
            var request = await _dataSubjectService.GetDataRequestAsync(requestId);
            
            var response = new DataRequestResponseDto
            {
                Id = request.Id,
                RequestType = request.RequestType,
                Status = request.Status,
                RequestDate = request.RequestDate,
                CompletedDate = request.CompletionDate,
                Details = request.RequestDetails ?? string.Empty,
                ResponseData = request.ResponseDetails
            };

            return Ok(response);
        }
        catch (ArgumentException)
        {
            return NotFound(new { Error = "Request not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get data request {RequestId}", requestId);
            return StatusCode(500, new { Error = "Failed to retrieve data request" });
        }
    }

    [HttpPost("validate-erasure/{userId}")]
    public async Task<IActionResult> ValidateErasureRequest(int userId)
    {
        try
        {
            var validation = await _dataSubjectService.ValidateErasureRequestAsync(userId);
            return Ok(validation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate erasure request for user {UserId}", userId);
            return StatusCode(500, new { Error = "Failed to validate erasure request" });
        }
    }

    [HttpPost("process/{requestId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ProcessRequest(Guid requestId)
    {
        try
        {
            await _dataSubjectService.ProcessDataRequestAsync(requestId);
            return Ok(new { Success = true, Message = "Request processed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process data request {RequestId}", requestId);
            return StatusCode(500, new { Error = "Failed to process data request" });
        }
    }
}

// DTOs
public class DataRequestDto
{
    public int UserId { get; set; }
    public DataRequestType RequestType { get; set; }
    public string? Details { get; set; }
}

public class DataRequestResponseDto
{
    public Guid Id { get; set; }
    public DataRequestType RequestType { get; set; }
    public DataRequestStatus Status { get; set; }
    public DateTime RequestDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string Details { get; set; } = string.Empty;
    public string? ResponseData { get; set; }
}