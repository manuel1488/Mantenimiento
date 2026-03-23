using AutoMapper;
using App.Core.DTOs.Shop;
using App.Models.Shop;

namespace App.Services.Mappings.Shop;

public class RemissionMappingProfile : Profile
{
    public RemissionMappingProfile()
    {
        CreateMap<Remission, RemissionDto>()
            .ForMember(dest => dest.CustomerName,
                opt => opt.MapFrom(src => src.Customer.Name))
            .ForMember(dest => dest.LocationName,
                opt => opt.MapFrom(src => src.Location != null ? src.Location.Name : null))
            .ForMember(dest => dest.Details,
                opt => opt.MapFrom(src => src.Details));

        CreateMap<RemissionDetail, RemissionDetailDto>();
    }
}
