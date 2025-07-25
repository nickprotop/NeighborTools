namespace ToolsSharing.Core.Entities;

public class Favorite : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public Guid? ToolId { get; set; }
    public Guid? BundleId { get; set; }
    
    // Type of favorite (Tool or Bundle)
    public string FavoriteType { get; set; } = string.Empty; // "Tool" or "Bundle"
    
    // Navigation properties
    public User User { get; set; } = null!;
    public Tool? Tool { get; set; }
    public Bundle? Bundle { get; set; }
}