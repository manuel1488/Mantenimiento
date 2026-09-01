using AutoMapper;
using App.Core.DTOs.Settings;
using App.Models.Settings;

namespace App.Services.Mappings;

public class TelegramSettingsMappingProfile : Profile
{
    public TelegramSettingsMappingProfile()
    {
        CreateMap<TelegramSettings, TelegramSettingsDto>();

        CreateMap<UpdateTelegramSettingsDto, TelegramSettings>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}
