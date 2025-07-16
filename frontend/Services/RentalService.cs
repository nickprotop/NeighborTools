using System.Net.Http.Json;
using System.Text.Json;
using frontend.Models;

namespace frontend.Services;

public interface IRentalService
{
    Task<ApiResponse<List<Rental>>> GetRentalsAsync(string? status = null, int? page = null, int? pageSize = null);
    Task<ApiResponse<Rental>> GetRentalAsync(string id);
    Task<ApiResponse<Rental>> GetRentalByIdAsync(Guid id);
    Task<ApiResponse<Rental>> CreateRentalAsync(CreateRentalRequest request);
    Task<ApiResponse> ApproveRentalAsync(string id, RentalApprovalRequest request);
    Task<ApiResponse> RejectRentalAsync(string id, RentalApprovalRequest request);
    Task<ApiResponse<List<Rental>>> GetMyRentalsAsync(string? status = null);
    Task<ApiResponse<List<Rental>>> GetMyToolRentalsAsync(string? status = null);
    Task<ApiResponse> ConfirmPickupAsync(Guid rentalId, string? notes = null);
    Task<ApiResponse> ConfirmReturnAsync(Guid rentalId, string? notes = null, string? conditionNotes = null);
    Task<ApiResponse> ExtendRentalAsync(Guid rentalId, DateTime newEndDate, string? notes = null);
    Task<ApiResponse<List<Rental>>> GetOverdueRentalsAsync();
}

public class RentalService : IRentalService
{
    private readonly HttpClient _httpClient;

