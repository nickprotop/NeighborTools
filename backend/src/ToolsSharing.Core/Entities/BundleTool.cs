using System;

namespace ToolsSharing.Core.Entities
{
    public class BundleTool : BaseEntity
    {
        public Guid BundleId { get; set; }
        public Bundle Bundle { get; set; } = null!;
        
        public Guid ToolId { get; set; }
        public Tool Tool { get; set; } = null!;
        
        // Additional information specific to this tool in this bundle
        public string UsageNotes { get; set; } = ""; // How this tool fits into the bundle project
        public int OrderInBundle { get; set; } = 0; // Display order
        public bool IsOptional { get; set; } = false; // Whether this tool is optional in the bundle
        
        // Quantity needed for the bundle project
        public int QuantityNeeded { get; set; } = 1;
    }
}