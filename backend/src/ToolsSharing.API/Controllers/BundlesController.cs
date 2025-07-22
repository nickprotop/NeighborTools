using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ToolsSharing.Core.DTOs.Bundle;
using ToolsSharing.Core.Interfaces;
using ToolsSharing.Core.Common.Models;

namespace ToolsSharing.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BundlesController : ControllerBase
    {
        private readonly IBundleService _bundleService;
        private readonly IFileStorageService _fileStorageService;

        public BundlesController(IBundleService bundleService, IFileStorageService fileStorageService)
        {
            _bundleService = bundleService;
            _fileStorageService = fileStorageService;
        }

        /// <summary>
        /// Get all bundles with filtering and pagination
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetBundles(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? category = null,
            [FromQuery] string? searchTerm = null,
            [FromQuery] bool featuredOnly = false)
        {
            var result = await _bundleService.GetBundlesAsync(page, pageSize, category, searchTerm, featuredOnly);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Get a specific bundle by ID
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetBundle(Guid id)
        {
            var result = await _bundleService.GetBundleByIdAsync(id);
            if (!result.Success)
            {
                return BadRequest(result);
            }

            if (result.Data == null)
            {
                return NotFound();
            }

            // Increment view count synchronously to avoid DbContext disposal issues
            _ = await _bundleService.IncrementViewCountAsync(id);

            return Ok(result);
        }

        /// <summary>
        /// Get featured bundles
        /// </summary>
        [HttpGet("featured")]
        public async Task<IActionResult> GetFeaturedBundles([FromQuery] int count = 6)
        {
            var result = await _bundleService.GetFeaturedBundlesAsync(count);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Get bundle categories with counts
        /// </summary>
        [HttpGet("categories")]
        public async Task<IActionResult> GetBundleCategories()
        {
            var result = await _bundleService.GetBundleCategoryCountsAsync();
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Check bundle availability for specific dates
        /// </summary>
        [HttpPost("{id:guid}/availability")]
        public async Task<IActionResult> CheckBundleAvailability(Guid id, [FromBody] BundleAvailabilityRequest request)
        {
            if (request.BundleId != id)
            {
                return BadRequest("Bundle ID mismatch");
            }

            if (request.StartDate >= request.EndDate)
            {
                return BadRequest("End date must be after start date");
            }

            if (request.StartDate < DateTime.UtcNow.Date)
            {
                return BadRequest("Start date cannot be in the past");
            }

            var result = await _bundleService.CheckBundleAvailabilityAsync(request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Calculate bundle rental cost
        /// </summary>
        [HttpPost("{id:guid}/cost")]
        public async Task<IActionResult> CalculateBundleCost(Guid id, [FromBody] BundleAvailabilityRequest request)
        {
            if (request.BundleId != id)
            {
                return BadRequest("Bundle ID mismatch");
            }

            var result = await _bundleService.CalculateBundleCostAsync(id, request.StartDate, request.EndDate);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Create a new bundle (requires authentication)
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateBundle([FromBody] CreateBundleRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var result = await _bundleService.CreateBundleAsync(request, userId);
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return CreatedAtAction(nameof(GetBundle), new { id = result.Data?.Id }, result);
        }

        /// <summary>
        /// Update an existing bundle (requires authentication and ownership)
        /// </summary>
        [HttpPut("{id:guid}")]
        [Authorize]
        public async Task<IActionResult> UpdateBundle(Guid id, [FromBody] CreateBundleRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var result = await _bundleService.UpdateBundleAsync(id, request, userId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Delete a bundle (requires authentication and ownership)
        /// </summary>
        [HttpDelete("{id:guid}")]
        [Authorize]
        public async Task<IActionResult> DeleteBundle(Guid id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var result = await _bundleService.DeleteBundleAsync(id, userId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Get user's bundles (requires authentication)
        /// </summary>
        [HttpGet("my-bundles")]
        [Authorize]
        public async Task<IActionResult> GetMyBundles(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var result = await _bundleService.GetUserBundlesAsync(userId, page, pageSize);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Create a bundle rental request (requires authentication)
        /// </summary>
        [HttpPost("rentals")]
        [Authorize]
        public async Task<IActionResult> CreateBundleRental([FromBody] CreateBundleRentalRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            if (request.RentalDate >= request.ReturnDate)
            {
                return BadRequest("Return date must be after rental date");
            }

            if (request.RentalDate < DateTime.UtcNow.Date)
            {
                return BadRequest("Rental date cannot be in the past");
            }

            var result = await _bundleService.CreateBundleRentalAsync(request, userId);
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return CreatedAtAction(nameof(GetBundleRental), new { id = result.Data?.Id }, result);
        }

        /// <summary>
        /// Get a specific bundle rental (requires authentication)
        /// </summary>
        [HttpGet("rentals/{id:guid}")]
        [Authorize]
        public async Task<IActionResult> GetBundleRental(Guid id)
        {
            var result = await _bundleService.GetBundleRentalByIdAsync(id);
            if (!result.Success)
            {
                return BadRequest(result);
            }

            if (result.Data == null)
            {
                return NotFound();
            }

            return Ok(result);
        }

        /// <summary>
        /// Get user's bundle rentals (requires authentication)
        /// </summary>
        [HttpGet("rentals")]
        [Authorize]
        public async Task<IActionResult> GetMyBundleRentals(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var result = await _bundleService.GetUserBundleRentalsAsync(userId, page, pageSize);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Approve a bundle rental (requires authentication and ownership of tools in bundle)
        /// </summary>
        [HttpPost("rentals/{id:guid}/approve")]
        [Authorize]
        public async Task<IActionResult> ApproveBundleRental(Guid id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var result = await _bundleService.ApproveBundleRentalAsync(id, userId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Reject a bundle rental (requires authentication and ownership of tools in bundle)
        /// </summary>
        [HttpPost("rentals/{id:guid}/reject")]
        [Authorize]
        public async Task<IActionResult> RejectBundleRental(Guid id, [FromBody] RejectBundleRentalRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var result = await _bundleService.RejectBundleRentalAsync(id, userId, request.Reason);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Cancel a bundle rental (requires authentication and being the renter)
        /// </summary>
        [HttpPost("rentals/{id:guid}/cancel")]
        [Authorize]
        public async Task<IActionResult> CancelBundleRental(Guid id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var result = await _bundleService.CancelBundleRentalAsync(id, userId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Set featured status for a bundle (admin only)
        /// </summary>
        [HttpPost("{id:guid}/featured")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SetFeaturedStatus(Guid id, [FromBody] SetFeaturedRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var result = await _bundleService.SetFeaturedStatusAsync(id, request.IsFeatured, userId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Create a review for a bundle (requires authentication and completed rental)
        /// </summary>
        [HttpPost("{bundleId:guid}/reviews")]
        [Authorize]
        public async Task<IActionResult> CreateBundleReview(Guid bundleId, [FromBody] CreateBundleReviewRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Validate request
            if (request.Rating < 1 || request.Rating > 5)
            {
                return BadRequest("Rating must be between 1 and 5");
            }

            if (string.IsNullOrEmpty(request.Title) || request.Title.Length > 100)
            {
                return BadRequest("Title is required and must be 100 characters or less");
            }

            if (string.IsNullOrEmpty(request.Comment) || request.Comment.Length > 1000)
            {
                return BadRequest("Comment is required and must be 1000 characters or less");
            }

            // Set the bundle ID from the route
            request.BundleId = bundleId;

            var result = await _bundleService.CreateBundleReviewAsync(request, userId);
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return CreatedAtAction(nameof(GetBundleReviews), new { bundleId = bundleId }, result);
        }

        /// <summary>
        /// Get reviews for a bundle with pagination
        /// </summary>
        [HttpGet("{bundleId:guid}/reviews")]
        public async Task<IActionResult> GetBundleReviews(
            Guid bundleId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _bundleService.GetBundleReviewsAsync(bundleId, page, pageSize);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Get bundle review summary (ratings distribution and latest reviews)
        /// </summary>
        [HttpGet("{bundleId:guid}/reviews/summary")]
        public async Task<IActionResult> GetBundleReviewSummary(Guid bundleId)
        {
            var result = await _bundleService.GetBundleReviewSummaryAsync(bundleId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Check if user can review a bundle (requires authentication)
        /// </summary>
        [HttpGet("{bundleId:guid}/can-review")]
        [Authorize]
        public async Task<IActionResult> CanReviewBundle(Guid bundleId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var result = await _bundleService.CanUserReviewBundleAsync(bundleId, userId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Delete a bundle review (requires authentication and ownership of review)
        /// </summary>
        [HttpDelete("reviews/{reviewId:guid}")]
        [Authorize]
        public async Task<IActionResult> DeleteBundleReview(Guid reviewId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var result = await _bundleService.DeleteBundleReviewAsync(reviewId, userId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Get bundle approval status for bundle owners (requires authentication)
        /// </summary>
        [HttpGet("{id:guid}/approval-status")]
        [Authorize]
        public async Task<IActionResult> GetBundleApprovalStatus(Guid id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var result = await _bundleService.GetBundleApprovalStatusAsync(id, userId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Upload a single image for bundle (requires authentication)
        /// </summary>
        [HttpPost("upload-image")]
        [Authorize]
        public async Task<IActionResult> UploadBundleImage(IFormFile file)
        {
            try
            {
                if (file == null)
                {
                    return BadRequest(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "No file provided",
                        Errors = new List<string> { "A file must be provided" }
                    });
                }

                var allowedTypes = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                
                if (!allowedTypes.Contains(extension))
                {
                    return BadRequest(new ApiResponse<string>
                    {
                        Success = false,
                        Message = $"Invalid file type: {file.FileName}",
                        Errors = new List<string> { "Only JPG, JPEG, PNG, GIF, and WebP files are allowed" }
                    });
                }

                if (file.Length > 5 * 1024 * 1024) // 5MB
                {
                    return BadRequest(new ApiResponse<string>
                    {
                        Success = false,
                        Message = $"File too large: {file.FileName}",
                        Errors = new List<string> { "Maximum file size is 5MB" }
                    });
                }

                using var stream = file.OpenReadStream();
                var fileName = $"{Guid.NewGuid()}{extension}";
                var storagePath = await _fileStorageService.UploadFileAsync(stream, fileName, file.ContentType, "images");
                var fileUrl = await _fileStorageService.GetFileUrlAsync(storagePath);

                return Ok(new ApiResponse<string>
                {
                    Success = true,
                    Data = fileUrl,
                    Message = "Image uploaded successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "An error occurred while uploading image",
                    Errors = new List<string> { ex.Message }
                });
            }
        }
    }

    public class RejectBundleRentalRequest
    {
        public string Reason { get; set; } = "";
    }

    public class SetFeaturedRequest
    {
        public bool IsFeatured { get; set; }
    }
}