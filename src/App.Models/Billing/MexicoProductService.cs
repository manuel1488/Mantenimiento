using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Core.Base;

namespace App.Models.Billing;

[Table("mx_product_services")]
public class MexicoProductService : BaseEntity<int>
{
    [Required]
    [StringLength(10)]
    public string Code { get; set; } = null!;

    [Required]
    [StringLength(250)]
    public string Description { get; set; } = null!;
}