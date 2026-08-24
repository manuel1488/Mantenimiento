using AutoMapper;

using App.Core.DTOs.Cotizaciones;
using App.Models.Cotizaciones;

namespace App.Services.Mappings;

public class CotizacionMappingProfile : Profile
{
    public CotizacionMappingProfile()
    {
        CreateMap<Cotizacion, CotizacionDto>()
            .ForMember(d => d.ClienteNombre, opt => opt.MapFrom(s => s.Cliente.Nombre))
            .ForMember(d => d.ClienteCorreo, opt => opt.MapFrom(s => s.Cliente.Correo))
            .ForMember(d => d.ObraGeneradaId, opt => opt.MapFrom(s => s.ObraGenerada != null ? s.ObraGenerada.Id : (int?)null));

        CreateMap<CotizacionLinea, CotizacionLineaDto>();
        CreateMap<CotizacionFoto, CotizacionFotoDto>();
    }
}
