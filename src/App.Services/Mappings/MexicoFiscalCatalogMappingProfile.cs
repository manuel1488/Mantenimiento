using AutoMapper;

using App.Core.DTOs.Billing;
using App.Core.DTOs.Billing.Mexico;
using App.Models.Billing;

namespace App.Services.Mappings;

public class MexicoFiscalCatalogMappingProfile : Profile
{
    public MexicoFiscalCatalogMappingProfile()
    {
        // Fiscal Regimes
        CreateMap<MexicoFiscalRegime, MexicoFiscalRegimeDto>();
        CreateMap<CreateMexicoFiscalRegimeDto, MexicoFiscalRegime>();

        // Payment Forms
        CreateMap<MexicoPaymentForm, MexicoPaymentFormDto>();
        CreateMap<CreateMexicoPaymentFormDto, MexicoPaymentForm>();

        // Payment Methods
        CreateMap<MexicoPaymentMethod, MexicoPaymentMethodDto>();
        CreateMap<CreateMexicoPaymentMethodDto, MexicoPaymentMethod>();

        // CFDI Uses
        CreateMap<MexicoCfdiUse, MexicoCfdiUseDto>();
        CreateMap<CreateMexicoCfdiUseDto, MexicoCfdiUse>();

        // Product Services
        CreateMap<MexicoProductService, MexicoProductServiceDto>();
        CreateMap<CreateMexicoProductServiceDto, MexicoProductService>();

        // Update DTO
        CreateMap<UpdateMexicoFiscalCatalogDto, MexicoFiscalRegime>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        CreateMap<UpdateMexicoFiscalCatalogDto, MexicoPaymentForm>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        CreateMap<UpdateMexicoFiscalCatalogDto, MexicoPaymentMethod>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        CreateMap<UpdateMexicoFiscalCatalogDto, MexicoCfdiUse>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        CreateMap<UpdateMexicoFiscalCatalogDto, MexicoProductService>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}