using AutoMapper;

using App.Core.DTOs.Cotizaciones;
using App.Models.Cotizaciones;

namespace App.Services.Mappings;

public class CotizacionMappingProfile : Profile
{
    public CotizacionMappingProfile()
    {
        CreateMap<Cotizacion, CotizacionDto>()
            .ForMember(d => d.ClienteNombre, opt => opt.MapFrom(s => s.Cliente.Nombre));

        CreateMap<CotizacionLinea, CotizacionLineaDto>();
    }
}
