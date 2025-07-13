using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using frontend.Models;

namespace frontend.Services;

public interface IDisputeService
{
    Task<ApiResponse<List<Dispute>>> GetDisputesAsync(GetDisputesRequest? request = null);
    Task<ApiResponse<Dispute>> GetDisputeAsync(string disputeId);
    Task<ApiResponse<Dispute>> CreateDisputeAsync(CreateDisputeRequest request);
    Task<ApiResponse<List<DisputeMessage>>> GetDisputeMessagesAsync(string disputeId);
    Task<ApiResponse<DisputeMessage>> AddDisputeMessageAsync(AddDisputeMessageRequest request);
    Task<ApiResponse<bool>> UpdateDisputeStatusAsync(string disputeId, UpdateDisputeStatusRequest request);
    Task<ApiResponse<bool>> EscalateDisputeAsync(string disputeId);
    Task<ApiResponse<bool>> ResolveDisputeAsync(string disputeId, ResolveDisputeRequest request);
    Task<ApiResponse<bool>> CloseDisputeAsync(string disputeId, CloseDisputeRequest request);
    Task<ApiResponse<List<DisputeTimelineEvent>>> GetDisputeTimelineAsync(string disputeId);
    Task<ApiResponse<DisputeStats>> GetDisputeStatsAsync();
}

