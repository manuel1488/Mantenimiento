using AutoMapper;
using App.Core.DTOs.Shop;
using App.Models.Shop;

namespace App.Services.Mappings.Shop;

public class QuotationMappingProfile : Profile
{
    public QuotationMappingProfile()
    {
        CreateMap<Quotation, QuotationDto>()
            .ForMember(dest => dest.CustomerName,
                opt => opt.MapFrom(src => src.Customer.Name))
            .ForMember(dest => dest.CustomerEmail,
                opt => opt.MapFrom(src => src.Customer.Email))
            .ForMember(dest => dest.Details,
                opt => opt.MapFrom(src => src.Details));

        CreateMap<QuotationDetail, QuotationDetailDto>();
    }
}
