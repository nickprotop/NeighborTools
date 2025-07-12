namespace ToolsSharing.Core.Common.Models.EmailNotifications;

public class RentalRequestNotification : EmailNotification
{
    public string OwnerName { get; set; } = string.Empty;
    public string RenterName { get; set; } = string.Empty;
    public string ToolName { get; set; } = string.Empty;
    public string ToolImageUrl { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalCost { get; set; }
    public string? Message { get; set; }
    public string ApprovalUrl { get; set; } = string.Empty;
    public string RentalDetailsUrl { get; set; } = string.Empty;
    
    public RentalRequestNotification()
    {
        Type = EmailNotificationType.RentalRequest;
        Priority = EmailPriority.High;
    }
    
    public override string GetSubject() => $"New rental request for {ToolName}";
    public override string GetTemplateName() => "RentalRequest";
    public override object GetTemplateData() => new
    {
        OwnerName,
        RenterName,
        ToolName,
        ToolImageUrl,
        StartDate,
        EndDate,
        Duration = (EndDate - StartDate).Days,
        TotalCost,
        Message,
        ApprovalUrl,
        RentalDetailsUrl,
        Year = DateTime.UtcNow.Year
    };
}

public class RentalApprovedNotification : EmailNotification
{
    public string RenterName { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string OwnerPhone { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
    public string ToolName { get; set; } = string.Empty;
    public string ToolLocation { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalCost { get; set; }
    public string RentalDetailsUrl { get; set; } = string.Empty;
    
    public RentalApprovedNotification()
    {
        Type = EmailNotificationType.RentalApproved;
        Priority = EmailPriority.High;
    }
    
    public override string GetSubject() => $"Your rental request for {ToolName} has been approved!";
    public override string GetTemplateName() => "RentalApproved";
    public override object GetTemplateData() => new
    {
        RenterName,
        OwnerName,
        OwnerPhone,
        OwnerEmail,
        ToolName,
        ToolLocation,
        StartDate,
        EndDate,
        Duration = (EndDate - StartDate).Days,
        TotalCost,
        RentalDetailsUrl,
        Year = DateTime.UtcNow.Year
    };
}

public class RentalRejectedNotification : EmailNotification
{
    public string RenterName { get; set; } = string.Empty;
    public string ToolName { get; set; } = string.Empty;
    public string? RejectionReason { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string BrowseToolsUrl { get; set; } = string.Empty;
    
    public RentalRejectedNotification()
    {
        Type = EmailNotificationType.RentalRejected;
        Priority = EmailPriority.Normal;
    }
    
    public override string GetSubject() => $"Rental request for {ToolName} was not approved";
    public override string GetTemplateName() => "RentalRejected";
    public override object GetTemplateData() => new
    {
        RenterName,
        ToolName,
        RejectionReason,
        StartDate,
        EndDate,
        BrowseToolsUrl,
        Year = DateTime.UtcNow.Year
    };
}

public class RentalReminderNotification : EmailNotification
{
    public string UserName { get; set; } = string.Empty;
    public string ToolName { get; set; } = string.Empty;
    public DateTime PickupDate { get; set; }
    public DateTime ReturnDate { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public string OwnerPhone { get; set; } = string.Empty;
    public string ToolLocation { get; set; } = string.Empty;
    public bool IsPickupReminder { get; set; }
    public string RentalDetailsUrl { get; set; } = string.Empty;
    
    public RentalReminderNotification()
    {
        Type = EmailNotificationType.RentalReminder;
        Priority = EmailPriority.Normal;
    }
    
    public override string GetSubject() => IsPickupReminder 
        ? $"Reminder: Pick up {ToolName} tomorrow" 
        : $"Reminder: Return {ToolName} tomorrow";
        
    public override string GetTemplateName() => "RentalReminder";
    public override object GetTemplateData() => new
    {
        UserName,
        ToolName,
        PickupDate,
        ReturnDate,
        OwnerName,
        OwnerPhone,
        ToolLocation,
        IsPickupReminder,
        ReminderType = IsPickupReminder ? "pickup" : "return",
        RentalDetailsUrl,
        Year = DateTime.UtcNow.Year
    };
}