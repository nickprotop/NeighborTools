using AutoMapper;
using ToolsSharing.Core.Entities;
using ToolsSharing.Core.Features.Tools;
using ToolsSharing.Core.Features.Rentals;

namespace ToolsSharing.Infrastructure.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Tool mappings
        CreateMap<Tool, ToolDto>()
            .ForMember(dest => dest.OwnerName, opt => opt.MapFrom(src => $"{src.Owner.FirstName} {src.Owner.LastName}"))
            .ForMember(dest => dest.ImageUrls, opt => opt.MapFrom(src => 
                src.Images != null ? src.Images.Select(img => img.ImageUrl).ToList() : new List<string>()));

        CreateMap<ToolDto, Tool>()
            .ForMember(dest => dest.Images, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore())
            .ForMember(dest => dest.Rentals, opt => opt.Ignore())
            .ForMember(dest => dest.OwnerId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

        // Rental mappings
        CreateMap<Rental, RentalDto>()
            .ForMember(dest => dest.ToolName, opt => opt.MapFrom(src => src.Tool.Name))
            .ForMember(dest => dest.RenterName, opt => opt.MapFrom(src => $"{src.Renter.FirstName} {src.Renter.LastName}"))
            .ForMember(dest => dest.OwnerId, opt => opt.MapFrom(src => src.Tool.OwnerId))
            .ForMember(dest => dest.OwnerName, opt => opt.MapFrom(src => $"{src.Tool.Owner.FirstName} {src.Tool.Owner.LastName}"))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

        CreateMap<RentalDto, Rental>()
            .ForMember(dest => dest.Tool, opt => opt.Ignore())
            .ForMember(dest => dest.Renter, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.ApprovedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CancelledAt, opt => opt.Ignore())
            .ForMember(dest => dest.CancellationReason, opt => opt.Ignore());
    }
}