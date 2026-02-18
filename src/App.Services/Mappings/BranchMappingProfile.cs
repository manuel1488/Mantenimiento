using AutoMapper;
using App.Core.DTOs.Branch;
using App.Models.Shop;

namespace App.Services.Mappings;

public class BranchMappingProfile : Profile
{
    public BranchMappingProfile()
    {
        CreateMap<Branch, BranchDto>();
        CreateMap<CreateBranchDto, Branch>();
        CreateMap<UpdateBranchDto, Branch>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}
