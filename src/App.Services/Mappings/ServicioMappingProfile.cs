using AutoMapper;

using App.Core.DTOs.Servicios;
using App.Models.Servicios;

namespace App.Services.Mappings;

public class ServicioMappingProfile : Profile
{
    public ServicioMappingProfile()
    {
        CreateMap<Servicio, ServicioDto>()
            .ForMember(dest => dest.UnidadMedidaCodigo,
                opt => opt.MapFrom(src => src.UnidadMedida.Codigo))
            .ForMember(dest => dest.UnidadMedidaNombre,
                opt => opt.MapFrom(src => src.UnidadMedida.Nombre));

        CreateMap<CreateServicioDto, Servicio>();

        CreateMap<UpdateServicioDto, Servicio>();
    }
}
