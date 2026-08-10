using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Base;
using App.Core.Interfaces;
using App.Models.Obras;

namespace App.Models.Facturas;

/// <summary>
/// Registro de facturación (documento) y pago; el timbrado real con un PAC queda fuera del MVP.
/// </summary>
[Table("fac_facturas")]
public class Factura : BaseEntity<int>, IAuditTracked
{
    [Required]
    public int ObraId { get; set; }
    public Obra Obra { get; set; } = null!;

    [Required]
    [StringLength(50)]
    public string Folio { get; set; } = null!;

    [Required]
    public DateTime FechaEmision { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Total { get; set; }

    [Required]
    [StringLength(50)]
    public string FormaPago { get; set; } = null!;

    public bool Pagada { get; set; }

    public DateTime? FechaPago { get; set; }
}