    public RentalService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ApiResponse<List<Rental>>> GetRentalsAsync(string? status = null, int? page = null, int? pageSize = null)
    {
        try
        {
            var queryParams = new List<string>();
            if (!string.IsNullOrEmpty(status)) queryParams.Add($"status={Uri.EscapeDataString(status)}");
            if (page.HasValue) queryParams.Add($"page={page}");
            if (pageSize.HasValue) queryParams.Add($"pageSize={pageSize}");
            
            var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
            var response = await _httpClient.GetAsync($"/api/rentals{queryString}");
            var content = await response.Content.ReadAsStringAsync();
            
            var result = JsonSerializer.Deserialize<ApiResponse<List<Rental>>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new ApiResponse<List<Rental>> { Success = false, Message = "Invalid response" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<Rental>> 
            { 
                Success = false, 
                Message = $"Failed to retrieve rentals: {ex.Message}" 
            };
        }
    }

    public async Task<ApiResponse<Rental>> GetRentalAsync(string id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/rentals/{id}");
            var content = await response.Content.ReadAsStringAsync();
            
            var result = JsonSerializer.Deserialize<ApiResponse<Rental>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new ApiResponse<Rental> { Success = false, Message = "Invalid response" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<Rental> 
            { 
                Success = false, 
                Message = $"Failed to retrieve rental: {ex.Message}" 
            };
        }
    }

    public async Task<ApiResponse<Rental>> CreateRentalAsync(CreateRentalRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/rentals", request);
            var content = await response.Content.ReadAsStringAsync();
            
            var result = JsonSerializer.Deserialize<ApiResponse<Rental>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new ApiResponse<Rental> { Success = false, Message = "Invalid response" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<Rental> 
            { 
                Success = false, 
                Message = $"Failed to create rental: {ex.Message}" 
            };
        }
    }

    public async Task<ApiResponse> ApproveRentalAsync(string id, RentalApprovalRequest request)
    {
        try
        {
            var response = await _httpClient.PatchAsJsonAsync($"/api/rentals/{id}/approve", request);
            var content = await response.Content.ReadAsStringAsync();
            
            var result = JsonSerializer.Deserialize<ApiResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new ApiResponse { Success = false, Message = "Invalid response" };
        }
        catch (Exception ex)
        {
            return new ApiResponse 
            { 
                Success = false, 
                Message = $"Failed to approve rental: {ex.Message}" 
            };
        }
    }

    public async Task<ApiResponse> RejectRentalAsync(string id, RentalApprovalRequest request)
    {
        try
        {
            var response = await _httpClient.PatchAsJsonAsync($"/api/rentals/{id}/reject", request);
            var content = await response.Content.ReadAsStringAsync();
            
            var result = JsonSerializer.Deserialize<ApiResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new ApiResponse { Success = false, Message = "Invalid response" };
        }
        catch (Exception ex)
        {
            return new ApiResponse 
            { 
                Success = false, 
                Message = $"Failed to reject rental: {ex.Message}" 
            };
        }
    }

    public async Task<ApiResponse<List<Rental>>> GetMyRentalsAsync(string? status = null)
    {
        try
        {
            var queryString = !string.IsNullOrEmpty(status) ? $"?status={Uri.EscapeDataString(status)}&type=renter" : "?type=renter";
            var response = await _httpClient.GetAsync($"/api/rentals{queryString}");
            var content = await response.Content.ReadAsStringAsync();
            
            var result = JsonSerializer.Deserialize<ApiResponse<List<Rental>>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new ApiResponse<List<Rental>> { Success = false, Message = "Invalid response" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<Rental>> 
            { 
                Success = false, 
                Message = $"Failed to retrieve my rentals: {ex.Message}" 
            };
        }
    }

    public async Task<ApiResponse<List<Rental>>> GetMyToolRentalsAsync(string? status = null)
    {
        try
        {
            var queryString = !string.IsNullOrEmpty(status) ? $"?status={Uri.EscapeDataString(status)}&type=owner" : "?type=owner";
            var response = await _httpClient.GetAsync($"/api/rentals{queryString}");
            var content = await response.Content.ReadAsStringAsync();
            
            var result = JsonSerializer.Deserialize<ApiResponse<List<Rental>>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new ApiResponse<List<Rental>> { Success = false, Message = "Invalid response" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<Rental>> 
            { 
                Success = false, 
                Message = $"Failed to retrieve my tool rentals: {ex.Message}" 
            };
        }
    }

    public async Task<ApiResponse<Rental>> GetRentalByIdAsync(Guid id)
    {
        return await GetRentalAsync(id.ToString());
    }

    public async Task<ApiResponse> ConfirmPickupAsync(Guid rentalId, string? notes = null)
    {
        try
        {
            var response = await _httpClient.PatchAsJsonAsync($"/api/rentals/{rentalId}/pickup", notes);
            var content = await response.Content.ReadAsStringAsync();
            
            var result = JsonSerializer.Deserialize<ApiResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new ApiResponse { Success = false, Message = "Invalid response" };
        }
        catch (Exception ex)
        {
            return new ApiResponse 
            { 
                Success = false, 
                Message = $"Failed to confirm pickup: {ex.Message}" 
            };
        }
    }

    public async Task<ApiResponse> ConfirmReturnAsync(Guid rentalId, string? notes = null, string? conditionNotes = null)
    {
        try
        {
            var requestData = new { Notes = notes, ConditionNotes = conditionNotes };
            var response = await _httpClient.PatchAsJsonAsync($"/api/rentals/{rentalId}/return", requestData);
            var content = await response.Content.ReadAsStringAsync();
            
            var result = JsonSerializer.Deserialize<ApiResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new ApiResponse { Success = false, Message = "Invalid response" };
        }
        catch (Exception ex)
        {
            return new ApiResponse 
            { 
                Success = false, 
                Message = $"Failed to confirm return: {ex.Message}" 
            };
        }
    }

    public async Task<ApiResponse> ExtendRentalAsync(Guid rentalId, DateTime newEndDate, string? notes = null)
    {
        try
        {
            var request = new { NewEndDate = newEndDate, Notes = notes };
            var response = await _httpClient.PatchAsJsonAsync($"/api/rentals/{rentalId}/extend", request);
            var content = await response.Content.ReadAsStringAsync();
            
            var result = JsonSerializer.Deserialize<ApiResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new ApiResponse { Success = false, Message = "Invalid response" };
        }
        catch (Exception ex)
        {
            return new ApiResponse 
            { 
                Success = false, 
                Message = $"Failed to extend rental: {ex.Message}" 
            };
        }
    }

    public async Task<ApiResponse<List<Rental>>> GetOverdueRentalsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/rentals/overdue");
            var content = await response.Content.ReadAsStringAsync();
            
            var result = JsonSerializer.Deserialize<ApiResponse<List<Rental>>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new ApiResponse<List<Rental>> { Success = false, Message = "Invalid response" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<Rental>> 
            { 
                Success = false, 
                Message = $"Failed to retrieve overdue rentals: {ex.Message}" 
            };
        }
    }
}