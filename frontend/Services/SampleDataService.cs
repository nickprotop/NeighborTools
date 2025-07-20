using System.Net.Http.Json;
using frontend.Models;

namespace frontend.Services;

public class SampleDataService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SampleDataService> _logger;

    public SampleDataService(HttpClient httpClient, ILogger<SampleDataService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ApiResponse<SampleDataStatusDto>> GetStatusAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/sampledata/status");
            
            if (response.IsSuccessStatusCode)
            {
                var status = await response.Content.ReadFromJsonAsync<SampleDataStatusDto>();
                return new ApiResponse<SampleDataStatusDto>
                {
                    Success = true,
                    Data = status,
                    Message = "Sample data status retrieved successfully"
                };
            }
            
            var errorContent = await response.Content.ReadAsStringAsync();
            return new ApiResponse<SampleDataStatusDto>
            {
                Success = false,
                Message = $"Failed to get sample data status: {response.StatusCode}",
                Errors = new List<string> { errorContent }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sample data status");
            return new ApiResponse<SampleDataStatusDto>
            {
                Success = false,
                Message = "An error occurred while getting sample data status",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ApiResponse<SampleDataStatusDto>> ApplySampleDataAsync(List<string> dataTypes)
    {
        try
        {
            var request = new ApplySampleDataRequest { DataTypes = dataTypes };
            var response = await _httpClient.PostAsJsonAsync("api/sampledata/apply", request);
            
            if (response.IsSuccessStatusCode)
            {
                var status = await response.Content.ReadFromJsonAsync<SampleDataStatusDto>();
                return new ApiResponse<SampleDataStatusDto>
                {
                    Success = true,
                    Data = status,
                    Message = "Sample data applied successfully"
                };
            }
            
            var errorContent = await response.Content.ReadAsStringAsync();
            return new ApiResponse<SampleDataStatusDto>
            {
                Success = false,
                Message = $"Failed to apply sample data: {response.StatusCode}",
                Errors = new List<string> { errorContent }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying sample data");
            return new ApiResponse<SampleDataStatusDto>
            {
                Success = false,
                Message = "An error occurred while applying sample data",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ApiResponse<SampleDataStatusDto>> RemoveSampleDataAsync(List<string> dataTypes)
    {
        try
        {
            var request = new RemoveSampleDataRequest { DataTypes = dataTypes };
            var response = await _httpClient.PostAsJsonAsync("api/sampledata/remove", request);
            
            if (response.IsSuccessStatusCode)
            {
                var status = await response.Content.ReadFromJsonAsync<SampleDataStatusDto>();
                return new ApiResponse<SampleDataStatusDto>
                {
                    Success = true,
                    Data = status,
                    Message = "Sample data removed successfully"
                };
            }
            
            var errorContent = await response.Content.ReadAsStringAsync();
            return new ApiResponse<SampleDataStatusDto>
            {
                Success = false,
                Message = $"Failed to remove sample data: {response.StatusCode}",
                Errors = new List<string> { errorContent }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing sample data");
            return new ApiResponse<SampleDataStatusDto>
            {
                Success = false,
                Message = "An error occurred while removing sample data",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ApiResponse<SampleDataStatusDto>> RemoveAllSampleDataAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync("api/sampledata/remove-all", null);
            
            if (response.IsSuccessStatusCode)
            {
                var status = await response.Content.ReadFromJsonAsync<SampleDataStatusDto>();
                return new ApiResponse<SampleDataStatusDto>
                {
                    Success = true,
                    Data = status,
                    Message = "All sample data removed successfully"
                };
            }
            
            var errorContent = await response.Content.ReadAsStringAsync();
            return new ApiResponse<SampleDataStatusDto>
            {
                Success = false,
                Message = $"Failed to remove all sample data: {response.StatusCode}",
                Errors = new List<string> { errorContent }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing all sample data");
            return new ApiResponse<SampleDataStatusDto>
            {
                Success = false,
                Message = "An error occurred while removing all sample data",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ApiResponse<bool>> CheckSampleDataTypeAsync(string dataType)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/sampledata/check/{dataType}");
            
            if (response.IsSuccessStatusCode)
            {
                var isApplied = await response.Content.ReadFromJsonAsync<bool>();
                return new ApiResponse<bool>
                {
                    Success = true,
                    Data = isApplied,
                    Message = $"Sample data type {dataType} check completed"
                };
            }
            
            var errorContent = await response.Content.ReadAsStringAsync();
            return new ApiResponse<bool>
            {
                Success = false,
                Message = $"Failed to check sample data type: {response.StatusCode}",
                Errors = new List<string> { errorContent }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking sample data type {DataType}", dataType);
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "An error occurred while checking sample data type",
                Errors = new List<string> { ex.Message }
            };
        }
    }
}

// DTOs for sample data management
public class SampleDataStatusDto
{
    public List<SampleDataTypeStatus> DataTypes { get; set; } = new();
    public bool HasAnySampleData { get; set; }
}

public class SampleDataTypeStatus
{
    public string DataType { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsApplied { get; set; }
    public int Count { get; set; }
}

public class ApplySampleDataRequest
{
    public List<string> DataTypes { get; set; } = new();
}

public class RemoveSampleDataRequest
{
    public List<string> DataTypes { get; set; } = new();
}