using System.ComponentModel.DataAnnotations.Schema;
using App.Core.Base;
using App.Core.Enums.Shop;

namespace App.Models.Shop;

[Table("sh_cash_register_denominations")]
public class CashRegisterDenomination : BaseEntity<long>
{
    public long CashRegisterId { get; set; }

    public DenominationType DenominationType { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal DenominationValue { get; set; }

    public int Quantity { get; set; }

    [ForeignKey(nameof(CashRegisterId))]
    public virtual CashRegister CashRegister { get; set; } = null!;
}
