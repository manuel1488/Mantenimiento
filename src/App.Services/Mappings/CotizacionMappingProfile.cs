using AutoMapper;

using App.Core.DTOs.Cotizaciones;
using App.Models.Cotizaciones;

namespace App.Services.Mappings;

public class CotizacionMappingProfile : Profile
{
    public CotizacionMappingProfile()
    {
        CreateMap<Cotizacion, CotizacionDto>();
        CreateMap<CotizacionLinea, CotizacionLineaDto>();
    }
}
