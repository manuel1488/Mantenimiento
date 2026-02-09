using AutoMapper;

using App.Core.DTOs.Settings;
using App.Models.Settings;

namespace App.Services.Mappings;

public class CompanySettingsMappingProfile : Profile
{
    public CompanySettingsMappingProfile()
    {
        CreateMap<CompanySettings, CompanySettingsDto>();
        CreateMap<UpdateCompanySettingsDto, CompanySettings>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}