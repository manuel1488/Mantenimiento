using AutoMapper;
using App.Core.DTOs.Settings;
using App.Models.Settings;

namespace App.Services.Mappings;

public class ObraFolioSettingsMappingProfile : Profile
{
    public ObraFolioSettingsMappingProfile()
    {
        CreateMap<ObraFolioSettings, ObraFolioSettingsDto>();
        CreateMap<UpdateObraFolioSettingsDto, ObraFolioSettings>();
    }
}
