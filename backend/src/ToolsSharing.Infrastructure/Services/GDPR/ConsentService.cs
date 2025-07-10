using Microsoft.EntityFrameworkCore;
using ToolsSharing.Core.Entities.GDPR;
using ToolsSharing.Core.Common.Interfaces;
using ToolsSharing.Core.Interfaces.GDPR;
using ToolsSharing.Infrastructure.Data;

namespace ToolsSharing.Infrastructure.Services.GDPR;

public class ConsentService : IConsentService
{
    private readonly ApplicationDbContext _context;
    public ConsentService(ApplicationDbContext context)
    {
        _context = context;
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

    public async Task<List<UserConsent>> GetUserConsentsAsync(int userId)
    {
        return await _context.UserConsents
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.ConsentDate)
            .ToListAsync();
    }

    public async Task WithdrawConsentAsync(int userId, ConsentType consentType, string reason)
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

    public async Task<bool> HasValidConsentAsync(int userId, ConsentType consentType)
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
        return currentPolicy?.Version ?? "1.0";
    }

    public async Task UpdateUserConsentStatusAsync(int userId, ConsentType consentType, bool granted)
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
}