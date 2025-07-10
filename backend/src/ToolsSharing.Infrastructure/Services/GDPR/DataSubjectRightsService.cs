using Microsoft.EntityFrameworkCore;
using ToolsSharing.Core.Entities;
using ToolsSharing.Core.Entities.GDPR;
using ToolsSharing.Core.Interfaces.GDPR;
using ToolsSharing.Infrastructure.Data;

namespace ToolsSharing.Infrastructure.Services.GDPR;

public class DataSubjectRightsService : IDataSubjectRightsService
{
    private readonly ApplicationDbContext _context;
    private readonly IDataExportService _dataExportService;

    public DataSubjectRightsService(
        ApplicationDbContext context,
        IDataExportService dataExportService)
    {
        _context = context;
        _dataExportService = dataExportService;
    }

    public async Task<DataSubjectRequest> CreateDataRequestAsync(int userId, DataRequestType type, string? details)
    {
        var request = new DataSubjectRequest
        {
            UserId = userId,
            RequestType = type,
            Status = DataRequestStatus.Pending,
            RequestDate = DateTime.UtcNow,
            RequestDetails = details ?? string.Empty
        };

        _context.DataSubjectRequests.Add(request);
        await _context.SaveChangesAsync();

        return request;
    }

    public async Task<DataErasureValidation> ValidateErasureRequestAsync(int userId)
    {
        var validation = new DataErasureValidation();
        var reasons = new List<string>();

        // Check for active rentals
        var activeRentals = await _context.Rentals
            .Where(r => (r.RenterId == userId.ToString() || r.Tool.OwnerId == userId.ToString()) && 
                       (r.Status == RentalStatus.PickedUp || r.Status == RentalStatus.Approved))
            .CountAsync();

        if (activeRentals > 0)
        {
            reasons.Add($"User has {activeRentals} active rental(s)");
        }

        // Future: Check for pending financial transactions
        // This will be implemented when payment system is added
        
        // Future: Check for legal obligations (e.g., tax records)
        // This will be implemented when payment system is added

        validation.CanErase = reasons.Count == 0;
        validation.Reasons = reasons;

        return validation;
    }

    public async Task ProcessDataRequestAsync(Guid requestId)
    {
        var request = await _context.DataSubjectRequests.FindAsync(requestId);
        if (request == null) return;

        try
        {
            switch (request.RequestType)
            {
                case DataRequestType.Access:
                    var exportPath = await _dataExportService.ExportUserDataAsync(requestId);
                    request.CompletionDate = DateTime.UtcNow;
                    request.Status = DataRequestStatus.Completed;
                    request.DataExportPath = exportPath;
                    break;

                case DataRequestType.Erasure:
                    var validation = await ValidateErasureRequestAsync(request.UserId);
                    if (validation.CanErase)
                    {
                        await AnonymizeUserDataAsync(request.UserId);
                        request.CompletionDate = DateTime.UtcNow;
                        request.Status = DataRequestStatus.Completed;
                    }
                    else
                    {
                        request.Status = DataRequestStatus.Rejected;
                        request.RejectionReason = string.Join("; ", validation.Reasons);
                    }
                    break;

                case DataRequestType.Rectification:
                    // This would require manual review
                    request.Status = DataRequestStatus.InProgress;
                    break;

                case DataRequestType.Portability:
                    var portabilityData = await _dataExportService.GenerateUserDataExportAsync(request.UserId);
                    request.CompletionDate = DateTime.UtcNow;
                    request.Status = DataRequestStatus.Completed;
                    request.ResponseDetails = System.Text.Json.JsonSerializer.Serialize(portabilityData);
                    break;
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception)
        {
            request.Status = DataRequestStatus.Rejected;
            await _context.SaveChangesAsync();
            throw;
        }
    }

    public async Task<DataSubjectRequest> GetDataRequestAsync(Guid requestId)
    {
        return await _context.DataSubjectRequests.FindAsync(requestId) ?? 
               throw new ArgumentException("Request not found", nameof(requestId));
    }

    public async Task<List<DataSubjectRequest>> GetUserDataRequestsAsync(int userId)
    {
        return await _context.DataSubjectRequests
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.RequestDate)
            .ToListAsync();
    }

    private async Task AnonymizeUserDataAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId.ToString());
        if (user == null) return;

        // Anonymize personal data
        user.FirstName = "Anonymized";
        user.LastName = "User";
        user.Email = $"anonymized_{userId}@deleted.local";
        user.PhoneNumber = null;
        user.Address = null;
        user.City = null;
        user.PostalCode = null;
        user.Country = null;
        user.ProfilePictureUrl = null;
        user.AnonymizationDate = DateTime.UtcNow;
        user.IsDeleted = true;

        // Keep audit trails but anonymize IP addresses in logs
        var logs = await _context.DataProcessingLogs
            .Where(l => l.UserId == userId)
            .ToListAsync();

        foreach (var log in logs)
        {
            log.IPAddress = "xxx.xxx.xxx.xxx";
            log.UserAgent = "Anonymized";
        }

        await _context.SaveChangesAsync();
    }
}