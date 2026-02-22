using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Core.Interfaces;

namespace App.Models.Shop;

[Table("sh_cash_stations")]
public class CashStation : IAuditableEntity
{
    public int Id { get; set; }

    public int LocationId { get; set; }

    [MaxLength(100)]
    public string Name { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    [ForeignKey(nameof(LocationId))]
    public virtual Location Location { get; set; } = null!;

    public virtual ICollection<CashRegister> CashRegisters { get; set; } = [];

    // IAuditableEntity
    public string CreatedBy { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
}
