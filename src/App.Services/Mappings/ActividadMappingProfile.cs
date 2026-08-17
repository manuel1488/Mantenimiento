using AutoMapper;

using App.Core.DTOs.Obras;
using App.Models.Obras;

namespace App.Services.Mappings;

public class ActividadMappingProfile : Profile
{
    public ActividadMappingProfile()
    {
        CreateMap<Actividad, ActividadDto>()
            .ForMember(dest => dest.ServicioNombre, opt => opt.MapFrom(src => src.Servicio.Nombre))
            .ForMember(dest => dest.UnidadMedidaNombre, opt => opt.MapFrom(src => src.Servicio.UnidadMedida.Nombre));

        CreateMap<ActividadEvidenciaFoto, ActividadEvidenciaFotoDto>();
    }
}
