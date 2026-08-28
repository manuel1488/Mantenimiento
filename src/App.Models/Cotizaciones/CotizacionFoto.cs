using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Base;
using App.Core.Interfaces;

namespace App.Models.Cotizaciones;

/// <summary>Foto asociada a una Cotización. El archivo y su miniatura se guardan en MinIO vía
/// <see cref="Core.Interfaces.IFileStorageService"/> — aquí solo se persiste la clave del objeto
/// y su metadata.</summary>
[Table("cot_cotizacion_fotos")]
public class CotizacionFoto : BaseEntity<int>, IAuditTracked
{
    [Required]
    public int CotizacionId { get; set; }
    public Cotizacion Cotizacion { get; set; } = null!;

    /// <summary>Clave del objeto en MinIO (no una ruta de disco).</summary>
    [Required]
    [StringLength(1000)]
    public string FileKey { get; set; } = null!;

    /// <summary>
    /// Clave de la miniatura en MinIO, generada junto con la imagen completa.
    /// Nula si la miniatura falló al subirse — la UI hace fallback a <see cref="FileKey"/>.
    /// </summary>
    [StringLength(1000)]
    public string? ThumbnailFileKey { get; set; }

    [Required]
    [StringLength(100)]
    public string MimeType { get; set; } = null!;

    /// <summary>Tamaño en bytes de la imagen ya comprimida (no del archivo original subido).</summary>
    [Required]
    public long FileSize { get; set; }

    [StringLength(3000)]
    public string? Descripcion { get; set; }

    [Required]
    public DateTime FechaCarga { get; set; }
}
