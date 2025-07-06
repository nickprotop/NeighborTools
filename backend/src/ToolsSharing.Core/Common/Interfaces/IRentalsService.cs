using ToolsSharing.Core.Common.Models;
using ToolsSharing.Core.Features.Rentals;

namespace ToolsSharing.Core.Common.Interfaces;

public interface IRentalsService
{
    Task<ApiResponse<List<RentalDto>>> GetRentalsAsync(GetRentalsQuery query);
    Task<ApiResponse<RentalDto>> GetRentalByIdAsync(GetRentalByIdQuery query);
    Task<ApiResponse<RentalDto>> CreateRentalAsync(CreateRentalCommand command);
    Task<ApiResponse<bool>> ApproveRentalAsync(ApproveRentalCommand command);
    Task<ApiResponse<bool>> RejectRentalAsync(RejectRentalCommand command);
}