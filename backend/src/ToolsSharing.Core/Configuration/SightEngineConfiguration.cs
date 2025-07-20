using System.ComponentModel.DataAnnotations;
using ToolsSharing.Core.DTOs.ContentModeration;

namespace ToolsSharing.Core.Configuration;

public class SightEngineConfiguration
{
    public const string SectionName = "SightEngine";

    [Required]
    public string ApiUser { get; set; } = "";

    [Required]
    public string ApiSecret { get; set; } = "";

    public string BaseUrl { get; set; } = "https://api.sightengine.com/1.0";

    public int TimeoutSeconds { get; set; } = 30;

    public int MaxRetries { get; set; } = 3;

    public bool EnableLogging { get; set; } = true;

    public bool EnableCaching { get; set; } = false;

    public int CacheDurationMinutes { get; set; } = 60;

    public SightEngineModels Models { get; set; } = new();

    public SightEngineThresholds Thresholds { get; set; } = new();

    public SightEngineWorkflows Workflows { get; set; } = new();

    public bool IsConfigured => !string.IsNullOrEmpty(ApiUser) && !string.IsNullOrEmpty(ApiSecret);
}

public class SightEngineModels
{
    public List<string> Image { get; set; } = new()
    {
        "nudity-2.0",
        "wad",
        "offensive",
        "faces",
        "face-attributes",
        "celebrities",
        "text-content",
        "logo",
        "qr-content"
    };

    public List<string> Text { get; set; } = new()
    {
        "text-content",
        "text-classification"
    };

    public List<string> Video { get; set; } = new()
    {
        "nudity-2.0",
        "wad",
        "offensive"
    };
}

public class SightEngineThresholds
{
    // Image/Video model thresholds
    public double NudityThreshold { get; set; } = 0.5;
    public double WeaponThreshold { get; set; } = 0.7;
    public double AlcoholThreshold { get; set; } = 0.6;
    public double DrugThreshold { get; set; } = 0.7;
    public double OffensiveThreshold { get; set; } = 0.6;
    
    // Text rule-based model thresholds
    public double ProfanityThreshold { get; set; } = 0.5;
    public double TextContentThreshold { get; set; } = 0.5;
    
    // Text ML classification model thresholds (comprehensive)
    public double SexualThreshold { get; set; } = 0.5;
    public double DiscriminatoryThreshold { get; set; } = 0.4;
    public double InsultingThreshold { get; set; } = 0.5;
    public double ViolentThreshold { get; set; } = 0.3;
    public double ToxicThreshold { get; set; } = 0.5;
    public double SelfHarmThreshold { get; set; } = 0.3;
}

public class SightEngineWorkflows
{
    public string? ImageModerationWorkflow { get; set; }
    public string? TextModerationWorkflow { get; set; }
    public string? VideoModerationWorkflow { get; set; }
    public Dictionary<string, string> CustomWorkflows { get; set; } = new();
}

public class CascadedModerationConfiguration
{
    public const string SectionName = "CascadedModeration";

    /// <summary>
    /// Minimum severity level that triggers SightEngine analysis.
    /// Content with severity below this threshold will only use basic moderation.
    /// </summary>
    public ModerationSeverity SightEngineThreshold { get; set; } = ModerationSeverity.Severe;

    /// <summary>
    /// Whether to allow images when SightEngine is unavailable
    /// </summary>
    public bool AllowImagesWhenSightEngineUnavailable { get; set; } = false;

    /// <summary>
    /// Whether to allow videos when SightEngine is unavailable
    /// </summary>
    public bool AllowVideosWhenSightEngineUnavailable { get; set; } = false;

    /// <summary>
    /// SightEngine models to use for text analysis in the second stage
    /// </summary>
    public List<string> SightEngineModels { get; set; } = new()
    {
        "text-content",
        "text-classification"
    };

    /// <summary>
    /// Whether to enable cascaded moderation (if false, will use only basic or only SightEngine based on availability)
    /// </summary>
    public bool EnableCascadedModeration { get; set; } = true;

    /// <summary>
    /// Maximum time to wait for SightEngine response before falling back to basic result
    /// </summary>
    public int SightEngineTimeoutSeconds { get; set; } = 10;
}