using frontend.Models;
using System.Web;
using MudBlazor;

namespace frontend.Services;

/// <summary>
/// Service for generating breadcrumb navigation items
/// </summary>
public class BreadcrumbService : IBreadcrumbService
{
    /// <summary>
    /// Generate breadcrumbs for tools section
    /// </summary>
    public List<frontend.Models.BreadcrumbItem> GenerateToolsBreadcrumb(
        string? toolName = null, 
        string? category = null, 
        BreadcrumbPage page = BreadcrumbPage.Browser,
        string? toolId = null)
    {
        var items = new List<frontend.Models.BreadcrumbItem>();

        // Add Marketplace root (always first)
        items.Add(CreateBreadcrumbItem(
            "Marketplace", 
            "/marketplace", 
            icon: Icons.Material.Filled.Store));

        // Add Tools section (always show except for root marketplace)
        items.Add(CreateBreadcrumbItem(
            "Tools", 
            "/tools", 
            icon: Icons.Material.Filled.Build));

        // Add category if provided
        if (!string.IsNullOrEmpty(category))
        {
            var categoryUrl = $"/tools?category={HttpUtility.UrlEncode(category)}";
            items.Add(CreateBreadcrumbItem(category, categoryUrl));
        }

        // Add page-specific breadcrumbs
        switch (page)
        {
            case BreadcrumbPage.Details:
                if (!string.IsNullOrEmpty(toolName))
                {
                    items.Add(CreateBreadcrumbItem(toolName, disabled: true));
                }
                break;

            case BreadcrumbPage.Create:
                items.Add(CreateBreadcrumbItem(
                    "Create Tool", 
                    disabled: true, 
                    icon: Icons.Material.Filled.Add));
                break;

            case BreadcrumbPage.Edit:
                if (!string.IsNullOrEmpty(toolName) && !string.IsNullOrEmpty(toolId))
                {
                    // Add tool name linking back to details
                    items.Add(CreateBreadcrumbItem(toolName, $"/tools/{toolId}"));
                    // Add current Edit page
                    items.Add(CreateBreadcrumbItem(
                        "Edit", 
                        disabled: true, 
                        icon: Icons.Material.Filled.Edit));
                }
                else if (!string.IsNullOrEmpty(toolName))
                {
                    // Fallback if no ID provided
                    items.Add(CreateBreadcrumbItem($"Edit {toolName}", disabled: true));
                }
                break;
        }

        return items;
    }

    /// <summary>
    /// Generate breadcrumbs for bundles section
    /// </summary>
    public List<frontend.Models.BreadcrumbItem> GenerateBundlesBreadcrumb(
        string? bundleName = null, 
        string? category = null, 
        BreadcrumbPage page = BreadcrumbPage.Browser,
        string? bundleId = null)
    {
        var items = new List<frontend.Models.BreadcrumbItem>();

        // Add Marketplace root (always first)
        items.Add(CreateBreadcrumbItem(
            "Marketplace", 
            "/marketplace", 
            icon: Icons.Material.Filled.Store));

        // Add Bundles section (always show except for root marketplace)
        items.Add(CreateBreadcrumbItem(
            "Bundles", 
            "/bundles", 
            icon: Icons.Material.Filled.Inventory));

        // Add category if provided
        if (!string.IsNullOrEmpty(category))
        {
            var categoryUrl = $"/bundles?category={HttpUtility.UrlEncode(category)}";
            items.Add(CreateBreadcrumbItem(category, categoryUrl));
        }

        // Add page-specific breadcrumbs
        switch (page)
        {
            case BreadcrumbPage.Details:
                if (!string.IsNullOrEmpty(bundleName))
                {
                    items.Add(CreateBreadcrumbItem(bundleName, disabled: true));
                }
                break;

            case BreadcrumbPage.Create:
                items.Add(CreateBreadcrumbItem(
                    "Create Bundle", 
                    disabled: true, 
                    icon: Icons.Material.Filled.Add));
                break;

            case BreadcrumbPage.Edit:
                if (!string.IsNullOrEmpty(bundleName) && !string.IsNullOrEmpty(bundleId))
                {
                    // Add bundle name linking back to details
                    items.Add(CreateBreadcrumbItem(bundleName, $"/bundles/{bundleId}"));
                    // Add current Edit page
                    items.Add(CreateBreadcrumbItem(
                        "Edit", 
                        disabled: true, 
                        icon: Icons.Material.Filled.Edit));
                }
                else if (!string.IsNullOrEmpty(bundleName))
                {
                    // Fallback if no ID provided
                    items.Add(CreateBreadcrumbItem($"Edit {bundleName}", disabled: true));
                }
                break;
        }

        return items;
    }

    /// <summary>
    /// Generate breadcrumbs using configuration object
    /// </summary>
    public List<frontend.Models.BreadcrumbItem> GenerateBreadcrumbs(BreadcrumbConfiguration config)
    {
        return config.Section switch
        {
            BreadcrumbSection.Tools => GenerateToolsBreadcrumb(
                config.ItemName, 
                config.Category, 
                config.Page, 
                config.ItemId),
                
            BreadcrumbSection.Bundles => GenerateBundlesBreadcrumb(
                config.ItemName, 
                config.Category, 
                config.Page, 
                config.ItemId),
                
            BreadcrumbSection.Admin => GenerateAdminBreadcrumb(config.CustomSegments.ToArray()),
            
            _ => GenerateCustomBreadcrumb("Home", "/", config.CustomSegments.ToArray())
        };
    }

    /// <summary>
    /// Generate breadcrumbs for admin section
    /// </summary>
    public List<frontend.Models.BreadcrumbItem> GenerateAdminBreadcrumb(params string[] segments)
    {
        var items = new List<frontend.Models.BreadcrumbItem>
        {
            CreateBreadcrumbItem(
                "Marketplace", 
                "/marketplace", 
                icon: Icons.Material.Filled.Store),
            CreateBreadcrumbItem(
                "Admin", 
                "/admin", 
                icon: Icons.Material.Filled.AdminPanelSettings)
        };

        for (int i = 0; i < segments.Length; i++)
        {
            var isLast = i == segments.Length - 1;
            var segment = segments[i];
            
            // For admin breadcrumbs, we don't auto-generate URLs
            // The calling code should handle navigation appropriately
            items.Add(CreateBreadcrumbItem(segment, disabled: isLast));
        }

        return items;
    }

    /// <summary>
    /// Generate custom breadcrumbs
    /// </summary>
    public List<frontend.Models.BreadcrumbItem> GenerateCustomBreadcrumb(string rootText, string rootUrl, params string[] segments)
    {
        var items = new List<frontend.Models.BreadcrumbItem>
        {
            CreateBreadcrumbItem(rootText, rootUrl)
        };

        for (int i = 0; i < segments.Length; i++)
        {
            var isLast = i == segments.Length - 1;
            var segment = segments[i];
            
            items.Add(CreateBreadcrumbItem(segment, disabled: isLast));
        }

        return items;
    }

    /// <summary>
    /// Create a simple breadcrumb item
    /// </summary>
    public frontend.Models.BreadcrumbItem CreateBreadcrumbItem(string text, string? href = null, bool disabled = false, string? icon = null)
    {
        return new frontend.Models.BreadcrumbItem
        {
            Text = text,
            Href = href,
            Disabled = disabled,
            Icon = icon
        };
    }
}