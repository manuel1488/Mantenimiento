using AutoMapper;
using App.Core.DTOs.Shop;
using App.Models.Shop;

namespace App.Services.Mappings.Shop;

public class WholesaleTierMappingProfile : Profile
{
    public WholesaleTierMappingProfile()
    {
        CreateMap<WholesaleTier, WholesaleTierDto>();
    }
}
