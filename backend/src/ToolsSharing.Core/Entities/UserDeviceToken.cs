namespace ToolsSharing.Core.Entities;

public class UserDeviceToken : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string DeviceToken { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty; // ios, android, web
    public bool IsActive { get; set; } = true;
    public DateTime LastUsed { get; set; } = DateTime.UtcNow;
    public string? DeviceInfo { get; set; }
    
    // Navigation properties
    public User User { get; set; } = null!;
}