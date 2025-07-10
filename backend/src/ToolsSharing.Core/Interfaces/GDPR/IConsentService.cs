using ToolsSharing.Core.Entities.GDPR;

namespace ToolsSharing.Core.Interfaces.GDPR;

public interface IConsentService
{
    Task RecordConsentAsync(UserConsent consent);
    Task<List<UserConsent>> GetUserConsentsAsync(string userId);
    Task WithdrawConsentAsync(string userId, ConsentType consentType, string reason);
    Task<bool> HasValidConsentAsync(string userId, ConsentType consentType);
    Task<string> GetCurrentPrivacyVersionAsync();
    Task UpdateUserConsentStatusAsync(string userId, ConsentType consentType, bool granted);
}

public interface IDataProcessingLogger
{
    Task LogDataProcessingAsync(DataProcessingActivity activity);
    Task<List<DataProcessingLog>> GetUserProcessingLogAsync(string userId);
}

public interface IDataSubjectRightsService
{
    Task<DataSubjectRequest> CreateDataRequestAsync(string userId, DataRequestType type, string? details);
    Task<DataErasureValidation> ValidateErasureRequestAsync(string userId);
    Task ProcessDataRequestAsync(Guid requestId);
    Task<DataSubjectRequest> GetDataRequestAsync(Guid requestId);
    Task<List<DataSubjectRequest>> GetUserDataRequestsAsync(string userId);
}

public interface IDataExportService
{
    Task<string> ExportUserDataAsync(Guid requestId);
    Task<UserDataExport> GenerateUserDataExportAsync(string userId);
}

public interface IPrivacyPolicyService
{
    Task<PrivacyPolicyVersion> GetCurrentPolicyAsync();
    Task<List<PrivacyPolicyVersion>> GetPolicyVersionsAsync();
    Task<PrivacyPolicyVersion> CreatePolicyVersionAsync(string version, string content, string createdBy);
}

// DTOs
public class DataProcessingActivity
{
    public string UserId { get; set; } = string.Empty;
    public string ActivityType { get; set; } = string.Empty;
    public List<string> DataCategories { get; set; } = new();
    public string ProcessingPurpose { get; set; } = string.Empty;
    public LegalBasis LegalBasis { get; set; }
    public List<string> DataSources { get; set; } = new();
    public List<string>? DataRecipients { get; set; }
    public string RetentionPeriod { get; set; } = string.Empty;
    public DateTime ProcessingDate { get; set; } = DateTime.UtcNow;
    public string IPAddress { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
}

public class DataErasureValidation
{
    public bool CanErase { get; set; }
    public List<string> Reasons { get; set; } = new();
}

public class UserDataExport
{
    public DateTime ExportDate { get; set; }
    public object PersonalData { get; set; } = null!;
    public object ToolsData { get; set; } = null!;
    public object RentalsData { get; set; } = null!;
    public object FinancialData { get; set; } = null!;
    public object ConsentHistory { get; set; } = null!;
    public object ProcessingLog { get; set; } = null!;
}