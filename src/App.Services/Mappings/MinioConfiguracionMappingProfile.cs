using AutoMapper;

using App.Core.DTOs.Settings;
using App.Models.Settings;

namespace App.Services.Mappings;

public class MinioConfiguracionMappingProfile : Profile
{
    public MinioConfiguracionMappingProfile()
    {
        CreateMap<MinioConfiguracion, MinioConfiguracionDto>();
        CreateMap<UpdateMinioConfiguracionDto, MinioConfiguracion>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}
