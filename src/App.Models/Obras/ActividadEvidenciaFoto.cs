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

    [Required]
    public DateTime FechaCarga { get; set; }
}
