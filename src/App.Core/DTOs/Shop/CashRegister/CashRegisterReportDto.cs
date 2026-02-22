namespace App.Core.DTOs.Shop;

public class CashRegisterReportDto
{
    // Header
    public long CashRegisterId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string CashierName { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    // Financial summary
    public decimal InitialFund { get; set; }
    public decimal TotalCashSales { get; set; }
    public decimal TotalDeposits { get; set; }
    public decimal TotalWithdrawals { get; set; }
    public decimal ExpectedCash { get; set; }
    public decimal CountedCash { get; set; }
    public decimal Difference { get; set; }

    // Payment breakdown
    public List<PaymentMethodSummaryDto> PaymentSummary { get; set; } = [];

    // Movements detail
    public List<CashRegisterMovementDto> Movements { get; set; } = [];

    // Denomination detail
    public List<CashRegisterDenominationDto> Denominations { get; set; } = [];

    // Notes
    public string? OpeningNotes { get; set; }
    public string? ClosingNotes { get; set; }
}
