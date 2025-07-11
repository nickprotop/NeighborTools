using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using ToolsSharing.Core.Entities.GDPR;
using ToolsSharing.Core.Entities;
using ToolsSharing.Core.Common.Interfaces;
using ToolsSharing.Core.Interfaces.GDPR;
using ToolsSharing.Infrastructure.Data;
using ToolsSharing.Core.Common.Constants;

namespace ToolsSharing.Infrastructure.Services.GDPR;

public class ConsentService : IConsentService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;
    
    public ConsentService(ApplicationDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task RecordConsentAsync(UserConsent consent)
    {
        // Withdraw previous consent of same type if exists
        var existingConsent = await _context.UserConsents
            .Where(c => c.UserId == consent.UserId && 
                       c.ConsentType == consent.ConsentType && 
                       c.WithdrawnDate == null)
            .FirstOrDefaultAsync();

        if (existingConsent != null)
        {
            existingConsent.WithdrawnDate = DateTime.UtcNow;
            existingConsent.WithdrawalReason = "Superseded by new consent";
        }

        _context.UserConsents.Add(consent);
        await _context.SaveChangesAsync();
    }

    public async Task<List<UserConsent>> GetUserConsentsAsync(string userId)
    {
        return await _context.UserConsents
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.ConsentDate)
            .ToListAsync();
    }

    public async Task WithdrawConsentAsync(string userId, ConsentType consentType, string reason)
    {
        var activeConsents = await _context.UserConsents
            .Where(c => c.UserId == userId && 
                       c.ConsentType == consentType && 
                       c.WithdrawnDate == null)
            .ToListAsync();

        foreach (var consent in activeConsents)
        {
            consent.WithdrawnDate = DateTime.UtcNow;
            consent.WithdrawalReason = reason;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<bool> HasValidConsentAsync(string userId, ConsentType consentType)
    {
        return await _context.UserConsents
            .AnyAsync(c => c.UserId == userId && 
                          c.ConsentType == consentType && 
                          c.ConsentGiven && 
                          c.WithdrawnDate == null);
    }

    public async Task<string> GetCurrentPrivacyVersionAsync()
    {
        var currentPolicy = await _context.PrivacyPolicyVersions.FirstOrDefaultAsync(p => p.IsActive);
        return currentPolicy?.Version ?? VersionConstants.GetCurrentPrivacyVersion();
    }

    public async Task UpdateUserConsentStatusAsync(string userId, ConsentType consentType, bool granted)
    {
        var consent = new UserConsent
        {
            UserId = userId,
            ConsentType = consentType,
            ConsentGiven = granted,
            ConsentDate = DateTime.UtcNow,
            ConsentSource = "system_update",
            ConsentVersion = await GetCurrentPrivacyVersionAsync()
        };

        await RecordConsentAsync(consent);
    }

    public async Task SyncUserConsentAsync(string userId, ConsentType consentType, bool granted, string source, string? ipAddress = null, string? userAgent = null)
    {
        // Create UserConsent record for audit trail
        var consent = new UserConsent
        {
            UserId = userId,
            ConsentType = consentType,
            ConsentGiven = granted,
            ConsentDate = DateTime.UtcNow,
            ConsentSource = source,
            ConsentVersion = await GetCurrentPrivacyVersionAsync(),
            IPAddress = ipAddress ?? "",
            UserAgent = userAgent ?? ""
        };

        await RecordConsentAsync(consent);

        // Update User entity fields for quick access
        var user = await _userManager.FindByIdAsync(userId);
        if (user != null)
        {
            var updateUser = false;

            switch (consentType)
            {
                case ConsentType.DataProcessing:
                    user.DataProcessingConsent = granted;
                    updateUser = true;
                    break;
                case ConsentType.Marketing:
                    user.MarketingConsent = granted;
                    updateUser = true;
                    break;
            }

            if (updateUser)
            {
                user.LastConsentUpdate = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
            }
        }
    }

    public async Task SyncAllUserConsentsFromEntityAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return;

        var currentVersion = await GetCurrentPrivacyVersionAsync();

        // Check if we already have UserConsent records for this user
        var existingConsents = await _context.UserConsents
            .Where(c => c.UserId == userId && c.WithdrawnDate == null)
            .Select(c => c.ConsentType)
            .ToListAsync();

        // Create UserConsent records for User entity consents if they don't exist
        if (!existingConsents.Contains(ConsentType.DataProcessing) && user.DataProcessingConsent)
        {
            _context.UserConsents.Add(new UserConsent
            {
                UserId = userId,
                ConsentType = ConsentType.DataProcessing,
                ConsentGiven = user.DataProcessingConsent,
                ConsentDate = user.LastConsentUpdate ?? user.CreatedAt,
                ConsentSource = "sync_from_entity",
                ConsentVersion = currentVersion
            });
        }

        if (!existingConsents.Contains(ConsentType.Marketing) && user.MarketingConsent)
        {
            _context.UserConsents.Add(new UserConsent
            {
                UserId = userId,
                ConsentType = ConsentType.Marketing,
                ConsentGiven = user.MarketingConsent,
                ConsentDate = user.LastConsentUpdate ?? user.CreatedAt,
                ConsentSource = "sync_from_entity",
                ConsentVersion = currentVersion
            });
        }

        await _context.SaveChangesAsync();
    }

    public async Task RecordInitialConsentsAsync(string userId, bool dataProcessingConsent, bool marketingConsent, string source, string? ipAddress = null, string? userAgent = null)
    {
        var currentVersion = await GetCurrentPrivacyVersionAsync();
        var consentDate = DateTime.UtcNow;

        // Create UserConsent records
        if (dataProcessingConsent)
        {
            _context.UserConsents.Add(new UserConsent
            {
                UserId = userId,
                ConsentType = ConsentType.DataProcessing,
                ConsentGiven = true,
                ConsentDate = consentDate,
                ConsentSource = source,
                ConsentVersion = currentVersion,
                IPAddress = ipAddress ?? "",
                UserAgent = userAgent ?? ""
            });
        }

        if (marketingConsent)
        {
            _context.UserConsents.Add(new UserConsent
            {
                UserId = userId,
                ConsentType = ConsentType.Marketing,
                ConsentGiven = true,
                ConsentDate = consentDate,
                ConsentSource = source,
                ConsentVersion = currentVersion,
                IPAddress = ipAddress ?? "",
                UserAgent = userAgent ?? ""
            });
        }

        await _context.SaveChangesAsync();
    }
}