using System.Collections.Generic;
using ToolsSharing.Core.DTOs.Location;
using ToolsSharing.Core.Enums;

namespace ToolsSharing.Core.DTOs.Bundle
{
    public class CreateBundleRequest
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Guidelines { get; set; } = "";
        public string RequiredSkillLevel { get; set; } = "Beginner";
        public int EstimatedProjectDuration { get; set; }
        public string? ImageUrl { get; set; }
        public decimal BundleDiscount { get; set; } = 0;
        public string Category { get; set; } = "";
        public string Tags { get; set; } = ""; // Comma-separated
        public bool IsPublished { get; set; } = false;
        
        // Location inheritance options (Phase 7)
        public LocationInheritanceOption LocationSource { get; set; } = LocationInheritanceOption.InheritFromProfile;
        public UpdateLocationRequest? CustomLocation { get; set; }
        
        public List<CreateBundleToolRequest> Tools { get; set; } = new();
    }
    
    public class CreateBundleToolRequest
    {
        public System.Guid ToolId { get; set; }
        public string UsageNotes { get; set; } = "";
        public int OrderInBundle { get; set; }
        public bool IsOptional { get; set; } = false;
        public int QuantityNeeded { get; set; } = 1;
    }
}