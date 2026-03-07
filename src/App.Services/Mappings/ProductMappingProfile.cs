using AutoMapper;

using App.Core.DTOs.Product;
using App.Models.Shop;

namespace App.Services.Mappings;

public class ProductMappingProfile : Profile
{
    public ProductMappingProfile()
    {
        CreateMap<Product, ProductDto>()
            .ForMember(dest => dest.UnitMeasureName,
                opt => opt.MapFrom(src => src.UnitMeasure.Name))
            .ForMember(dest => dest.UnitMeasureCode,
                opt => opt.MapFrom(src => src.UnitMeasure.Code))
            .ForMember(dest => dest.MexicoProductServiceCode,
                opt => opt.MapFrom(src => src.MexicoProductService != null ? src.MexicoProductService.Code : null))
            .ForMember(dest => dest.MexicoProductServiceDescription,
                opt => opt.MapFrom(src => src.MexicoProductService != null ? src.MexicoProductService.Description : null));

        CreateMap<CreateProductDto, Product>();

        CreateMap<UpdateProductDto, Product>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) =>
                srcMember != null));

        CreateMap<ProductImage, ProductImageDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.FileName, opt => opt.MapFrom(src => src.FileName))
            .ForMember(dest => dest.ThumbnailFileName, opt => opt.MapFrom(src => src.ThumbnailFileName))
            .ForMember(dest => dest.ContentType, opt => opt.MapFrom(src => src.ContentType))
            .ForMember(dest => dest.IsPrimary, opt => opt.MapFrom(src => src.IsPrimary));
    }
}