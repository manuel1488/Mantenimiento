using AutoMapper;
using App.Core.DTOs.Settings;
using App.Models.Settings;

namespace App.Services.Mappings;

public class ObraSeguimientoClienteSettingsMappingProfile : Profile
{
    public ObraSeguimientoClienteSettingsMappingProfile()
    {
        CreateMap<ObraSeguimientoClienteSettings, ObraSeguimientoClienteSettingsDto>();
        CreateMap<UpdateObraSeguimientoClienteSettingsDto, ObraSeguimientoClienteSettings>();
    }
}
