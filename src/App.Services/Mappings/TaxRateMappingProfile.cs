using AutoMapper;

using App.Core.DTOs.Settings;
using App.Models.Settings;

namespace App.Services.Mappings;

public class TaxRateMappingProfile : Profile
{
    public TaxRateMappingProfile()
    {
        CreateMap<TaxRate, TaxRateDto>();
        
        CreateMap<CreateTaxRateDto, TaxRate>()
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(_ => true));

        CreateMap<UpdateTaxRateDto, TaxRate>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}