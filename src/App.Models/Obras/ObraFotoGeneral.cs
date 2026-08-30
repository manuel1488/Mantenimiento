using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Base;
using App.Core.Interfaces;

namespace App.Models.Obras;

/// <summary>Foto general de una Obra, no asociada a ninguna Actividad en particular. El archivo y su
/// miniatura se guardan en el almacenamiento vía <see cref="Core.Interfaces.IFileStorageService"/> —
/// aquí solo se persiste la clave del objeto y su metadata.</summary>
[Table("obr_obra_fotos_generales")]
public class ObraFotoGeneral : BaseEntity<int>, IAuditTracked
{
    [Required]
    public int ObraId { get; set; }
    public Obra Obra { get; set; } = null!;

    [Required]
    [StringLength(500)]
    public string RutaArchivo { get; set; } = null!;

    /// <summary>
    /// Clave de la miniatura en el almacenamiento, generada junto con la imagen completa.
    /// Nula si la miniatura falló al subirse — la UI hace fallback a <see cref="RutaArchivo"/>.
    /// </summary>
    [StringLength(500)]
    public string? RutaArchivoThumbnail { get; set; }

    [StringLength(3000)]
    public string? Descripcion { get; set; }

    [Required]
    public DateTime FechaCarga { get; set; }
}
