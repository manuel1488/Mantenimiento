using App.Core.Enums.Shop;

namespace App.Core.DTOs.Shop;

public class CashRegisterDenominationDto
{
    public long Id { get; set; }
    public long CashRegisterId { get; set; }
    public DenominationType DenominationType { get; set; }
    public decimal DenominationValue { get; set; }
    public int Quantity { get; set; }
    public decimal Subtotal => DenominationValue * Quantity;
}
