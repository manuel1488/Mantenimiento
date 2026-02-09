using AutoMapper;
using App.Core.DTOs.Ticket;
using App.Models.Settings;

namespace App.Services.Mappings.Shop;

public class TicketProfile : Profile
{
    public TicketProfile()
    {
        // TicketConfiguration -> TicketConfigurationDto
        CreateMap<TicketConfiguration, TicketConfigurationDto>();
        
        // UpdateTicketConfigurationDto -> TicketConfiguration (para actualización)
        CreateMap<UpdateTicketConfigurationDto, TicketConfiguration>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}