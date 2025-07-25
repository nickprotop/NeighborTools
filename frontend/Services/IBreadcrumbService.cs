using frontend.Models;

namespace frontend.Services;

/// <summary>
/// Service for generating breadcrumb navigation items
/// </summary>
public interface IBreadcrumbService
{
    /// <summary>
    /// Generate breadcrumbs for tools section
    /// </summary>
    /// <param name="toolName">Name of the tool (for details/edit pages)</param>
    /// <param name="category">Tool category (for filtered views)</param>
    /// <param name="page">Current page type</param>
    /// <param name="toolId">Tool ID (for edit pages to link back to details)</param>
    /// <returns>List of breadcrumb items</returns>
    List<frontend.Models.BreadcrumbItem> GenerateToolsBreadcrumb(
        string? toolName = null, 
        string? category = null, 
        BreadcrumbPage page = BreadcrumbPage.Browser,
        string? toolId = null);

    /// <summary>
    /// Generate breadcrumbs for bundles section
    /// </summary>
    /// <param name="bundleName">Name of the bundle (for details/edit pages)</param>
    /// <param name="category">Bundle category (for filtered views)</param>
    /// <param name="page">Current page type</param>
    /// <param name="bundleId">Bundle ID (for edit pages to link back to details)</param>
    /// <returns>List of breadcrumb items</returns>
    List<frontend.Models.BreadcrumbItem> GenerateBundlesBreadcrumb(
        string? bundleName = null, 
        string? category = null, 
        BreadcrumbPage page = BreadcrumbPage.Browser,
        string? bundleId = null);

    /// <summary>
    /// Generate breadcrumbs using configuration object
    /// </summary>
    /// <param name="config">Breadcrumb configuration</param>
    /// <returns>List of breadcrumb items</returns>
    List<frontend.Models.BreadcrumbItem> GenerateBreadcrumbs(BreadcrumbConfiguration config);

    /// <summary>
    /// Generate breadcrumbs for admin section
    /// </summary>
    /// <param name="segments">Admin section segments</param>
    /// <returns>List of breadcrumb items</returns>
    List<frontend.Models.BreadcrumbItem> GenerateAdminBreadcrumb(params string[] segments);

    /// <summary>
    /// Generate custom breadcrumbs
    /// </summary>
    /// <param name="rootText">Root breadcrumb text</param>
    /// <param name="rootUrl">Root breadcrumb URL</param>
    /// <param name="segments">Additional segments</param>
    /// <returns>List of breadcrumb items</returns>
    List<frontend.Models.BreadcrumbItem> GenerateCustomBreadcrumb(string rootText, string rootUrl, params string[] segments);

    /// <summary>
    /// Create a simple breadcrumb item
    /// </summary>
    /// <param name="text">Display text</param>
    /// <param name="href">Navigation URL</param>
    /// <param name="disabled">Whether item is disabled</param>
    /// <param name="icon">Optional icon</param>
    /// <returns>Breadcrumb item</returns>
    frontend.Models.BreadcrumbItem CreateBreadcrumbItem(string text, string? href = null, bool disabled = false, string? icon = null);
}