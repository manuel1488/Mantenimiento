using AutoMapper;

using App.Core.DTOs.Settings;
using App.Models.Settings;

namespace App.Services.Mappings.Settings;

public class InventorySettingsMappingProfile : Profile
{
    public InventorySettingsMappingProfile()
    {
        CreateMap<InventorySettings, InventorySettingsDto>();
    }
}
