using AutoMapper;

using App.Core.DTOs.Obras;
using App.Models.Obras;

namespace App.Services.Mappings;

public class ObraMappingProfile : Profile
{
    public ObraMappingProfile()
    {
        CreateMap<Obra, ObraDto>()
            .ForMember(dest => dest.ClienteNombre, opt => opt.MapFrom(src => src.Cliente.Nombre));

        CreateMap<CreateObraDto, Obra>();

        CreateMap<UpdateObraDto, Obra>();

        CreateMap<ObraFotoGeneral, ObraFotoGeneralDto>();
    }
}
