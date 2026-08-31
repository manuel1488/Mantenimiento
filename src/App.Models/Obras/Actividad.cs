using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Base;
using App.Core.Enums.Obras;
using App.Core.Interfaces;
using App.Models.Servicios;
using App.Models.Subcontratistas;
using App.Models.Tecnicos;

namespace App.Models.Obras;

[Table("obr_actividades")]
public class Actividad : BaseEntity<int>, IAuditTracked
{
    [Required]
    public int ObraId { get; set; }
    public Obra Obra { get; set; } = null!;

    [Required]
    public int ServicioId { get; set; }
    public Servicio Servicio { get; set; } = null!;

    /// <summary>
    /// Descripción de esta Actividad, copiada del catálogo de Servicio al agregarla pero editable
    /// libremente por Actividad sin afectar el catálogo — un snapshot igual que PrecioUnitario/
    /// RendimientoDiasPorUnidad, que preserva lo capturado aunque el Servicio cambie después.
    /// </summary>
    [StringLength(500)]
    public string? Descripcion { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,3)")]
    public decimal Cantidad { get; set; }

    /// <summary>
    /// Copiado del catálogo de Servicio al agregar la Actividad; ajustable manualmente por el Coordinador.
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal PrecioUnitario { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Costo { get; set; }

    /// <summary>
    /// Copiado del catálogo de Servicio al agregar la Actividad; ajustable manualmente por el Coordinador.
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal RendimientoDiasPorUnidad { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal TiempoEstimadoDias { get; set; }

    // Asignación: solo uno de los dos debe estar establecido (validado en la capa de servicio).
    public int? TecnicoId { get; set; }
    public Tecnico? Tecnico { get; set; }

    public int? SubcontratistaId { get; set; }
    public Subcontratista? Subcontratista { get; set; }

    [Required]
    public ActividadEstado Estado { get; set; } = ActividadEstado.Pendiente;

    [Range(0, 100)]
    public int PorcentajeAvance { get; set; }

    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }

    public ICollection<ActividadEvidenciaFoto> Evidencias { get; set; } = new List<ActividadEvidenciaFoto>();
    public ICollection<ActividadReasignacion> Reasignaciones { get; set; } = new List<ActividadReasignacion>();
    public ICollection<ActividadAvanceRegistro> AvanceRegistros { get; set; } = new List<ActividadAvanceRegistro>();
}
