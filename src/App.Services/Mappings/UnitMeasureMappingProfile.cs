using AutoMapper;

using App.Core.DTOs.UnitMeasure;
using App.Models.Shop;

namespace App.Services.Mappings;

public class UnitMeasureMappingProfile : Profile
{
    public UnitMeasureMappingProfile()
    {
        CreateMap<UnitMeasure, UnitMeasureDto>();
        
        CreateMap<CreateUnitMeasureDto, UnitMeasure>();
        
        CreateMap<UpdateUnitMeasureDto, UnitMeasure>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}