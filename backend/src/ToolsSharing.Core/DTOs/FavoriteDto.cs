namespace ToolsSharing.Core.DTOs;

public class FavoriteDto
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public Guid ToolId { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Tool information
    public string ToolName { get; set; } = string.Empty;
    public string ToolDescription { get; set; } = string.Empty;
    public string ToolCategory { get; set; } = string.Empty;
    public decimal DailyRate { get; set; }
    public string ToolCondition { get; set; } = string.Empty;
    public string ToolLocation { get; set; } = string.Empty;
    public List<string> ToolImageUrls { get; set; } = new();
    public bool IsToolAvailable { get; set; }
    
    // Owner information
    public string OwnerName { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
}

public class AddToFavoritesRequest
{
    public Guid ToolId { get; set; }
}

public class RemoveFromFavoritesRequest
{
    public Guid ToolId { get; set; }
}

public class CheckFavoriteStatusRequest
{
    public Guid ToolId { get; set; }
}

public class FavoriteStatusDto
{
    public bool IsFavorited { get; set; }
    public Guid? FavoriteId { get; set; }
}