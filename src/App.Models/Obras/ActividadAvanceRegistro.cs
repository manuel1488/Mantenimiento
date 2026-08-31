using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Base;
using App.Core.Interfaces;

namespace App.Models.Obras;

/// <summary>
/// Bitácora de cada registro de avance de una Actividad: porcentaje al momento, observación y foto
/// opcionales capturadas por quien registró el avance. Es la fuente de la notificación al cliente.
/// </summary>
[Table("obr_actividad_avance_registros")]
public class ActividadAvanceRegistro : BaseEntity<int>, IAuditTracked
{
    [Required]
    public int ActividadId { get; set; }
    public Actividad Actividad { get; set; } = null!;

    [Range(0, 100)]
    public int PorcentajeAvance { get; set; }

    [StringLength(1000)]
    public string? Observaciones { get; set; }

    [StringLength(500)]
    public string? RutaArchivoFoto { get; set; }

    [StringLength(500)]
    public string? RutaArchivoFotoThumbnail { get; set; }

    [Required]
    public DateTime FechaRegistro { get; set; }
}