public class DisputeService : IDisputeService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DisputeService> _logger;

    public DisputeService(HttpClient httpClient, ILogger<DisputeService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ApiResponse<List<Dispute>>> GetDisputesAsync(GetDisputesRequest? request = null)
    {
        try
        {
            var queryString = BuildQueryString(request);
            var response = await _httpClient.GetAsync($"api/disputes{queryString}");

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<Dispute>>>();
                return result ?? ApiResponse<List<Dispute>>.CreateFailure("Failed to deserialize response");
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to get disputes. Status: {StatusCode}, Content: {Content}", 
                response.StatusCode, errorContent);
            
            return ApiResponse<List<Dispute>>.CreateFailure($"Failed to retrieve disputes: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting disputes");
            return ApiResponse<List<Dispute>>.CreateFailure($"Error retrieving disputes: {ex.Message}");
        }
    }

    public async Task<ApiResponse<Dispute>> GetDisputeAsync(string disputeId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/disputes/{disputeId}");

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<Dispute>>();
                return result ?? ApiResponse<Dispute>.CreateFailure("Failed to deserialize response");
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to get dispute {DisputeId}. Status: {StatusCode}, Content: {Content}", 
                disputeId, response.StatusCode, errorContent);
            
            return ApiResponse<Dispute>.CreateFailure($"Failed to retrieve dispute: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dispute {DisputeId}", disputeId);
            return ApiResponse<Dispute>.CreateFailure($"Error retrieving dispute: {ex.Message}");
        }
    }

    public async Task<ApiResponse<Dispute>> CreateDisputeAsync(CreateDisputeRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/disputes", request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<Dispute>>();
                return result ?? ApiResponse<Dispute>.CreateFailure("Failed to deserialize response");
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to create dispute. Status: {StatusCode}, Content: {Content}", 
                response.StatusCode, errorContent);
            
            return ApiResponse<Dispute>.CreateFailure($"Failed to create dispute: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating dispute");
            return ApiResponse<Dispute>.CreateFailure($"Error creating dispute: {ex.Message}");
        }
    }

    public async Task<ApiResponse<List<DisputeMessage>>> GetDisputeMessagesAsync(string disputeId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/disputes/{disputeId}/messages");

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<DisputeMessage>>>();
                return result ?? ApiResponse<List<DisputeMessage>>.CreateFailure("Failed to deserialize response");
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to get dispute messages for {DisputeId}. Status: {StatusCode}, Content: {Content}", 
                disputeId, response.StatusCode, errorContent);
            
            return ApiResponse<List<DisputeMessage>>.CreateFailure($"Failed to retrieve messages: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dispute messages for {DisputeId}", disputeId);
            return ApiResponse<List<DisputeMessage>>.CreateFailure($"Error retrieving messages: {ex.Message}");
        }
    }

    public async Task<ApiResponse<DisputeMessage>> AddDisputeMessageAsync(AddDisputeMessageRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/disputes/{request.DisputeId}/messages", request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<DisputeMessage>>();
                return result ?? ApiResponse<DisputeMessage>.CreateFailure("Failed to deserialize response");
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to add dispute message. Status: {StatusCode}, Content: {Content}", 
                response.StatusCode, errorContent);
            
            return ApiResponse<DisputeMessage>.CreateFailure($"Failed to add message: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding dispute message");
            return ApiResponse<DisputeMessage>.CreateFailure($"Error adding message: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> UpdateDisputeStatusAsync(string disputeId, UpdateDisputeStatusRequest request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/disputes/{disputeId}/status", request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
                return result ?? ApiResponse<bool>.CreateFailure("Failed to deserialize response");
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to update dispute status. Status: {StatusCode}, Content: {Content}", 
                response.StatusCode, errorContent);
            
            return ApiResponse<bool>.CreateFailure($"Failed to update status: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating dispute status");
            return ApiResponse<bool>.CreateFailure($"Error updating status: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> EscalateDisputeAsync(string disputeId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"api/disputes/{disputeId}/escalate", null);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
                return result ?? ApiResponse<bool>.CreateFailure("Failed to deserialize response");
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to escalate dispute. Status: {StatusCode}, Content: {Content}", 
                response.StatusCode, errorContent);
            
            return ApiResponse<bool>.CreateFailure($"Failed to escalate dispute: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error escalating dispute");
            return ApiResponse<bool>.CreateFailure($"Error escalating dispute: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> ResolveDisputeAsync(string disputeId, ResolveDisputeRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/disputes/{disputeId}/resolve", request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
                return result ?? ApiResponse<bool>.CreateFailure("Failed to deserialize response");
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to resolve dispute. Status: {StatusCode}, Content: {Content}", 
                response.StatusCode, errorContent);
            
            return ApiResponse<bool>.CreateFailure($"Failed to resolve dispute: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving dispute");
            return ApiResponse<bool>.CreateFailure($"Error resolving dispute: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> CloseDisputeAsync(string disputeId, CloseDisputeRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/disputes/{disputeId}/close", request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
                return result ?? ApiResponse<bool>.CreateFailure("Failed to deserialize response");
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to close dispute. Status: {StatusCode}, Content: {Content}", 
                response.StatusCode, errorContent);
            
            return ApiResponse<bool>.CreateFailure($"Failed to close dispute: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing dispute");
            return ApiResponse<bool>.CreateFailure($"Error closing dispute: {ex.Message}");
        }
    }

    public async Task<ApiResponse<List<DisputeTimelineEvent>>> GetDisputeTimelineAsync(string disputeId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/disputes/{disputeId}/timeline");

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<DisputeTimelineEvent>>>();
                return result ?? ApiResponse<List<DisputeTimelineEvent>>.CreateFailure("Failed to deserialize response");
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to get dispute timeline for {DisputeId}. Status: {StatusCode}, Content: {Content}", 
                disputeId, response.StatusCode, errorContent);
            
            return ApiResponse<List<DisputeTimelineEvent>>.CreateFailure($"Failed to retrieve timeline: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dispute timeline for {DisputeId}", disputeId);
            return ApiResponse<List<DisputeTimelineEvent>>.CreateFailure($"Error retrieving timeline: {ex.Message}");
        }
    }

    public async Task<ApiResponse<DisputeStats>> GetDisputeStatsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/disputes/stats");

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<DisputeStats>>();
                return result ?? ApiResponse<DisputeStats>.CreateFailure("Failed to deserialize response");
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to get dispute stats. Status: {StatusCode}, Content: {Content}", 
                response.StatusCode, errorContent);
            
            return ApiResponse<DisputeStats>.CreateFailure($"Failed to retrieve stats: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dispute stats");
            return ApiResponse<DisputeStats>.CreateFailure($"Error retrieving stats: {ex.Message}");
        }
    }

    private static string BuildQueryString(GetDisputesRequest? request)
    {
        if (request == null) return "";

        var parameters = new List<string>();

        if (request.Status.HasValue)
            parameters.Add($"status={request.Status}");
        
        if (request.Type.HasValue)
            parameters.Add($"type={request.Type}");
        
        if (request.StartDate.HasValue)
            parameters.Add($"startDate={request.StartDate:yyyy-MM-dd}");
        
        if (request.EndDate.HasValue)
            parameters.Add($"endDate={request.EndDate:yyyy-MM-dd}");
        
        if (request.PageNumber > 0)
            parameters.Add($"pageNumber={request.PageNumber}");
        
        if (request.PageSize > 0)
            parameters.Add($"pageSize={request.PageSize}");
        
        if (!string.IsNullOrEmpty(request.SortBy))
            parameters.Add($"sortBy={request.SortBy}");
        
        parameters.Add($"sortDescending={request.SortDescending}");

        return parameters.Count > 0 ? "?" + string.Join("&", parameters) : "";
    }
}

// Request DTOs for frontend service
public class GetDisputesRequest
{
    public DisputeStatus? Status { get; set; }
    public DisputeType? Type { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
}

public class UpdateDisputeStatusRequest
{
    public DisputeStatus Status { get; set; }
    public string? Reason { get; set; }
    public string? Notes { get; set; }
}

public class ResolveDisputeRequest
{
    public DisputeResolution Resolution { get; set; }
    public string ResolutionNotes { get; set; } = string.Empty;
    public decimal? RefundAmount { get; set; }
    public bool NotifyParties { get; set; } = true;
}

public class CloseDisputeRequest
{
    public string Reason { get; set; } = string.Empty;
    public bool NotifyParties { get; set; } = true;
}

public class DisputeStats
{
    public int TotalDisputes { get; set; }
    public int OpenDisputes { get; set; }
    public int InProgressDisputes { get; set; }
    public int ResolvedDisputes { get; set; }
    public int EscalatedDisputes { get; set; }
    public decimal AverageResolutionTime { get; set; }
    public decimal RefundedAmount { get; set; }
    public Dictionary<string, int> DisputesByCategory { get; set; } = new();
    public Dictionary<string, int> DisputesByMonth { get; set; } = new();
}