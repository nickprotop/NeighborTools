using Microsoft.AspNetCore.Identity;
using ToolsSharing.Core.Entities.GDPR;

namespace ToolsSharing.Core.Entities;

public class User : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? PublicLocation { get; set; }
    public DateTime DateOfBirth { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    
    // GDPR fields
    public bool DataProcessingConsent { get; set; } = false;
    public bool MarketingConsent { get; set; } = false;
    public DateTime? DataRetentionDate { get; set; }
    public DateTime? LastConsentUpdate { get; set; }
    public bool GDPROptOut { get; set; } = false;
    public bool DataPortabilityRequested { get; set; } = false;
    public DateTime? AnonymizationDate { get; set; }
    
    // Terms and Policies
    public bool TermsOfServiceAccepted { get; set; } = false;
    public DateTime? TermsAcceptedDate { get; set; }
    public string? TermsVersion { get; set; }
    
    // Navigation properties
    public ICollection<Tool> OwnedTools { get; set; } = new List<Tool>();
    public ICollection<Rental> RentalsAsOwner { get; set; } = new List<Rental>();
    public ICollection<Rental> RentalsAsRenter { get; set; } = new List<Rental>();
    public ICollection<Review> ReviewsGiven { get; set; } = new List<Review>();
    public ICollection<Review> ReviewsReceived { get; set; } = new List<Review>();
    public UserSettings? Settings { get; set; }
    
    // Payment navigation properties
    public ICollection<Payment> PaymentsMade { get; set; } = new List<Payment>();
    public ICollection<Payment> PaymentsReceived { get; set; } = new List<Payment>();
    public ICollection<Payout> Payouts { get; set; } = new List<Payout>();
    public PaymentSettings? PaymentSettings { get; set; }
    
    // GDPR navigation properties
    public ICollection<UserConsent> Consents { get; set; } = new List<UserConsent>();
    public ICollection<DataProcessingLog> ProcessingLogs { get; set; } = new List<DataProcessingLog>();
    public ICollection<DataSubjectRequest> DataRequests { get; set; } = new List<DataSubjectRequest>();
    public ICollection<CookieConsent> CookieConsents { get; set; } = new List<CookieConsent>();
    
    // Messaging navigation properties
    public ICollection<Message> SentMessages { get; set; } = new List<Message>();
    public ICollection<Message> ReceivedMessages { get; set; } = new List<Message>();
    public ICollection<Conversation> ConversationsAsParticipant1 { get; set; } = new List<Conversation>();
    public ICollection<Conversation> ConversationsAsParticipant2 { get; set; } = new List<Conversation>();
    
    // Favorites navigation properties
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
}