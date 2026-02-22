using AutoMapper;
using App.Core.DTOs.Shop.CashStation;
using App.Models.Shop;

namespace App.Services.Mappings;

public class CashStationMappingProfile : Profile
{
    public CashStationMappingProfile()
    {
        CreateMap<CashStation, CashStationDto>()
            .ForMember(d => d.LocationName, o => o.MapFrom(s => s.Location != null ? s.Location.Name : string.Empty));
    }
}
