using AutoMapper;

using App.Core.DTOs.Shop;
using App.Models.Shop;

namespace App.Services.Mappings;

public class SaleMappingProfile : Profile
{
    public SaleMappingProfile()
    {
        CreateMap<Sale, SaleDto>()
            .ForMember(dest => dest.CustomerName,
                opt => opt.MapFrom(src => src.Customer.Name))
            .ForMember(dest => dest.BranchName,
                opt => opt.MapFrom(src => src.Branch != null ? src.Branch.Name : null))
            .ForMember(dest => dest.Details,
                opt => opt.MapFrom(src => src.Details));

        CreateMap<SaleDetail, SaleDetailDto>()
            .ForMember(dest => dest.ProductName,
                opt => opt.MapFrom(src => src.Product.Name))
            .ForMember(dest => dest.ProductCode,
                opt => opt.MapFrom(src => src.Product.Code))
            .ForMember(dest => dest.PartialSaleFractionCode,
                opt => opt.MapFrom(src => src.PartialSaleFraction != null ? src.PartialSaleFraction.Code : null))
            .ForMember(dest => dest.PartialSaleFractionName,
                opt => opt.MapFrom(src => src.PartialSaleFraction != null ? src.PartialSaleFraction.Name : null));
    }
}
