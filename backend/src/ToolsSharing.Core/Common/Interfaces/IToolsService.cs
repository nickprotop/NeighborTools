using ToolsSharing.Core.Common.Models;
using ToolsSharing.Core.Features.Tools;

namespace ToolsSharing.Core.Common.Interfaces;

public interface IToolsService
{
    Task<ApiResponse<List<ToolDto>>> GetToolsAsync(GetToolsQuery query);
    Task<ApiResponse<ToolDto>> GetToolByIdAsync(GetToolByIdQuery query);
    Task<ApiResponse<ToolDto>> CreateToolAsync(CreateToolCommand command);
    Task<ApiResponse<ToolDto>> UpdateToolAsync(UpdateToolCommand command);
    Task<ApiResponse<bool>> DeleteToolAsync(DeleteToolCommand command);
}