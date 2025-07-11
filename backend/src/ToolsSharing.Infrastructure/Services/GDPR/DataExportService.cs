using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using ToolsSharing.Core.Interfaces.GDPR;
using ToolsSharing.Infrastructure.Data;

namespace ToolsSharing.Infrastructure.Services.GDPR;

public class DataExportService : IDataExportService
{
    private readonly ApplicationDbContext _context;
    private readonly string _exportPath;

    public DataExportService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _exportPath = configuration["GDPR:ExportPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "exports");
        
        // Ensure export directory exists
        Directory.CreateDirectory(_exportPath);
    }

    public async Task<string> ExportUserDataAsync(Guid requestId)
    {
        var request = await _context.DataSubjectRequests.FindAsync(requestId);
        if (request == null) throw new ArgumentException("Request not found");

        var userData = await GenerateUserDataExportAsync(request.UserId);
        
        var fileName = $"user_data_export_{request.UserId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
        var filePath = Path.Combine(_exportPath, fileName);
        
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        var jsonData = JsonSerializer.Serialize(userData, jsonOptions);
        await File.WriteAllTextAsync(filePath, jsonData);
        
        return filePath;
    }

    public async Task<UserDataExport> GenerateUserDataExportAsync(string userId)
    {
        var user = await _context.Users
            .Include(u => u.OwnedTools)
            .Include(u => u.RentalsAsOwner)
            .Include(u => u.RentalsAsRenter)
            .Include(u => u.ReviewsGiven)
            .Include(u => u.ReviewsReceived)
            .Include(u => u.Consents)
            .Include(u => u.ProcessingLogs)
            .Include(u => u.DataRequests)
            .Include(u => u.CookieConsents)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) throw new ArgumentException("User not found");

        var personalData = new
        {
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            user.PhoneNumber,
            user.Address,
            user.City,
            user.PostalCode,
            user.Country,
            user.DateOfBirth,
            user.ProfilePictureUrl,
            user.CreatedAt,
            user.UpdatedAt
        };

        var toolsData = user.OwnedTools.Select(t => new
        {
            t.Id,
            t.Name,
            t.Description,
            t.Category,
            t.DailyRate,
            t.IsAvailable,
            t.CreatedAt,
            t.UpdatedAt
        });

        var rentalsData = user.RentalsAsRenter.Concat(user.RentalsAsOwner).Select(r => new
        {
            r.Id,
            r.ToolId,
            r.RenterId,
            r.StartDate,
            r.EndDate,
            r.TotalCost,
            r.Status,
            r.CreatedAt,
            Role = r.RenterId == userId ? "Renter" : "Owner"
        });

        // Future: Financial data will be included when payment system is implemented
        var financialData = new object[] { };

        var consentHistory = user.Consents.Select(c => new
        {
            c.ConsentType,
            c.ConsentGiven,
            c.ConsentDate,
            c.ConsentVersion,
            c.ConsentSource,
            c.WithdrawnDate,
            c.WithdrawalReason
        });

        var processingLog = user.ProcessingLogs.Select(l => new
        {
            l.ActivityType,
            l.ProcessingPurpose,
            l.LegalBasis,
            l.ProcessingDate,
            l.RetentionPeriod
        });

        return new UserDataExport
        {
            ExportDate = DateTime.UtcNow,
            PersonalData = personalData,
            ToolsData = toolsData,
            RentalsData = rentalsData,
            FinancialData = financialData,
            ConsentHistory = consentHistory,
            ProcessingLog = processingLog
        };
    }
}