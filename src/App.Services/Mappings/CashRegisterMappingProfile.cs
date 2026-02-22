using AutoMapper;
using App.Core.DTOs.Shop;
using App.Models.Settings;
using App.Models.Shop;

namespace App.Services.Mappings;

public class CashRegisterMappingProfile : Profile
{
    public CashRegisterMappingProfile()
    {
        CreateMap<CashRegister, CashRegisterDto>()
            .ForMember(d => d.LocationName, o => o.MapFrom(s => s.Location != null ? s.Location.Name : string.Empty))
            .ForMember(d => d.CashStationName, o => o.Ignore())
            .ForMember(d => d.UserName, o => o.Ignore())
            .ForMember(d => d.TotalCashSales, o => o.Ignore())
            .ForMember(d => d.TotalDeposits, o => o.Ignore())
            .ForMember(d => d.TotalWithdrawals, o => o.Ignore())
            .ForMember(d => d.ExpectedCash, o => o.Ignore())
            .ForMember(d => d.CountedCash, o => o.Ignore())
            .ForMember(d => d.Difference, o => o.Ignore())
            .ForMember(d => d.PaymentSummary, o => o.Ignore())
            .ForMember(d => d.Movements, o => o.MapFrom(s => s.Movements));

        CreateMap<CashRegisterMovement, CashRegisterMovementDto>()
            .ForMember(d => d.Type, o => o.MapFrom(s => s.MovementType));

        CreateMap<CashRegisterDenomination, CashRegisterDenominationDto>();

        CreateMap<CashRegisterSettings, CashRegisterSettingsDto>();
    }
}
