using System;
using System.Collections.Generic;

namespace ToolsSharing.Core.DTOs.Bundle
{
    public class BundleDto
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
        
        // Location
        public string Location { get; set; } = ""; // Bundle location (independent or falls back to owner's LocationDisplay)
        
        // Pricing
        public decimal BundleDiscount { get; set; }
        public decimal TotalCost { get; set; } // Sum of all tool costs
        public decimal DiscountedCost { get; set; } // After applying bundle discount
        
        // Visibility
        public bool IsPublished { get; set; }
        public bool IsFeatured { get; set; }
        public int ViewCount { get; set; }
        
        // Categories/Tags
        public string Category { get; set; } = "";
        public List<string> Tags { get; set; } = new();
        
        // Tools in bundle
        public List<BundleToolDto> Tools { get; set; } = new();
        
        // Availability
        public bool IsAvailable { get; set; }
        public DateTime? AvailableFromDate { get; set; }
        public List<UnavailableDateRangeDto> UnavailableDates { get; set; } = new();
        
        // Statistics
        public int RentalCount { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        
        // Approval/Moderation fields
        public bool IsApproved { get; set; }
        public bool PendingApproval { get; set; }
        public string? RejectionReason { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
    
    public class BundleToolDto
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
    
    public class UnavailableDateRangeDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Reason { get; set; } = "";
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
}