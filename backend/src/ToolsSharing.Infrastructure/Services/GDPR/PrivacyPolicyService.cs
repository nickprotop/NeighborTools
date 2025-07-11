using Microsoft.EntityFrameworkCore;
using ToolsSharing.Core.Entities.GDPR;
using ToolsSharing.Core.Interfaces.GDPR;
using ToolsSharing.Infrastructure.Data;

namespace ToolsSharing.Infrastructure.Services.GDPR;

public class PrivacyPolicyService : IPrivacyPolicyService
{
    private readonly ApplicationDbContext _context;
    public PrivacyPolicyService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PrivacyPolicyVersion> GetCurrentPolicyAsync()
    {
        return await _context.PrivacyPolicyVersions
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.EffectiveDate)
            .FirstOrDefaultAsync() ?? 
            throw new InvalidOperationException("No active privacy policy found");
    }

    public async Task<List<PrivacyPolicyVersion>> GetPolicyVersionsAsync()
    {
        return await _context.PrivacyPolicyVersions
            .OrderByDescending(p => p.EffectiveDate)
            .ToListAsync();
    }

    public async Task<PrivacyPolicyVersion> CreatePolicyVersionAsync(string version, string content, string createdBy)
    {
        // Deactivate current policy
        var currentPolicy = await _context.PrivacyPolicyVersions
            .Where(p => p.IsActive)
            .FirstOrDefaultAsync();

        if (currentPolicy != null)
        {
            currentPolicy.IsActive = false;
        }

        // Create new policy version
        var newPolicy = new PrivacyPolicyVersion
        {
            Version = version,
            Content = content,
            EffectiveDate = DateTime.UtcNow,
            CreatedBy = createdBy,
            IsActive = true
        };

        _context.PrivacyPolicyVersions.Add(newPolicy);
        await _context.SaveChangesAsync();

        return newPolicy;
    }
}