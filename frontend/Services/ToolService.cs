using System.Net.Http.Json;
using System.Text.Json;
using frontend.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace frontend.Services;

public interface IToolService
{
    Task<ApiResponse<List<Tool>>> GetToolsAsync();
    Task<ApiResponse<List<Tool>>> GetMyToolsAsync();
    Task<ApiResponse<Tool>> GetToolAsync(string id);
    Task<ApiResponse<Tool>> CreateToolAsync(CreateToolRequest request);
    Task<ApiResponse<Tool>> UpdateToolAsync(string id, UpdateToolRequest request);
    Task<ApiResponse> DeleteToolAsync(string id);
    Task<ApiResponse<List<Tool>>> SearchToolsAsync(string query);
    Task<ApiResponse<ToolRentalPreferences>> GetToolRentalPreferencesAsync(string toolId);
    Task<ApiResponse<List<string>>> UploadImagesAsync(List<IBrowserFile> files);
}

public class ToolService : IToolService
{
    private readonly HttpClient _httpClient;

    public ToolService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ApiResponse<List<Tool>>> GetToolsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/tools");
            var content = await response.Content.ReadAsStringAsync();
            
            var result = JsonSerializer.Deserialize<ApiResponse<List<Tool>>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new ApiResponse<List<Tool>> { Success = false, Message = "Invalid response" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<Tool>> 
            { 
                Success = false, 
                Message = $"Failed to retrieve tools: {ex.Message}" 
            };
        }
    }

    public async Task<ApiResponse<List<Tool>>> GetMyToolsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/tools/my-tools");
            var content = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return new ApiResponse<List<Tool>> 
                    { 
                        Success = false, 
                        Message = "You must be logged in to view your tools. Please log in and try again." 
                    };
                }
                
                return new ApiResponse<List<Tool>> 
                { 
                    Success = false, 
                    Message = $"HTTP {response.StatusCode}: {content}" 
                };
            }
            
            var result = JsonSerializer.Deserialize<ApiResponse<List<Tool>>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new ApiResponse<List<Tool>> { Success = false, Message = "Invalid response" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<Tool>> 
            { 
                Success = false, 
                Message = $"Failed to retrieve my tools: {ex.Message}" 
            };
        }
    }

    public async Task<ApiResponse<Tool>> GetToolAsync(string id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/tools/{id}");
            var content = await response.Content.ReadAsStringAsync();
            
            var result = JsonSerializer.Deserialize<ApiResponse<Tool>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new ApiResponse<Tool> { Success = false, Message = "Invalid response" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<Tool> 
            { 
                Success = false, 
                Message = $"Failed to retrieve tool: {ex.Message}" 
            };
        }
    }

    public async Task<ApiResponse<Tool>> CreateToolAsync(CreateToolRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/tools", request);
            var content = await response.Content.ReadAsStringAsync();
            
            var result = JsonSerializer.Deserialize<ApiResponse<Tool>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new ApiResponse<Tool> { Success = false, Message = "Invalid response" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<Tool> 
            { 
                Success = false, 
                Message = $"Failed to create tool: {ex.Message}" 
            };
        }
    }

    public async Task<ApiResponse<Tool>> UpdateToolAsync(string id, UpdateToolRequest request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/tools/{id}", request);
            var content = await response.Content.ReadAsStringAsync();
            
            var result = JsonSerializer.Deserialize<ApiResponse<Tool>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new ApiResponse<Tool> { Success = false, Message = "Invalid response" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<Tool> 
            { 
                Success = false, 
                Message = $"Failed to update tool: {ex.Message}" 
            };
        }
    }

    public async Task<ApiResponse> DeleteToolAsync(string id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/tools/{id}");
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
                Message = $"Failed to delete tool: {ex.Message}" 
            };
        }
    }

    public async Task<ApiResponse<List<Tool>>> SearchToolsAsync(string query)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/tools/search?query={Uri.EscapeDataString(query)}");
            var content = await response.Content.ReadAsStringAsync();
            
            var result = JsonSerializer.Deserialize<ApiResponse<List<Tool>>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new ApiResponse<List<Tool>> { Success = false, Message = "Invalid response" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<Tool>> 
            { 
                Success = false, 
                Message = $"Failed to search tools: {ex.Message}" 
            };
        }
    }

    public async Task<ApiResponse<ToolRentalPreferences>> GetToolRentalPreferencesAsync(string toolId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/tools/{toolId}/rental-preferences");
            var content = await response.Content.ReadAsStringAsync();
            
            var result = JsonSerializer.Deserialize<ApiResponse<ToolRentalPreferences>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new ApiResponse<ToolRentalPreferences> { Success = false, Message = "Invalid response" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<ToolRentalPreferences> 
            { 
                Success = false, 
                Message = $"Failed to retrieve rental preferences: {ex.Message}" 
            };
        }
    }

    public async Task<ApiResponse<List<string>>> UploadImagesAsync(List<IBrowserFile> files)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            
            foreach (var file in files)
            {
                var fileContent = new StreamContent(file.OpenReadStream(5 * 1024 * 1024)); // 5MB limit
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
                content.Add(fileContent, "files", file.Name);
            }

            var response = await _httpClient.PostAsync("/api/tools/upload-images", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            var result = JsonSerializer.Deserialize<ApiResponse<List<string>>>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new ApiResponse<List<string>> { Success = false, Message = "Invalid response" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<string>> 
            { 
                Success = false, 
                Message = $"Failed to upload images: {ex.Message}" 
            };
        }
    }
}