using MudBlazor;

namespace frontend.Models;

public class ReminderInfo
{
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public List<string> Suggestions { get; set; } = new();
    public string PrimaryAction { get; set; } = "";
    public string SecondaryAction { get; set; } = "";
    public string Icon { get; set; } = Icons.Material.Filled.Info;
    public Color Color { get; set; } = Color.Info;
}