namespace ToolsSharing.Core.DTOs.Admin;

public class SampleDataAuditLog
{
    public Guid Id { get; set; }
    public string DataType { get; set; } = "";
    public string Action { get; set; } = ""; // "Applied", "Removed"
    public int RecordsAffected { get; set; }
    public string AdminUserId { get; set; } = "";
    public string AdminUserName { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class SampleDataConstants
{
    public const string USERS = "Users";
    public const string TOOLS = "Tools";
    public const string RENTALS = "Rentals";
    public const string REVIEWS = "Reviews";
    public const string MESSAGES = "Messages";
    public const string CONVERSATIONS = "Conversations";
    
    public static readonly Dictionary<string, string> DataTypeDisplayNames = new()
    {
        { USERS, "Sample Users" },
        { TOOLS, "Sample Tools" },
        { RENTALS, "Sample Rentals" },
        { REVIEWS, "Sample Reviews" },
        { MESSAGES, "Sample Messages" },
        { CONVERSATIONS, "Sample Conversations" }
    };
    
    public static readonly Dictionary<string, string> DataTypeDescriptions = new()
    {
        { USERS, "2 test users (john.doe@email.com, jane.smith@email.com)" },
        { TOOLS, "4 sample tools with different categories and conditions" },
        { RENTALS, "3 sample rental transactions with various statuses" },
        { REVIEWS, "4 sample reviews and ratings between users" },
        { MESSAGES, "8 sample messages across multiple conversations" },
        { CONVERSATIONS, "3 sample conversation threads between users" }
    };
}

// Sample user IDs for consistent referencing
public static class SampleDataIds
{
    public const string JOHN_DOE_USER_ID = "user1-guid-1234-5678-9012345678901";
    public const string JANE_SMITH_USER_ID = "user2-guid-1234-5678-9012345678902";
    
    // Tool IDs
    public static readonly Guid DRILL_TOOL_ID = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public static readonly Guid SAW_TOOL_ID = Guid.Parse("00000000-0000-0000-0000-000000000002");
    public static readonly Guid LADDER_TOOL_ID = Guid.Parse("00000000-0000-0000-0000-000000000003");
    public static readonly Guid PRESSURE_WASHER_TOOL_ID = Guid.Parse("00000000-0000-0000-0000-000000000004");
    
    // Rental IDs
    public static readonly Guid RENTAL_1_ID = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public static readonly Guid RENTAL_2_ID = Guid.Parse("00000000-0000-0000-0000-000000000002");
    public static readonly Guid RENTAL_3_ID = Guid.Parse("00000000-0000-0000-0000-000000000003");
    
    // Conversation IDs
    public static readonly Guid CONVERSATION_1_ID = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public static readonly Guid CONVERSATION_2_ID = Guid.Parse("00000000-0000-0000-0000-000000000002");
    public static readonly Guid CONVERSATION_3_ID = Guid.Parse("00000000-0000-0000-0000-000000000003");
}