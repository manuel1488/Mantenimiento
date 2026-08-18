using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Base;
using App.Core.Enums.Obras;
using App.Core.Interfaces;

namespace App.Models.Obras;

[Table("obr_actividad_evidencias")]
public class ActividadEvidenciaFoto : BaseEntity<int>, IAuditTracked
{
    [Required]
    public int ActividadId { get; set; }
    public Actividad Actividad { get; set; } = null!;

    [Required]
    public TipoEvidencia Tipo { get; set; }

    [Required]
    [StringLength(500)]
    public string RutaArchivo { get; set; } = null!;

    /// <summary>
    /// Clave de la miniatura en el almacenamiento, generada junto con la imagen completa.
    /// Nula en filas cargadas antes de que existiera este campo — la UI hace fallback a <see cref="RutaArchivo"/>.
    /// </summary>
    [StringLength(500)]
    public string? RutaArchivoThumbnail { get; set; }

    [Required]
    public DateTime FechaCarga { get; set; }
}
