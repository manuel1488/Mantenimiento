using AutoMapper;
using App.Core.DTOs.Location;
using App.Models.Shop;

namespace App.Services.Mappings;

public class LocationTicketSettingsMappingProfile : Profile
{
    public LocationTicketSettingsMappingProfile()
    {
        CreateMap<LocationTicketSettings, LocationTicketSettingsDto>()
            .ForMember(dest => dest.LocationName,
                opt => opt.MapFrom(src => src.Location != null ? src.Location.Name : null));

        CreateMap<CreateLocationTicketSettingsDto, LocationTicketSettings>();

        CreateMap<UpdateLocationTicketSettingsDto, LocationTicketSettings>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}
