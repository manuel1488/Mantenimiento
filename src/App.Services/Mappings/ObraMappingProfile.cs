using AutoMapper;

using App.Core.DTOs.Obras;
using App.Models.Obras;

namespace App.Services.Mappings;

public class ObraMappingProfile : Profile
{
    public ObraMappingProfile()
    {
        CreateMap<Obra, ObraDto>()
            .ForMember(dest => dest.ClienteNombre, opt => opt.MapFrom(src => src.Cliente.Nombre))
            .ForMember(dest => dest.PorcentajeAvance, opt => opt.MapFrom(src =>
                src.Actividades.Any() ? (int)src.Actividades.Average(a => a.PorcentajeAvance) : 0));

        CreateMap<CreateObraDto, Obra>();

        CreateMap<UpdateObraDto, Obra>();

        CreateMap<ObraFotoGeneral, ObraFotoGeneralDto>();

        CreateMap<ObraMensaje, ObraMensajeDto>()
            .ForMember(dest => dest.FotoPresignedUrl, opt => opt.Ignore())
            .ForMember(dest => dest.FotoThumbnailPresignedUrl, opt => opt.Ignore());
    }
}
