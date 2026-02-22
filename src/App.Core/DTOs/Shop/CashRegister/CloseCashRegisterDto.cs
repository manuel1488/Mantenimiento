namespace App.Core.DTOs.Shop;

public class CloseCashRegisterDto
{
    public long CashRegisterId { get; set; }
    public string? ClosingNotes { get; set; }
    public Dictionary<decimal, int> DenominationCounts { get; set; } = [];
}
