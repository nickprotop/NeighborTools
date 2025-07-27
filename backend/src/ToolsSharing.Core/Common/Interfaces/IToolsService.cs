using ToolsSharing.Core.Common.Models;
using ToolsSharing.Core.Features.Tools;
using ToolsSharing.Core.DTOs.Tools;

namespace ToolsSharing.Core.Common.Interfaces;

public interface IToolsService
{
    Task<ApiResponse<List<DTOs.Tools.ToolDto>>> GetToolsAsync(GetToolsQuery query);
    Task<ApiResponse<PagedResult<DTOs.Tools.ToolDto>>> GetToolsPagedAsync(GetToolsQuery query);
    Task<ApiResponse<List<DTOs.Tools.ToolDto>>> GetUserToolsAsync(GetToolsQuery query, string userId);
    Task<ApiResponse<DTOs.Tools.ToolDto>> GetToolByIdAsync(GetToolByIdQuery query);
    Task<ApiResponse<DTOs.Tools.ToolDto>> CreateToolAsync(CreateToolCommand command);
    Task<ApiResponse<DTOs.Tools.ToolDto>> UpdateToolAsync(UpdateToolCommand command);
    Task<ApiResponse<bool>> DeleteToolAsync(DeleteToolCommand command);
    
    // New feature methods
    Task<ApiResponse<bool>> IncrementViewCountAsync(Guid toolId);
    Task<ApiResponse<PagedResult<ToolReviewDto>>> GetToolReviewsAsync(Guid toolId, int page, int pageSize);
    Task<ApiResponse<ToolReviewDto>> CreateToolReviewAsync(Guid toolId, string userId, CreateToolReviewRequest request);
    Task<ApiResponse<ToolReviewSummaryDto>> GetToolReviewSummaryAsync(Guid toolId);
    Task<ApiResponse<bool>> CanUserReviewToolAsync(Guid toolId, string userId);
    Task<ApiResponse<List<DTOs.Tools.ToolDto>>> GetFeaturedToolsAsync(int count);
    Task<ApiResponse<List<DTOs.Tools.ToolDto>>> GetPopularToolsAsync(int count);
    Task<ApiResponse<List<TagDto>>> GetPopularTagsAsync(int count);
    Task<ApiResponse<PagedResult<DTOs.Tools.ToolDto>>> SearchToolsAsync(SearchToolsQuery query);
    Task<ApiResponse<bool>> RequestApprovalAsync(Guid toolId, string userId, DTOs.Tools.RequestApprovalRequest request);
}