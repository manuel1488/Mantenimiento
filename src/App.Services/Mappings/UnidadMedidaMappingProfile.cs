using AutoMapper;

using App.Core.DTOs.Servicios;
using App.Models.Servicios;

namespace App.Services.Mappings;

public class UnidadMedidaMappingProfile : Profile
{
    public UnidadMedidaMappingProfile()
    {
        CreateMap<UnidadMedida, UnidadMedidaDto>()
            .ForMember(dest => dest.ClaveUnidadSatCodigo,
                opt => opt.MapFrom(src => src.ClaveUnidadSat != null ? src.ClaveUnidadSat.Codigo : null))
            .ForMember(dest => dest.ClaveUnidadSatNombre,
                opt => opt.MapFrom(src => src.ClaveUnidadSat != null ? src.ClaveUnidadSat.Nombre : null));

        CreateMap<CreateUnidadMedidaDto, UnidadMedida>();

        CreateMap<UpdateUnidadMedidaDto, UnidadMedida>();
    }
}
