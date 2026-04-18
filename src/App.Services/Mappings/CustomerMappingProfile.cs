using AutoMapper;

using App.Core.DTOs.Customer;
using App.Models.Shared;

namespace App.Services.Mappings;

public class CustomerMappingProfile : Profile
{
    public CustomerMappingProfile()
    {
        // Customer entity → CustomerDto (includes nested FiscalProfile)
        CreateMap<Customer, CustomerDto>();

        // FiscalProfile entity → DTO
        CreateMap<CustomerFiscalProfile, CustomerFiscalProfileDto>();

        // Upsert DTO → FiscalProfile entity (used for both create and update)
        CreateMap<UpsertCustomerFiscalProfileDto, CustomerFiscalProfile>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CustomerId, opt => opt.Ignore())
            .ForMember(dest => dest.Customer, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedBy, opt => opt.Ignore());

        // CreateCustomerDto → Customer (commercial fields only)
        CreateMap<CreateCustomerDto, Customer>()
            .ForMember(dest => dest.FiscalProfile, opt => opt.Ignore())
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        // UpdateCustomerDto → Customer (commercial fields only)
        CreateMap<UpdateCustomerDto, Customer>()
            .ForMember(dest => dest.FiscalProfile, opt => opt.Ignore())
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}
