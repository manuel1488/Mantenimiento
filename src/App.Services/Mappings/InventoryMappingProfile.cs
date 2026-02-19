using AutoMapper;

using App.Core.Constants;
using App.Core.DTOs.Inventory;
using App.Models.Shop;

namespace App.Services.Mappings;

public class InventoryMappingProfile : Profile
{
    public InventoryMappingProfile()
    {
        CreateMap<App.Models.Shop.Inventory, InventoryDto>()
            .ForMember(dest => dest.ProductName,
                opt => opt.MapFrom(src => src.Product.Name))
            .ForMember(dest => dest.ProductCode,
                opt => opt.MapFrom(src => src.Product.Code))
            .ForMember(dest => dest.ProductBrand,
                opt => opt.MapFrom(src => src.Product.Brand))
            .ForMember(dest => dest.ProductDescription,
                opt => opt.MapFrom(src => src.Product.Description))
            .ForMember(dest => dest.LocationName,
                opt => opt.MapFrom(src => src.Location.Name))
            .ForMember(dest => dest.LocationType,
                opt => opt.MapFrom(src => src.Location.Type))
            .ForMember(dest => dest.UnitMeasureName,
                opt => opt.MapFrom(src => src.Product.UnitMeasure.Name))
            .ForMember(dest => dest.ProductContent,
                opt => opt.MapFrom(src => src.Product.Content));

        CreateMap<InventoryMovement, InventoryMovementDto>()
            .ForMember(dest => dest.ProductName,
                opt => opt.MapFrom(src => src.Product.Name))
            .ForMember(dest => dest.BrandName,
                opt => opt.MapFrom(src => src.Product.Brand))
            .ForMember(dest => dest.ProductDescription,
                opt => opt.MapFrom(src => src.Product.Description))
            .ForMember(dest => dest.ProductCode, 
                opt => opt.MapFrom(src => src.Product.Code))
            .ForMember(dest => dest.LocationName,
                opt => opt.MapFrom(src => src.Location.Name))
            .ForMember(dest => dest.DestinationLocationName,
                opt => opt.MapFrom(src => src.DestinationLocation != null ?
                    src.DestinationLocation.Name : null))
            .ForMember(dest => dest.UnitMeasureName,
                opt => opt.MapFrom(src => src.Product.UnitMeasure.Name))
            .ForMember(dest => dest.ProductContent,
                opt => opt.MapFrom(src => src.Product.Content));

        CreateMap<App.Models.Shop.Inventory, InventoryAlertDto>()
            .ForMember(dest => dest.ProductName,
                opt => opt.MapFrom(src => src.Product.Name))
            .ForMember(dest => dest.ProductCode,
                opt => opt.MapFrom(src => src.Product.Code))
            .ForMember(dest => dest.ProductBrand,
                opt => opt.MapFrom(src => src.Product.Brand))
            .ForMember(dest => dest.ProductDescription,
                opt => opt.MapFrom(src => src.Product.Description))
            .ForMember(dest => dest.LocationName,
                opt => opt.MapFrom(src => src.Location.Name))
            .ForMember(dest => dest.UnitMeasureName, 
                opt => opt.MapFrom(src => src.Product.UnitMeasure.Name))
            .ForMember(dest => dest.AlertType, 
                opt => opt.MapFrom(src => 
                    src.MinStock.HasValue && src.Quantity < src.MinStock.Value
                        ? InventoryAlertType.LowStock
                        : src.MaxStock.HasValue && src.Quantity > src.MaxStock.Value
                            ? InventoryAlertType.OverStock
                            : null))
            .ForMember(dest => dest.CurrentStock, 
                opt => opt.MapFrom(src => src.Quantity));
    }
}