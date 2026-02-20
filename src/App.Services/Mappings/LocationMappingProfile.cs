using App.Core.DTOs.Location;
using AutoMapper;
using LocationModel = App.Models.Shop.Location;

namespace App.Services.Mappings;

public class LocationMappingProfile : Profile
{
    public LocationMappingProfile()
    {
        CreateMap<LocationModel, LocationDto>();
        CreateMap<CreateLocationDto, LocationModel>();
        CreateMap<UpdateLocationDto, LocationModel>();
    }
}
