using AutoMapper;

using App.Core.DTOs.Servicios;
using App.Models.Servicios;

namespace App.Services.Mappings;

public class ServicioMappingProfile : Profile
{
    public ServicioMappingProfile()
    {
        CreateMap<Servicio, ServicioDto>();

        CreateMap<CreateServicioDto, Servicio>();

        CreateMap<UpdateServicioDto, Servicio>();
    }
}
