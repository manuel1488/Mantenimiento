namespace App.Core.DTOs.Shop;

public class WithdrawalTicketDataDto
{
    public long MovementId { get; set; }
    public int? WithdrawalNumber { get; set; }
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
    public string CashierName { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public DateTime CashRegisterOpenedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime PrintedAt { get; set; }
}
