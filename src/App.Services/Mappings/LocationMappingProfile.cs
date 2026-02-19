using App.Core.DTOs.Location;
using App.Models.Shop;
using AutoMapper;

namespace App.Services.Mappings;

public class LocationMappingProfile : Profile
{
    public LocationMappingProfile()
    {
        CreateMap<Location, LocationDto>();
        CreateMap<CreateLocationDto, Location>();
        CreateMap<UpdateLocationDto, Location>();
    }
}
