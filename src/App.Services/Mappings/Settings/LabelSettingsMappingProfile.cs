using App.Core.DTOs.Settings;
using App.Models.Settings;
using AutoMapper;

namespace App.Services.Mappings.Settings;

public class LabelSettingsMappingProfile : Profile
{
    public LabelSettingsMappingProfile()
    {
        CreateMap<LabelSettings, LabelSettingsDto>();
    }
}
