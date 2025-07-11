using Microsoft.EntityFrameworkCore;
using ToolsSharing.Core.Entities.GDPR;
using ToolsSharing.Core.Interfaces.GDPR;
using ToolsSharing.Infrastructure.Data;

namespace ToolsSharing.Infrastructure.Services.GDPR;

public class DataProcessingLogger : IDataProcessingLogger
{
    private readonly ApplicationDbContext _context;
    public DataProcessingLogger(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogDataProcessingAsync(DataProcessingActivity activity)
    {
        var log = new DataProcessingLog
        {
            UserId = activity.UserId,
            ActivityType = activity.ActivityType,
            DataCategories = activity.DataCategories,
            ProcessingPurpose = activity.ProcessingPurpose,
            LegalBasis = activity.LegalBasis,
            DataSources = activity.DataSources,
            DataRecipients = activity.DataRecipients,
            RetentionPeriod = activity.RetentionPeriod,
            ProcessingDate = activity.ProcessingDate,
            IPAddress = activity.IPAddress,
            UserAgent = activity.UserAgent
        };

        _context.DataProcessingLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task<List<DataProcessingLog>> GetUserProcessingLogAsync(string userId)
    {
        return await _context.DataProcessingLogs
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.ProcessingDate)
            .ToListAsync();
    }
}