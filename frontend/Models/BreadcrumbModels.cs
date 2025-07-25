namespace frontend.Models;

/// <summary>
/// Represents a single breadcrumb item in the navigation
/// </summary>
public class BreadcrumbItem
{
    /// <summary>
    /// Display text for the breadcrumb
    /// </summary>
    public string Text { get; set; } = "";
    
    /// <summary>
    /// URL to navigate to when clicked. If null, breadcrumb is not clickable
    /// </summary>
    public string? Href { get; set; }
    
    /// <summary>
    /// Whether the breadcrumb is disabled (typically the current page)
    /// </summary>
    public bool Disabled { get; set; } = false;
    
    /// <summary>
    /// Optional icon to display before the text
    /// </summary>
    public string? Icon { get; set; }
    
    /// <summary>
    /// Additional CSS classes to apply
    /// </summary>
    public string? Class { get; set; }
    
    /// <summary>
    /// Tooltip text to show on hover
    /// </summary>
    public string? Tooltip { get; set; }
}

/// <summary>
/// Enum representing different page types for breadcrumb generation
/// </summary>
public enum BreadcrumbPage
{
    /// <summary>
    /// Main browser/listing page (e.g., /tools, /bundles)
    /// </summary>
    Browser,
    
    /// <summary>
    /// Details/view page (e.g., /tools/{id}, /bundles/{id})
    /// </summary>
    Details,
    
    /// <summary>
    /// Create/new page (e.g., /tools/create, /bundles/create)
    /// </summary>
    Create,
    
    /// <summary>
    /// Edit page (e.g., /tools/{id}/edit, /bundles/{id}/edit)
    /// </summary>
    Edit,
    
    /// <summary>
    /// Custom page that doesn't fit other categories
    /// </summary>
    Custom
}

/// <summary>
/// Enum representing different section types for breadcrumb generation
/// </summary>
public enum BreadcrumbSection
{
    /// <summary>
    /// Tools section
    /// </summary>
    Tools,
    
    /// <summary>
    /// Bundles section
    /// </summary>
    Bundles,
    
    /// <summary>
    /// Admin section
    /// </summary>
    Admin,
    
    /// <summary>
    /// User profile section
    /// </summary>
    Profile,
    
    /// <summary>
    /// Custom section
    /// </summary>
    Custom
}

/// <summary>
/// Configuration for generating breadcrumbs
/// </summary>
public class BreadcrumbConfiguration
{
    /// <summary>
    /// The section this breadcrumb belongs to
    /// </summary>
    public BreadcrumbSection Section { get; set; }
    
    /// <summary>
    /// The current page type
    /// </summary>
    public BreadcrumbPage Page { get; set; }
    
    /// <summary>
    /// Item name (for details/edit pages)
    /// </summary>
    public string? ItemName { get; set; }
    
    /// <summary>
    /// Category (for filtered views)
    /// </summary>
    public string? Category { get; set; }
    
    /// <summary>
    /// Item ID (for edit pages that need to link back to details)
    /// </summary>
    public string? ItemId { get; set; }
    
    /// <summary>
    /// Custom segments for complex breadcrumbs
    /// </summary>
    public List<string> CustomSegments { get; set; } = new();
    
    /// <summary>
    /// Whether to include the marketplace root
    /// </summary>
    public bool IncludeMarketplace { get; set; } = true;
}