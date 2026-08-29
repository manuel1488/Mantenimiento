using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Base;
using App.Core.Interfaces;

namespace App.Models.Cotizaciones;

/// <summary>Firma electrónica autógrafa del cliente que acepta una Cotización. La imagen se guarda en
/// MinIO vía <see cref="Core.Interfaces.IFileStorageService"/> — aquí solo se persiste la clave del
/// objeto y su metadata, igual que <see cref="CotizacionFoto"/>.</summary>
[Table("cot_cotizacion_firmas")]
public class CotizacionFirma : BaseEntity<int>, IAuditTracked
{
    [Required]
    public int CotizacionId { get; set; }
    public Cotizacion Cotizacion { get; set; } = null!;

    [Required]
    [StringLength(150)]
    public string FirmanteNombre { get; set; } = null!;

    /// <summary>Clave del objeto en MinIO (no una ruta de disco).</summary>
    [Required]
    [StringLength(1000)]
    public string FileKey { get; set; } = null!;

    [Required]
    [StringLength(100)]
    public string ContentType { get; set; } = "image/png";

    [Required]
    public DateTime FechaFirma { get; set; }
}
