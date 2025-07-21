using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ToolsSharing.Core.DTOs.Bundle;
using ToolsSharing.Core.Interfaces;

namespace ToolsSharing.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BundlesController : ControllerBase
    {
        private readonly IBundleService _bundleService;

        public BundlesController(IBundleService bundleService)
        {
            _bundleService = bundleService;
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

            // Increment view count asynchronously
            _ = Task.Run(async () => await _bundleService.IncrementViewCountAsync(id));

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