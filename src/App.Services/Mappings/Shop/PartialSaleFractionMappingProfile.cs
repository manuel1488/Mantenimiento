using AutoMapper;
using App.Core.DTOs.Shop;
using App.Models.Shop;

namespace App.Services.Mappings.Shop;

public class PartialSaleFractionMappingProfile : Profile
{
    public PartialSaleFractionMappingProfile()
    {
        CreateMap<PartialSaleFraction, PartialSaleFractionDto>();
    }
}
