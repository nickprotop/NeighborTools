using ToolsSharing.Core.Entities;

namespace ToolsSharing.Core.Interfaces;

public interface IDisputeNotificationService
{
    /// <summary>
    /// Send notification when a dispute is created
    /// </summary>
    Task SendDisputeCreatedNotificationAsync(Dispute dispute, User initiator, User otherParty);
    
    /// <summary>
    /// Send notification when a new message is added to a dispute
    /// </summary>
    Task SendNewMessageNotificationAsync(Dispute dispute, DisputeMessage message, User recipient);
    
    /// <summary>
    /// Send notification when dispute status changes
    /// </summary>
    Task SendStatusChangeNotificationAsync(Dispute dispute, DisputeStatus oldStatus, DisputeStatus newStatus, string? notes = null);
    
    /// <summary>
    /// Send notification when a dispute is escalated to PayPal
    /// </summary>
    Task SendEscalationNotificationAsync(Dispute dispute, string escalatedBy);
    
    /// <summary>
    /// Send notification when a dispute is resolved
    /// </summary>
    Task SendResolutionNotificationAsync(Dispute dispute, DisputeResolution resolution, string? notes = null);
    
    /// <summary>
    /// Send notification for evidence upload
    /// </summary>
    Task SendEvidenceUploadedNotificationAsync(Dispute dispute, string uploadedBy, int fileCount);
    
    /// <summary>
    /// Send reminder notification for overdue responses
    /// </summary>
    Task SendOverdueReminderNotificationAsync(Dispute dispute, User recipient);
}

public class DisputeNotification
{
    public string Type { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, object> Data { get; set; } = new();
}