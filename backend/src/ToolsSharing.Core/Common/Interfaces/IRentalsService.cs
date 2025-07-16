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
    Task<ApiResponse<bool>> MarkRentalPickedUpAsync(MarkRentalPickedUpCommand command);
    Task<ApiResponse<bool>> MarkRentalReturnedAsync(MarkRentalReturnedCommand command);
    Task<ApiResponse<bool>> ExtendRentalAsync(ExtendRentalCommand command);
    Task<ApiResponse<List<RentalDto>>> GetOverdueRentalsAsync(GetOverdueRentalsQuery query);
    Task<ApiResponse<int>> CheckAndUpdateOverdueRentalsAsync();
}