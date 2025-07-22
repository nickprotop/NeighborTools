using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ToolsSharing.Frontend.Models
{
    public class BundleModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Guidelines { get; set; } = "";
        public string RequiredSkillLevel { get; set; } = "Beginner";
        public int EstimatedProjectDuration { get; set; }
        public string? ImageUrl { get; set; }
        
        // Owner information
        public string UserId { get; set; } = "";
        public string OwnerName { get; set; } = "";
        public string OwnerLocation { get; set; } = "";
        
        // Pricing
        public decimal BundleDiscount { get; set; }
        public decimal TotalCost { get; set; }
        public decimal DiscountedCost { get; set; }
        
        // Visibility
        public bool IsPublished { get; set; }
        public bool IsFeatured { get; set; }
        public int ViewCount { get; set; }
        
        // Categories/Tags
        public string Category { get; set; } = "";
        public List<string> Tags { get; set; } = new();
        
        // Tools in bundle
        public List<BundleToolModel> Tools { get; set; } = new();
        
        // Availability
        public bool IsAvailable { get; set; }
        public DateTime? AvailableFromDate { get; set; }
        
        // Statistics
        public int RentalCount { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        
        // Approval/Moderation properties
        public bool IsApproved { get; set; }
        public bool PendingApproval { get; set; }
        public string? RejectionReason { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
    
    public class BundleToolModel
    {
        public Guid Id { get; set; }
        public Guid ToolId { get; set; }
        public string ToolName { get; set; } = "";
        public string ToolDescription { get; set; } = "";
        public string? ToolImageUrl { get; set; }
        public decimal DailyRate { get; set; }
        public string UsageNotes { get; set; } = "";
        public int OrderInBundle { get; set; }
        public bool IsOptional { get; set; }
        public int QuantityNeeded { get; set; }
        public bool IsAvailable { get; set; }
        public string OwnerName { get; set; } = "";
        public string OwnerId { get; set; } = "";
    }

    public class CreateBundleModel
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = "";

        [Required]
        [StringLength(2000)]
        public string Description { get; set; } = "";

        [StringLength(5000)]
        public string Guidelines { get; set; } = "";

        public string RequiredSkillLevel { get; set; } = "Beginner";

        [Range(1, 1000)]
        public int EstimatedProjectDuration { get; set; }

        public string? ImageUrl { get; set; }

        [Range(0, 50)]
        public decimal BundleDiscount { get; set; } = 0;

        [Required]
        public string Category { get; set; } = "";

        public string Tags { get; set; } = "";

        public bool IsPublished { get; set; } = false;

        public List<CreateBundleToolModel> Tools { get; set; } = new();
    }

    public class CreateBundleToolModel
    {
        public Guid ToolId { get; set; }
        public string UsageNotes { get; set; } = "";
        public int OrderInBundle { get; set; }
        public bool IsOptional { get; set; } = false;
        public int QuantityNeeded { get; set; } = 1;
    }

    public class BundleAvailabilityModel
    {
        public Guid BundleId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class BundleAvailabilityResponseModel
    {
        public bool IsAvailable { get; set; }
        public DateTime? EarliestAvailableDate { get; set; }
        public string Message { get; set; } = "";
        public BundleCostCalculationModel? CostCalculation { get; set; }
        public List<ToolAvailabilityStatusModel> ToolAvailability { get; set; } = new();
    }

    public class ToolAvailabilityStatusModel
    {
        public Guid ToolId { get; set; }
        public string ToolName { get; set; } = "";
        public bool IsAvailable { get; set; }
        public DateTime? AvailableFromDate { get; set; }
        public string UnavailabilityReason { get; set; } = "";
        public bool IsOptional { get; set; }
    }

    public class BundleCostCalculationModel
    {
        public decimal TotalCost { get; set; }
        public decimal BundleDiscountAmount { get; set; }
        public decimal FinalCost { get; set; }
        public decimal SecurityDeposit { get; set; }
        public decimal PlatformFee { get; set; }
        public decimal GrandTotal { get; set; }
        public List<ToolCostBreakdownModel> ToolCosts { get; set; } = new();
    }

    public class ToolCostBreakdownModel
    {
        public Guid ToolId { get; set; }
        public string ToolName { get; set; } = "";
        public decimal DailyRate { get; set; }
        public int RentalDays { get; set; }
        public int QuantityNeeded { get; set; }
        public decimal TotalCost { get; set; }
    }

    public class BundleRentalModel
    {
        public Guid Id { get; set; }
        public Guid BundleId { get; set; }
        public string BundleName { get; set; } = "";
        public string RenterUserId { get; set; } = "";
        public string RenterName { get; set; } = "";
        public DateTime RentalDate { get; set; }
        public DateTime ReturnDate { get; set; }
        
        // Pricing
        public decimal TotalCost { get; set; }
        public decimal BundleDiscountAmount { get; set; }
        public decimal FinalCost { get; set; }
        
        // Status
        public string Status { get; set; } = "";
        
        // Notes
        public string? RenterNotes { get; set; }
        public string? OwnerNotes { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateBundleRentalModel
    {
        public Guid BundleId { get; set; }
        
        [Required]
        public DateTime RentalDate { get; set; }
        
        [Required]
        public DateTime ReturnDate { get; set; }
        
        public string? RenterNotes { get; set; }
        
        public List<Guid> SelectedToolIds { get; set; } = new(); // Optional tools selection
    }

    // DTOs for bundle approval status
    public class BundleApprovalStatusDto
    {
        public Guid BundleId { get; set; }
        public string BundleName { get; set; } = "";
        public bool BundleIsApproved { get; set; }
        public bool BundleIsPending { get; set; }
        public string? BundleRejectionReason { get; set; }
        public bool HasUnapprovedTools { get; set; }
        public List<UnapprovedToolInfo> UnapprovedTools { get; set; } = new();
        public bool CanBePubliclyVisible { get; set; }
        public string? WarningMessage { get; set; }
    }

    public class UnapprovedToolInfo
    {
        public Guid ToolId { get; set; }
        public string ToolName { get; set; } = "";
        public bool IsPending { get; set; }
        public string? RejectionReason { get; set; }
    }

    // Request approval models
    public class RequestApprovalRequest
    {
        public string? Message { get; set; }
    }
}