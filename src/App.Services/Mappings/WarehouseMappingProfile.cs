using AutoMapper;
using App.Core.DTOs.Warehouse;
using App.Models.Shop;

namespace App.Services.Mappings;

public class WarehouseMappingProfile : Profile
{
    public WarehouseMappingProfile()
    {
        CreateMap<Warehouse, WarehouseDto>()
            .ForMember(dest => dest.BranchName,
                opt => opt.MapFrom(src => src.Branch != null ? src.Branch.Name : null));
        CreateMap<CreateWarehouseDto, Warehouse>();
        CreateMap<UpdateWarehouseDto, Warehouse>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}