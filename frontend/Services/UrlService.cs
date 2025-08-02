using ToolsSharing.Frontend.Configuration;

namespace ToolsSharing.Frontend.Services;

public interface IUrlService
{
    string GetFileUrl(string storagePath);
    string GetApiBaseUrl();
}

public class UrlService : IUrlService
{
    private readonly AppSettings _appSettings;
    private readonly string _apiBaseUrl;

    public UrlService(AppSettings appSettings)
    {
        _appSettings = appSettings;
        
        // Get API base URL from AppSettings
        _apiBaseUrl = _appSettings.ApiSettings.BaseUrl.TrimEnd('/');
    }

    /// <summary>
    /// Converts a storage path to a full file download URL
    /// </summary>
    /// <param name="storagePath">Raw storage path (e.g., "images/file.png")</param>
    /// <returns>Full URL to download the file</returns>
    public string GetFileUrl(string storagePath)
    {
        if (string.IsNullOrEmpty(storagePath))
            return string.Empty;

        // Handle both raw paths and already-formed URLs
        if (storagePath.StartsWith("http://") || storagePath.StartsWith("https://"))
        {
            return storagePath; // Already a full URL
        }

        if (storagePath.StartsWith("/api/files/download/"))
        {
            // Convert relative API URL to full URL
            return $"{_apiBaseUrl}{storagePath}";
        }

        // Raw storage path - construct full download URL
        return $"{_apiBaseUrl}/api/files/download/{storagePath}";
    }

    /// <summary>
    /// Gets the base API URL for general use
    /// </summary>
    public string GetApiBaseUrl()
    {
        return _apiBaseUrl;
    }
}