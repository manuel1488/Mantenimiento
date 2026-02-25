using App.Core.Enums.Shop;

namespace App.Core.DTOs.Shop;

public class CashRegisterMovementDto
{
    public long Id { get; set; }
    public long CashRegisterId { get; set; }
    public CashRegisterMovementType Type { get; set; }
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
    public int? WithdrawalNumber { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
