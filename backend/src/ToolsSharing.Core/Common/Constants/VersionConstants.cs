namespace ToolsSharing.Core.Common.Constants;

/// <summary>
/// Centralized version management for Terms of Service, Privacy Policy, and other legal documents
/// </summary>
public static class VersionConstants
{
    /// <summary>
    /// Current version of Terms of Service
    /// </summary>
    public const string CURRENT_TERMS_VERSION = "1.0";
    
    /// <summary>
    /// Current version of Privacy Policy
    /// </summary>
    public const string CURRENT_PRIVACY_VERSION = "1.0";
    
    /// <summary>
    /// Current version of GDPR consent forms
    /// </summary>
    public const string CURRENT_CONSENT_VERSION = "1.0";
    
    /// <summary>
    /// Gets the current terms version
    /// </summary>
    public static string GetCurrentTermsVersion() => CURRENT_TERMS_VERSION;
    
    /// <summary>
    /// Gets the current privacy policy version
    /// </summary>
    public static string GetCurrentPrivacyVersion() => CURRENT_PRIVACY_VERSION;
    
    /// <summary>
    /// Gets the current consent version
    /// </summary>
    public static string GetCurrentConsentVersion() => CURRENT_CONSENT_VERSION;
}