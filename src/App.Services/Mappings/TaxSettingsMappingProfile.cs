using AutoMapper;
using App.Core.DTOs.Settings;
using App.Models.Settings;

namespace App.Services.Mappings;

public class TaxSettingsMappingProfile : Profile
{
    public TaxSettingsMappingProfile()
    {
        CreateMap<TaxSettings, TaxSettingsDto>();
        CreateMap<UpdateTaxSettingsDto, TaxSettings>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}