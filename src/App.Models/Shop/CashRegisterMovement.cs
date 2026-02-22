using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Core.Base;
using App.Core.Enums.Shop;

namespace App.Models.Shop;

[Table("sh_cash_register_movements")]
public class CashRegisterMovement : BaseEntity<long>
{
    public long CashRegisterId { get; set; }

    public CashRegisterMovementType MovementType { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(500)]
    public string Reason { get; set; } = null!;

    [ForeignKey(nameof(CashRegisterId))]
    public virtual CashRegister CashRegister { get; set; } = null!;
}
