using System;
using System.Collections.Generic;
using ToolsSharing.Core.Enums;

namespace ToolsSharing.Core.Entities
{
    public class Bundle : BaseEntity
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Guidelines { get; set; } = ""; // How to use the bundle effectively
        public string RequiredSkillLevel { get; set; } = "Beginner"; // Beginner, Intermediate, Expert
        public int EstimatedProjectDuration { get; set; } // in hours
        public string? ImageUrl { get; set; }
        
        // Owner/Creator of the bundle
        public string UserId { get; set; } = "";
        public User User { get; set; } = null!;
        
        // Enhanced location fields (Phase 1 - Comprehensive Location System)
        public string? LocationDisplay { get; set; } // User-friendly display name
        public string? LocationArea { get; set; } // Neighborhood/area name
        public string? LocationCity { get; set; } // City name
        public string? LocationState { get; set; } // State/province name
        public string? LocationCountry { get; set; } // Country name
        public decimal? LocationLat { get; set; } // Latitude (quantized for privacy)
        public decimal? LocationLng { get; set; } // Longitude (quantized for privacy)
        public int? LocationPrecisionRadius { get; set; } // Generalization radius in meters
        public LocationSource? LocationSource { get; set; } // How location was obtained
        public PrivacyLevel LocationPrivacyLevel { get; set; } = PrivacyLevel.Neighborhood; // User's privacy preference
        public DateTime? LocationUpdatedAt { get; set; } // Track location changes
        
        // Phase 7 - Location Inheritance System
        public LocationInheritanceOption LocationInheritanceOption { get; set; } = LocationInheritanceOption.InheritFromProfile;
        
        // Pricing
        public decimal BundleDiscount { get; set; } = 0; // Percentage discount when renting as bundle
        
        // Visibility
        public bool IsPublished { get; set; } = false;
        public bool IsFeatured { get; set; } = false;
        public int ViewCount { get; set; } = 0;
        
        // Approval/Moderation fields
        public bool IsApproved { get; set; } = false;
        public bool PendingApproval { get; set; } = true;
        public string? RejectionReason { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? ApprovedById { get; set; }
        
        // Categories/Tags
        public string Category { get; set; } = ""; // e.g., "Home Improvement", "Gardening", "Woodworking"
        public string Tags { get; set; } = ""; // Comma-separated tags
        
        // Navigation properties
        public ICollection<BundleTool> BundleTools { get; set; } = new List<BundleTool>();
        public ICollection<BundleRental> BundleRentals { get; set; } = new List<BundleRental>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}