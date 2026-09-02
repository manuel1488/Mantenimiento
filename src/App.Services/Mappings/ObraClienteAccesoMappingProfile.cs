using AutoMapper;

using App.Core.DTOs.Obras;
using App.Models.Obras;

namespace App.Services.Mappings;

public class ObraClienteAccesoMappingProfile : Profile
{
    public ObraClienteAccesoMappingProfile()
    {
        CreateMap<ObraClienteAcceso, ObraClienteAccesoDto>()
            .ForMember(dest => dest.ExpiraEn, opt => opt.Ignore())
            .ForMember(dest => dest.Vigente, opt => opt.Ignore());
    }
}
