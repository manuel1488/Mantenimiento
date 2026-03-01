using AutoMapper;

using App.Core.DTOs.Label;
using App.Models.Shop;

namespace App.Services.Mappings;

public class LabelMappingProfile : Profile
{
    public LabelMappingProfile()
    {
        CreateMap<BulkLabelJob, BulkLabelJobDto>()
            .ForMember(dest => dest.ProductName,
                opt => opt.MapFrom(src => src.Product != null ? src.Product.Name : string.Empty))
            .ForMember(dest => dest.ProductCode,
                opt => opt.MapFrom(src => src.Product != null ? src.Product.Code : string.Empty));
    }
}
