using AutoMapper;

using App.Core.DTOs.UnitMeasure;
using App.Models.Shop;

namespace App.Services.Mappings;

public class UnitMeasureMappingProfile : Profile
{
    public UnitMeasureMappingProfile()
    {
        CreateMap<UnitMeasure, UnitMeasureDto>()
            .ForMember(dest => dest.MexicoSatUnitCode,
                opt => opt.MapFrom(src => src.MexicoSatUnit != null ? src.MexicoSatUnit.Code : null))
            .ForMember(dest => dest.MexicoSatUnitName,
                opt => opt.MapFrom(src => src.MexicoSatUnit != null ? src.MexicoSatUnit.Name : null));
        
        CreateMap<CreateUnitMeasureDto, UnitMeasure>();
        
        CreateMap<UpdateUnitMeasureDto, UnitMeasure>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}