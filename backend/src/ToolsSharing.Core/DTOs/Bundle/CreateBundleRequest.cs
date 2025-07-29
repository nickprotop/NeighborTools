using System.Collections.Generic;

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
        public string Location { get; set; } = ""; // Bundle location, falls back to owner's PublicLocation if empty
        public string Category { get; set; } = "";
        public string Tags { get; set; } = ""; // Comma-separated
        public bool IsPublished { get; set; } = false;
        
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