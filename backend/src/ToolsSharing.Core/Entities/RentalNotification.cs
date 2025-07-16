namespace ToolsSharing.Core.Entities;

public class RentalNotification : BaseEntity
{
    public Guid RentalId { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public string RecipientId { get; set; } = string.Empty;
    public string? Channel { get; set; } // email, sms, mobile
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
    
    // Navigation properties
    public Rental Rental { get; set; } = null!;
    public User Recipient { get; set; } = null!;
}