using MudBlazor;

namespace frontend.Models;

public class SmartStatusInfo
{
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string? SubMessage { get; set; }
    public string Icon { get; set; } = Icons.Material.Filled.Circle;
    public Severity Severity { get; set; } = Severity.Normal;
    public bool ShowUrgency { get; set; } = false;
    public string UrgencyText { get; set; } = "";
    public Color UrgencyColor { get; set; } = Color.Warning;
    public string UrgencyIcon { get; set; } = Icons.Material.Filled.Warning;
    public bool ShowAdditionalInfo { get; set; } = false;
    public string AdditionalInfo { get; set; } = "";
}