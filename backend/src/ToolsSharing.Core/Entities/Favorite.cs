namespace ToolsSharing.Core.Entities;

public class Favorite : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public Guid ToolId { get; set; }
    
    // Navigation properties
    public User User { get; set; } = null!;
    public Tool Tool { get; set; } = null!;
}