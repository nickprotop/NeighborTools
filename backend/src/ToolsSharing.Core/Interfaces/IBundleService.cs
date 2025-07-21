using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ToolsSharing.Core.Common.Models;
using ToolsSharing.Core.DTOs.Bundle;

namespace ToolsSharing.Core.Interfaces
{
    public interface IBundleService
    {
        // Bundle CRUD operations
        Task<ApiResponse<BundleDto>> CreateBundleAsync(CreateBundleRequest request, string userId);
        Task<ApiResponse<BundleDto>> UpdateBundleAsync(Guid bundleId, CreateBundleRequest request, string userId);
        Task<ApiResponse<bool>> DeleteBundleAsync(Guid bundleId, string userId);
        Task<ApiResponse<BundleDto?>> GetBundleByIdAsync(Guid bundleId);
        Task<ApiResponse<PagedResult<BundleDto>>> GetBundlesAsync(int page = 1, int pageSize = 20, string? category = null, string? searchTerm = null, bool featuredOnly = false);
        Task<ApiResponse<PagedResult<BundleDto>>> GetUserBundlesAsync(string userId, int page = 1, int pageSize = 20);
        
        // Featured bundles
        Task<ApiResponse<List<BundleDto>>> GetFeaturedBundlesAsync(int count = 6);
        Task<ApiResponse<bool>> SetFeaturedStatusAsync(Guid bundleId, bool isFeatured, string adminUserId);
        
        // Bundle availability and pricing
        Task<ApiResponse<BundleAvailabilityResponse>> CheckBundleAvailabilityAsync(BundleAvailabilityRequest request);
        Task<ApiResponse<BundleCostCalculationResponse>> CalculateBundleCostAsync(Guid bundleId, DateTime startDate, DateTime endDate);
        
        // Bundle rentals
        Task<ApiResponse<BundleRentalDto>> CreateBundleRentalAsync(CreateBundleRentalRequest request, string userId);
        Task<ApiResponse<BundleRentalDto?>> GetBundleRentalByIdAsync(Guid rentalId);
        Task<ApiResponse<PagedResult<BundleRentalDto>>> GetUserBundleRentalsAsync(string userId, int page = 1, int pageSize = 20);
        Task<ApiResponse<bool>> ApproveBundleRentalAsync(Guid rentalId, string userId);
        Task<ApiResponse<bool>> RejectBundleRentalAsync(Guid rentalId, string userId, string reason);
        Task<ApiResponse<bool>> CancelBundleRentalAsync(Guid rentalId, string userId);
        
        // Bundle statistics
        Task<ApiResponse<bool>> IncrementViewCountAsync(Guid bundleId);
        Task<ApiResponse<Dictionary<string, int>>> GetBundleCategoryCountsAsync();
    }
}