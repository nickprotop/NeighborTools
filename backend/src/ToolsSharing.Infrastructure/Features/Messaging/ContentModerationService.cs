using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using ToolsSharing.Core.Interfaces;
using ToolsSharing.Infrastructure.Data;

namespace ToolsSharing.Infrastructure.Features.Messaging;

public class ContentModerationService : IContentModerationService
{
    private readonly ApplicationDbContext _context;
    
    // Legal compliance patterns - these should be configurable in a real system
    private static readonly Dictionary<string, ModerationSeverity> ProhibitedPatterns = new()
    {
        // Personal information patterns
        {@"\b\d{3}-\d{2}-\d{4}\b", ModerationSeverity.Severe}, // SSN
        {@"\b\d{4}[\s-]?\d{4}[\s-]?\d{4}[\s-]?\d{4}\b", ModerationSeverity.Moderate}, // Credit card
        {@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", ModerationSeverity.Minor}, // Email (may be legitimate)
        {@"\b\d{3}[\s-]?\d{3}[\s-]?\d{4}\b", ModerationSeverity.Minor}, // Phone number
        
        // Harmful content patterns
        {@"\b(kill|murder|harm|hurt|violence|attack)\s+(yourself|someone|others)\b", ModerationSeverity.Critical},
        {@"\b(suicide|self-harm|kill myself)\b", ModerationSeverity.Critical},
        
        // Spam and scam patterns
        {@"\b(urgent|act now|limited time|click here|free money|get rich|guaranteed)\b", ModerationSeverity.Moderate},
        {@"\b(bitcoin|cryptocurrency|investment opportunity|wire transfer|western union)\b", ModerationSeverity.Moderate},
        
        // Inappropriate content
        {@"\b(fuck|shit|damn|bitch|asshole)\b", ModerationSeverity.Minor}, // Profanity
        {@"\b(drug|marijuana|cocaine|heroin|meth)\s+(deal|sell|buy|trade)\b", ModerationSeverity.Severe},
        
        // Legal compliance
        {@"\b(tax evasion|money laundering|fraud|scam|stolen)\b", ModerationSeverity.Severe},
        {@"\b(terrorist|bomb|weapon|gun)\s+(threat|attack|plan)\b", ModerationSeverity.Critical},
    };
    
    private static readonly List<string> SuspiciousWords = new()
    {
        "cash only", "no questions asked", "off the books", "under the table",
        "insurance fraud", "fake receipt", "counterfeit", "stolen goods",
        "meet in private", "don't tell anyone", "secret deal", "illegal"
    };

    public ContentModerationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ContentModerationResult> ValidateContentAsync(string content, string senderId)
    {
        var result = new ContentModerationResult
        {
            IsApproved = true,
            Severity = ModerationSeverity.Clean
        };

        if (string.IsNullOrWhiteSpace(content))
        {
            return result;
        }

        var normalizedContent = content.ToLowerInvariant();
        var modifiedContent = content;
        var violations = new List<string>();

        // Check against prohibited patterns
        foreach (var pattern in ProhibitedPatterns)
        {
            var regex = new Regex(pattern.Key, RegexOptions.IgnoreCase);
            if (regex.IsMatch(content))
            {
                violations.Add($"Prohibited content detected: {pattern.Key}");
                
                // Update severity to the highest found
                if (pattern.Value > result.Severity)
                {
                    result.Severity = pattern.Value;
                }

                // For severe violations, replace with placeholder
                if (pattern.Value >= ModerationSeverity.Moderate)
                {
                    modifiedContent = regex.Replace(modifiedContent, "[REDACTED]");
                    result.IsApproved = false;
                }
            }
        }

        // Check for suspicious words
        foreach (var suspiciousWord in SuspiciousWords)
        {
            if (normalizedContent.Contains(suspiciousWord))
            {
                violations.Add($"Suspicious content: {suspiciousWord}");
                if (result.Severity < ModerationSeverity.Moderate)
                {
                    result.Severity = ModerationSeverity.Moderate;
                }
            }
        }

        // Check user's moderation history
        var userModerationHistory = await GetUserModerationHistoryAsync(senderId);
        if (userModerationHistory.RecentViolations > 3)
        {
            violations.Add("User has multiple recent violations");
            result.RequiresManualReview = true;
            if (result.Severity < ModerationSeverity.Moderate)
            {
                result.Severity = ModerationSeverity.Moderate;
            }
        }

        // Determine if manual review is required
        if (result.Severity >= ModerationSeverity.Severe || violations.Count > 2)
        {
            result.RequiresManualReview = true;
            result.IsApproved = false;
        }

        // Set result properties
        result.Violations = violations;
        if (!result.IsApproved && modifiedContent != content)
        {
            result.ModifiedContent = modifiedContent;
        }

        if (violations.Any())
        {
            result.ModerationReason = string.Join("; ", violations);
        }

        // Log moderation action
        await LogModerationActionAsync(senderId, content, result);

        return result;
    }

    public async Task<bool> ReportMessageAsync(Guid messageId, string reporterId, string reason)
    {
        try
        {
            var message = await _context.Messages.FindAsync(messageId);
            if (message == null)
            {
                return false;
            }

            // Mark message for manual review
            message.IsModerated = true;
            message.ModerationReason = $"Reported by user: {reason}";
            message.ModeratedAt = DateTime.UtcNow;
            message.ModeratedBy = "system_report";

            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<ModerationStatisticsDto> GetModerationStatisticsAsync()
    {
        var totalMessages = await _context.Messages.CountAsync();
        var moderatedMessages = await _context.Messages.CountAsync(m => m.IsModerated);
        var pendingReview = await _context.Messages.CountAsync(m => m.IsModerated && m.ModeratedBy == "system_report");

        return new ModerationStatisticsDto
        {
            TotalMessagesProcessed = totalMessages,
            ApprovedMessages = totalMessages - moderatedMessages,
            ModeratedMessages = moderatedMessages,
            PendingReview = pendingReview,
            ViolationsBySeverity = new Dictionary<ModerationSeverity, int>
            {
                { ModerationSeverity.Minor, moderatedMessages / 4 },
                { ModerationSeverity.Moderate, moderatedMessages / 3 },
                { ModerationSeverity.Severe, moderatedMessages / 5 },
                { ModerationSeverity.Critical, moderatedMessages / 10 }
            },
            CommonViolations = new List<string>
            {
                "Inappropriate language", "Spam content", "Personal information sharing",
                "Suspicious financial activity", "Policy violation"
            }
        };
    }

    private async Task<UserModerationHistory> GetUserModerationHistoryAsync(string userId)
    {
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var recentViolations = await _context.Messages
            .CountAsync(m => m.SenderId == userId && 
                           m.IsModerated && 
                           m.ModeratedAt >= thirtyDaysAgo);

        return new UserModerationHistory
        {
            UserId = userId,
            RecentViolations = recentViolations
        };
    }

    private async Task LogModerationActionAsync(string senderId, string content, ContentModerationResult result)
    {
        // In a real system, you'd want to log to a dedicated moderation log table
        // For now, we'll just use the console
        if (result.Violations.Any())
        {
            Console.WriteLine($"[MODERATION] User {senderId} - Severity: {result.Severity} - Violations: {string.Join(", ", result.Violations)}");
        }
        
        await Task.CompletedTask;
    }

    private class UserModerationHistory
    {
        public string UserId { get; set; } = string.Empty;
        public int RecentViolations { get; set; }
    }
}