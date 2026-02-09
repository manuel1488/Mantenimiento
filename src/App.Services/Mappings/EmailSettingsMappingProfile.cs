using AutoMapper;
using App.Core.DTOs.Settings;
using App.Models.Settings;

namespace App.Services.Mappings;

public class EmailSettingsMappingProfile : Profile
{
    public EmailSettingsMappingProfile()
    {
        CreateMap<EmailSettings, EmailSettingsDto>();
        
        CreateMap<UpdateEmailSettingsDto, EmailSettings>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}