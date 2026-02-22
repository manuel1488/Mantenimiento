namespace App.Core.DTOs.Shop;

public class OpenCashRegisterDto
{
    public int LocationId { get; set; }
    public int CashStationId { get; set; }
    public decimal InitialFund { get; set; }
    public string? Notes { get; set; }
}
