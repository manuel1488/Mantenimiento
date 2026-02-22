using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Core.Base;
using App.Core.Enums.Shop;

namespace App.Models.Shop;

[Table("sh_cash_registers")]
public class CashRegister : BaseEntity<long>
{
    public int LocationId { get; set; }

    [Required]
    [StringLength(450)]
    public string UserId { get; set; } = null!;

    public CashRegisterStatus Status { get; set; } = CashRegisterStatus.Open;

    [Column(TypeName = "decimal(10,2)")]
    public decimal InitialFund { get; set; }

    [StringLength(500)]
    public string? OpeningNotes { get; set; }

    [StringLength(500)]
    public string? ClosingNotes { get; set; }

    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    public int? CashStationId { get; set; }

    [ForeignKey(nameof(LocationId))]
    public virtual Location Location { get; set; } = null!;

    [ForeignKey(nameof(CashStationId))]
    public virtual CashStation? CashStation { get; set; }

    public virtual ICollection<CashRegisterMovement> Movements { get; set; } = [];
    public virtual ICollection<CashRegisterDenomination> Denominations { get; set; } = [];
    public virtual ICollection<Sale> Sales { get; set; } = [];
}
