namespace ToolsSharing.Core.Entities;

public class ToolImage : BaseEntity
{
    public string ImageUrl { get; set; } = string.Empty;
    public string AltText { get; set; } = string.Empty;
    public bool IsPrimary { get; set; } = false;
    public int Order { get; set; }
    public Guid ToolId { get; set; }
    
    // Navigation properties
    public Tool Tool { get; set; } = null!;
}