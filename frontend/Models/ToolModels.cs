namespace frontend.Models;

public class Tool
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal DailyRate { get; set; }
    public string Condition { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public string OwnerId { get; set; } = string.Empty;
    public UserInfo Owner { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public List<string> ImageUrls { get; set; } = new();
    public double? Rating { get; set; }
    public int ReviewCount { get; set; }
}

public class CreateToolRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal DailyRate { get; set; }
    public string Condition { get; set; } = string.Empty;
}