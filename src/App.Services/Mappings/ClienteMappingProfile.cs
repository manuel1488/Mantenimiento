using AutoMapper;

using App.Core.DTOs.Clientes;
using App.Models.Clientes;

namespace App.Services.Mappings;

public class ClienteMappingProfile : Profile
{
    public ClienteMappingProfile()
    {
        CreateMap<Cliente, ClienteDto>();

        CreateMap<CreateClienteDto, Cliente>();

        CreateMap<UpdateClienteDto, Cliente>();
    }
}
