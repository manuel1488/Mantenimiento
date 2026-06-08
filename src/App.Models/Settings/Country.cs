using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Core.Base;
using App.Core.Interfaces;

namespace App.Models.Settings;

[Table("stg_countries")]
public class Country : BaseEntity<int>, IAuditTracked
{
    [Required]
    [StringLength(2)]
    public string Code { get; set; } = null!;

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = null!;

    [Required]
    [StringLength(3)]
    public string DefaultCurrencyCode { get; set; } = null!;

    public bool IsActive { get; set; }
}