using AutoMapper;

using App.Core.DTOs.Identity;
using App.Models.Identity;

namespace App.Services.Mappings;

public class CashierProfileMappingProfile : Profile
{
    public CashierProfileMappingProfile()
    {
        CreateMap<CashierProfile, CashierProfileDto>()
            .ForMember(d => d.UserFullName, o => o.MapFrom(s => s.User != null ? s.User.FullName : string.Empty))
            .ForMember(d => d.UserEmail, o => o.MapFrom(s => s.User != null ? s.User.Email : null))
            .ForMember(d => d.LocationName, o => o.MapFrom(s => s.Location != null ? s.Location.Name : string.Empty));
    }
}
