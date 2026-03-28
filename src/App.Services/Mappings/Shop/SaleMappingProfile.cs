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
            .ForMember(dest => dest.LocationName,
                opt => opt.MapFrom(src => src.Location != null ? src.Location.Name : null))
            .ForMember(dest => dest.QuotationNumber,
                opt => opt.MapFrom(src => src.Quotation != null ? src.Quotation.QuotationNumber : null))
            .ForMember(dest => dest.Details,
                opt => opt.MapFrom(src => src.Details))
            .ForMember(dest => dest.Payments,
                opt => opt.MapFrom(src => src.Payments))
            .ForMember(dest => dest.TaxRate,
                opt => opt.MapFrom(src => src.Details.Any()
                    ? src.Details.Where(d => d.TaxRate > 0).Select(d => d.TaxRate).FirstOrDefault()
                    : 0));

        CreateMap<SaleDetail, SaleDetailDto>()
            .ForMember(dest => dest.ProductName,
                opt => opt.MapFrom(src => src.Product.Name))
            .ForMember(dest => dest.ProductCode,
                opt => opt.MapFrom(src => src.Product.Code))
            .ForMember(dest => dest.PartialSaleFractionCode,
                opt => opt.MapFrom(src => src.PartialSaleFraction != null ? src.PartialSaleFraction.Code : null))
            .ForMember(dest => dest.PartialSaleFractionName,
                opt => opt.MapFrom(src => src.PartialSaleFraction != null ? src.PartialSaleFraction.Name : null));

        CreateMap<SalePayment, SalePaymentDto>()
            .ForMember(dest => dest.PaymentMethodName,
                opt => opt.MapFrom(src => src.PaymentMethod.Name))
            .ForMember(dest => dest.PaymentMethodType,
                opt => opt.MapFrom(src => src.PaymentMethod.Type))
            .ForMember(dest => dest.CardSubtype,
                opt => opt.MapFrom(src => src.PaymentMethod.CardSubtype))
            .ForMember(dest => dest.PaymentMethodIcon,
                opt => opt.MapFrom(src => src.PaymentMethod.Icon));
    }
}
