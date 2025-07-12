using Mapster;
using ToolsSharing.Core.Entities;
using ToolsSharing.Core.Features.Tools;
using ToolsSharing.Core.Features.Rentals;

namespace ToolsSharing.Infrastructure.Mappings;

public static class MappingConfig
{
    public static void ConfigureMappings()
    {
        // Tool mappings
        TypeAdapterConfig<Tool, ToolDto>
            .NewConfig()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Name, src => src.Name ?? "")
            .Map(dest => dest.Description, src => src.Description ?? "")
            .Map(dest => dest.Category, src => src.Category ?? "")
            .Map(dest => dest.Brand, src => src.Brand ?? "")
            .Map(dest => dest.Model, src => src.Model ?? "")
            .Map(dest => dest.DailyRate, src => src.DailyRate)
            .Map(dest => dest.WeeklyRate, src => src.WeeklyRate)
            .Map(dest => dest.MonthlyRate, src => src.MonthlyRate)
            .Map(dest => dest.DepositRequired, src => src.DepositRequired)
            .Map(dest => dest.Condition, src => src.Condition ?? "")
            .Map(dest => dest.Location, src => src.Location ?? "")
            .Map(dest => dest.IsAvailable, src => src.IsAvailable)
            .Map(dest => dest.OwnerId, src => src.OwnerId ?? "")
            .Map(dest => dest.OwnerName, src => 
                src.Owner != null ? $"{src.Owner.FirstName ?? ""} {src.Owner.LastName ?? ""}".Trim() : "Unknown Owner")
            .Map(dest => dest.ImageUrls, src => 
                src.Images != null ? src.Images.Select(img => img.ImageUrl ?? "").ToList() : new List<string>());

        TypeAdapterConfig<ToolDto, Tool>
            .NewConfig()
            .Ignore(dest => dest.Images)
            .Ignore(dest => dest.Owner)
            .Ignore(dest => dest.Rentals)
            .Ignore(dest => dest.OwnerId)
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.UpdatedAt)
            .Ignore(dest => dest.IsDeleted);

        // Rental mappings
        TypeAdapterConfig<Rental, RentalDto>
            .NewConfig()
            .Map(dest => dest.ToolName, src => src.Tool.Name)
            .Map(dest => dest.RenterName, src => $"{src.Renter.FirstName} {src.Renter.LastName}")
            .Map(dest => dest.OwnerId, src => src.Tool.OwnerId)
            .Map(dest => dest.OwnerName, src => $"{src.Tool.Owner.FirstName} {src.Tool.Owner.LastName}")
            .Map(dest => dest.Status, src => src.Status.ToString());

        TypeAdapterConfig<RentalDto, Rental>
            .NewConfig()
            .Ignore(dest => dest.Tool)
            .Ignore(dest => dest.Renter)
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.UpdatedAt)
            .Ignore(dest => dest.ApprovedAt)
            .Ignore(dest => dest.CancelledAt)
            .Ignore(dest => dest.CancellationReason);
    }
}