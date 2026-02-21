using App.Core.DTOs.Settings;
using App.Models.Settings;
using AutoMapper;

namespace App.Services.Mappings.Settings;

public class PaymentMethodMappingProfile : Profile
{
    public PaymentMethodMappingProfile()
    {
        CreateMap<PaymentMethod, PaymentMethodDto>();
        CreateMap<CreatePaymentMethodDto, PaymentMethod>();
        CreateMap<UpdatePaymentMethodDto, PaymentMethod>();
    }
}
