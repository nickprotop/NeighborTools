using System.Net.Http.Json;
using System.Text.Json;
using frontend.Models;

namespace frontend.Services;

public class MessageService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public MessageService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    // Messages
    public async Task<ApiResponse<MessageDto>> SendMessageAsync(SendMessageRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/messages", request, _jsonOptions);
        return await response.Content.ReadFromJsonAsync<ApiResponse<MessageDto>>(_jsonOptions)
            ?? new ApiResponse<MessageDto> { Success = false, Message = "Failed to deserialize response" };
    }

    public async Task<ApiResponse<MessageDto>> ReplyToMessageAsync(string messageId, ReplyToMessageRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/messages/{messageId}/reply", request, _jsonOptions);
        return await response.Content.ReadFromJsonAsync<ApiResponse<MessageDto>>(_jsonOptions)
            ?? new ApiResponse<MessageDto> { Success = false, Message = "Failed to deserialize response" };
    }

    public async Task<ApiResponse<MessageDto>> GetMessageByIdAsync(string messageId)
    {
        var response = await _httpClient.GetAsync($"/api/messages/{messageId}");
        return await response.Content.ReadFromJsonAsync<ApiResponse<MessageDto>>(_jsonOptions)
            ?? new ApiResponse<MessageDto> { Success = false, Message = "Failed to deserialize response" };
    }

    public async Task<ApiResponse<List<MessageSummaryDto>>> GetMessagesAsync(
        int page = 1, 
        int pageSize = 20,
        bool? isRead = null,
        bool? isArchived = null,
        string? searchTerm = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var queryParams = new List<string>();
        queryParams.Add($"page={page}");
        queryParams.Add($"pageSize={pageSize}");
        
        if (isRead.HasValue) queryParams.Add($"isRead={isRead.Value}");
        if (isArchived.HasValue) queryParams.Add($"isArchived={isArchived.Value}");
        if (!string.IsNullOrEmpty(searchTerm)) queryParams.Add($"searchTerm={Uri.EscapeDataString(searchTerm)}");
        if (fromDate.HasValue) queryParams.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");
        if (toDate.HasValue) queryParams.Add($"toDate={toDate.Value:yyyy-MM-dd}");

        var queryString = string.Join("&", queryParams);
        var response = await _httpClient.GetAsync($"/api/messages?{queryString}");
        return await response.Content.ReadFromJsonAsync<ApiResponse<List<MessageSummaryDto>>>(_jsonOptions)
            ?? new ApiResponse<List<MessageSummaryDto>> { Success = false, Message = "Failed to deserialize response" };
    }

    public async Task<ApiResponse<List<MessageSummaryDto>>> SearchMessagesAsync(
        string searchTerm,
        int page = 1,
        int pageSize = 20,
        bool? isRead = null)
    {
        var queryParams = new List<string>();
        queryParams.Add($"searchTerm={Uri.EscapeDataString(searchTerm)}");
        queryParams.Add($"page={page}");
        queryParams.Add($"pageSize={pageSize}");
        
        if (isRead.HasValue) queryParams.Add($"isRead={isRead.Value}");

        var queryString = string.Join("&", queryParams);
        var response = await _httpClient.GetAsync($"/api/messages/search?{queryString}");
        return await response.Content.ReadFromJsonAsync<ApiResponse<List<MessageSummaryDto>>>(_jsonOptions)
            ?? new ApiResponse<List<MessageSummaryDto>> { Success = false, Message = "Failed to deserialize response" };
    }

    public async Task<ApiResponse<bool>> MarkMessageAsReadAsync(string messageId)
    {
        var response = await _httpClient.PatchAsync($"/api/messages/{messageId}/read", null);
        return await response.Content.ReadFromJsonAsync<ApiResponse<bool>>(_jsonOptions)
            ?? new ApiResponse<bool> { Success = false, Message = "Failed to deserialize response" };
    }

    public async Task<ApiResponse<bool>> ArchiveMessageAsync(string messageId)
    {
        var response = await _httpClient.PatchAsync($"/api/messages/{messageId}/archive", null);
        return await response.Content.ReadFromJsonAsync<ApiResponse<bool>>(_jsonOptions)
            ?? new ApiResponse<bool> { Success = false, Message = "Failed to deserialize response" };
    }

    public async Task<ApiResponse<bool>> DeleteMessageAsync(string messageId)
    {
        var response = await _httpClient.DeleteAsync($"/api/messages/{messageId}");
        return await response.Content.ReadFromJsonAsync<ApiResponse<bool>>(_jsonOptions)
            ?? new ApiResponse<bool> { Success = false, Message = "Failed to deserialize response" };
    }

    public async Task<ApiResponse<int>> GetUnreadMessageCountAsync()
    {
        var response = await _httpClient.GetAsync("/api/messages/unread-count");
        return await response.Content.ReadFromJsonAsync<ApiResponse<int>>(_jsonOptions)
            ?? new ApiResponse<int> { Success = false, Message = "Failed to deserialize response" };
    }

    public async Task<ApiResponse<MessageStatisticsDto>> GetMessageStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var queryParams = new List<string>();
        if (fromDate.HasValue) queryParams.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");
        if (toDate.HasValue) queryParams.Add($"toDate={toDate.Value:yyyy-MM-dd}");

        var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
        var response = await _httpClient.GetAsync($"/api/messages/statistics{queryString}");
        return await response.Content.ReadFromJsonAsync<ApiResponse<MessageStatisticsDto>>(_jsonOptions)
            ?? new ApiResponse<MessageStatisticsDto> { Success = false, Message = "Failed to deserialize response" };
    }

    // Conversations
    public async Task<ApiResponse<ConversationDto>> CreateConversationAsync(CreateConversationRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/conversations", request, _jsonOptions);
        return await response.Content.ReadFromJsonAsync<ApiResponse<ConversationDto>>(_jsonOptions)
            ?? new ApiResponse<ConversationDto> { Success = false, Message = "Failed to deserialize response" };
    }

    public async Task<ApiResponse<List<ConversationDto>>> GetConversationsAsync(
        int page = 1,
        int pageSize = 20,
        bool? isArchived = null,
        string? searchTerm = null)
    {
        var queryParams = new List<string>();
        queryParams.Add($"page={page}");
        queryParams.Add($"pageSize={pageSize}");
        
        if (isArchived.HasValue) queryParams.Add($"isArchived={isArchived.Value}");
        if (!string.IsNullOrEmpty(searchTerm)) queryParams.Add($"searchTerm={Uri.EscapeDataString(searchTerm)}");

        var queryString = string.Join("&", queryParams);
        var response = await _httpClient.GetAsync($"/api/conversations?{queryString}");
        return await response.Content.ReadFromJsonAsync<ApiResponse<List<ConversationDto>>>(_jsonOptions)
            ?? new ApiResponse<List<ConversationDto>> { Success = false, Message = "Failed to deserialize response" };
    }

    public async Task<ApiResponse<ConversationDetailsDto>> GetConversationByIdAsync(string conversationId, int pageSize = 50)
    {
        var response = await _httpClient.GetAsync($"/api/conversations/{conversationId}?pageSize={pageSize}");
        return await response.Content.ReadFromJsonAsync<ApiResponse<ConversationDetailsDto>>(_jsonOptions)
            ?? new ApiResponse<ConversationDetailsDto> { Success = false, Message = "Failed to deserialize response" };
    }

    public async Task<ApiResponse<ConversationDto>> GetConversationBetweenUsersAsync(string user1Id, string user2Id)
    {
        var response = await _httpClient.GetAsync($"/api/conversations/between/{user1Id}/{user2Id}");
        return await response.Content.ReadFromJsonAsync<ApiResponse<ConversationDto>>(_jsonOptions)
            ?? new ApiResponse<ConversationDto> { Success = false, Message = "Failed to deserialize response" };
    }

    public async Task<ApiResponse<bool>> MarkConversationAsReadAsync(string conversationId)
    {
        var response = await _httpClient.PatchAsync($"/api/conversations/{conversationId}/read", null);
        return await response.Content.ReadFromJsonAsync<ApiResponse<bool>>(_jsonOptions)
            ?? new ApiResponse<bool> { Success = false, Message = "Failed to deserialize response" };
    }

    public async Task<ApiResponse<ConversationDto>> UpdateConversationAsync(string conversationId, UpdateConversationRequest request)
    {
        var response = await _httpClient.PutAsJsonAsync($"/api/conversations/{conversationId}", request, _jsonOptions);
        return await response.Content.ReadFromJsonAsync<ApiResponse<ConversationDto>>(_jsonOptions)
            ?? new ApiResponse<ConversationDto> { Success = false, Message = "Failed to deserialize response" };
    }

    public async Task<ApiResponse<bool>> ArchiveConversationAsync(string conversationId)
    {
        var response = await _httpClient.PatchAsync($"/api/conversations/{conversationId}/archive", null);
        return await response.Content.ReadFromJsonAsync<ApiResponse<bool>>(_jsonOptions)
            ?? new ApiResponse<bool> { Success = false, Message = "Failed to deserialize response" };
    }
}

