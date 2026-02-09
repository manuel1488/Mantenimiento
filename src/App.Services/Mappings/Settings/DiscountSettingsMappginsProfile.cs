using AutoMapper;

using App.Core.DTOs.Settings;
using App.Models.Settings;

namespace App.Services.Mappings.Settings;

public class DiscountSettingsMappginsProfile : Profile
{
    public DiscountSettingsMappginsProfile()
    {
        CreateMap<DiscountSettings, DiscountSettingsDto>();
    }
}