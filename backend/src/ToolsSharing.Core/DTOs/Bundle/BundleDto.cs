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
}