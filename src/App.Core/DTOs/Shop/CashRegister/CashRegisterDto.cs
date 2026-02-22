using App.Core.Enums.Shop;

namespace App.Core.DTOs.Shop;

public class CashRegisterDto
{
    public long Id { get; set; }
    public int LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public int? CashStationId { get; set; }
    public string? CashStationName { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public CashRegisterStatus Status { get; set; }
    public decimal InitialFund { get; set; }
    public string? OpeningNotes { get; set; }
    public string? ClosingNotes { get; set; }
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    // Computed financial totals
    public decimal TotalCashSales { get; set; }
    public decimal TotalDeposits { get; set; }
    public decimal TotalWithdrawals { get; set; }
    public decimal ExpectedCash { get; set; }
    public decimal CountedCash { get; set; }
    public decimal Difference { get; set; }

    // Payment method breakdown
    public List<PaymentMethodSummaryDto> PaymentSummary { get; set; } = [];

    // Movement history
    public List<CashRegisterMovementDto> Movements { get; set; } = [];
}
