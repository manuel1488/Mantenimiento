using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Base;

namespace App.Models.Shop;


[Table("sh_warehouses")]
public class Warehouse : BaseEntity<int>
{
    [Required]
    [StringLength(50)]
    public string Name { get; set; } = null!;

    [StringLength(200)]
    public string? Description { get; set; }

    [Required]
    public bool IsActive { get; set; }

    // Branch relationship (optional)
    public int? BranchId { get; set; }

    [ForeignKey(nameof(BranchId))]
    public virtual Branch? Branch { get; set; }
}