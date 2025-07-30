using ToolsSharing.Frontend.Models.Location;

namespace ToolsSharing.Frontend.Utilities;

/// <summary>
/// Utility methods for location handling and display
/// </summary>
public static class LocationUtilities
{
    /// <summary>
    /// Convert distance band enum to human-readable text
    /// </summary>
    public static string GetDistanceBandText(DistanceBand distanceBand)
    {
        return distanceBand switch
        {
            DistanceBand.VeryClose => "Very close (under 0.5km)",
            DistanceBand.Nearby => "Nearby (0.5-2km)",
            DistanceBand.Moderate => "Moderate distance (2-10km)",
            DistanceBand.Far => "Far (10-50km)",
            DistanceBand.VeryFar => "Very far (over 50km)",
            _ => "Unknown distance"
        };
    }

    /// <summary>
    /// Convert privacy level enum to human-readable text
    /// </summary>
    public static string GetPrivacyLevelText(PrivacyLevel privacyLevel)
    {
        return privacyLevel switch
        {
            PrivacyLevel.Neighborhood => "Neighborhood level (~1km area)",
            PrivacyLevel.ZipCode => "Zip code level (~5km area)",
            PrivacyLevel.District => "District level (~15km area)",
            PrivacyLevel.Exact => "Exact location",
            _ => "Unknown privacy level"
        };
    }

    /// <summary>
    /// Convert location source enum to human-readable text
    /// </summary>
    public static string GetLocationSourceText(LocationSource locationSource)
    {
        return locationSource switch
        {
            LocationSource.Manual => "Manually entered",
            LocationSource.Geocoded => "From address lookup",
            LocationSource.UserClick => "Selected on map",
            LocationSource.Browser => "From your device",
            _ => "Unknown source"
        };
    }

    /// <summary>
    /// Validate coordinate ranges
    /// </summary>
    public static bool ValidateCoordinates(decimal? lat, decimal? lng)
    {
        if (!lat.HasValue || !lng.HasValue)
            return false;

        return lat.Value >= -90 && lat.Value <= 90 && 
               lng.Value >= -180 && lng.Value <= 180;
    }

    /// <summary>
    /// Parse coordinate string in various formats (decimal degrees, DMS)
    /// </summary>
    public static (decimal lat, decimal lng)? ParseCoordinates(string coordinateString)
    {
        if (string.IsNullOrWhiteSpace(coordinateString))
            return null;

        // Try decimal degrees format: "33.9519, -83.3576"
        var parts = coordinateString.Split(',');
        if (parts.Length == 2)
        {
            if (decimal.TryParse(parts[0].Trim(), out var lat) && 
                decimal.TryParse(parts[1].Trim(), out var lng))
            {
                if (ValidateCoordinates(lat, lng))
                    return (lat, lng);
            }
        }

        // Could add DMS parsing here if needed
        return null;
    }

    /// <summary>
    /// Format coordinates for display
    /// </summary>
    public static string FormatCoordinates(decimal lat, decimal lng, int precision = 4)
    {
        return $"{Math.Round(lat, precision)}, {Math.Round(lng, precision)}";
    }

    /// <summary>
    /// Calculate approximate distance between two points using Haversine formula
    /// Note: This is for display purposes only, server calculates authoritative distances
    /// </summary>
    public static decimal CalculateApproximateDistance(decimal lat1, decimal lng1, decimal lat2, decimal lng2)
    {
        const decimal EarthRadiusKm = 6371m;
        
        var dLat = ToRadians(lat2 - lat1);
        var dLng = ToRadians(lng2 - lng1);
        
        var a = Math.Sin((double)(dLat / 2)) * Math.Sin((double)(dLat / 2)) +
                Math.Cos((double)ToRadians(lat1)) * Math.Cos((double)ToRadians(lat2)) *
                Math.Sin((double)(dLng / 2)) * Math.Sin((double)(dLng / 2));
        
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        
        return EarthRadiusKm * (decimal)c;
    }

    private static decimal ToRadians(decimal degrees)
    {
        return degrees * (decimal)Math.PI / 180m;
    }

    /// <summary>
    /// Get geolocation error message for user display
    /// </summary>
    public static string GetGeolocationErrorMessage(GeolocationError error)
    {
        return error switch
        {
            GeolocationError.PermissionDenied => "Location access was denied. Please enable location permissions in your browser settings.",
            GeolocationError.PositionUnavailable => "Your location could not be determined. Please check your device's location settings.",
            GeolocationError.Timeout => "Location request timed out. Please try again.",
            GeolocationError.NotSupported => "Geolocation is not supported by your browser. Please enter your location manually.",
            _ => "An unknown error occurred while getting your location."
        };
    }

    /// <summary>
    /// Check if a location option has coordinates
    /// </summary>
    public static bool HasCoordinates(LocationOption location)
    {
        return location.Lat.HasValue && location.Lng.HasValue && 
               ValidateCoordinates(location.Lat, location.Lng);
    }

    /// <summary>
    /// Create a display string for a location option
    /// </summary>
    public static string GetLocationDisplayText(LocationOption location)
    {
        if (!string.IsNullOrEmpty(location.DisplayName))
            return location.DisplayName;

        var parts = new List<string>();
        if (!string.IsNullOrEmpty(location.Area)) parts.Add(location.Area);
        if (!string.IsNullOrEmpty(location.City)) parts.Add(location.City);
        if (!string.IsNullOrEmpty(location.State)) parts.Add(location.State);
        if (!string.IsNullOrEmpty(location.Country)) parts.Add(location.Country);

        return parts.Any() ? string.Join(", ", parts) : "Unknown location";
    }
}