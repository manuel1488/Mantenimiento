using AutoMapper;

using App.Core.DTOs.Settings;
using App.Models.Settings;

namespace App.Services.Mappings.Settings;

public class RoundingSettingsMappingProfile : Profile
{
    public RoundingSettingsMappingProfile()
    {
        CreateMap<RoundingSettings, RoundingSettingsDto>();
    }
}
