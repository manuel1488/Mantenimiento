using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Base;

namespace App.Models.Shop;

[Table("sh_branches")]
public class Branch : BaseEntity<int>
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = null!;

    [StringLength(500)]
    public string? Description { get; set; }

    // Address
    [StringLength(200)]
    public string? Street { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(100)]
    public string? State { get; set; }

    [StringLength(20)]
    public string? ZipCode { get; set; }

    [StringLength(3)]
    public string? Country { get; set; }

    // Contact
    [StringLength(20)]
    public string? Phone { get; set; }

    [StringLength(100)]
    public string? Email { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<Warehouse> Warehouses { get; set; } = new List<Warehouse>();
}
