using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Base;
using App.Core.Interfaces;
using App.Models.Subcontratistas;
using App.Models.Tecnicos;

namespace App.Models.Obras;

/// <summary>
/// Historial de reasignaciones de Técnico/Subcontratista de una Actividad (RN-12).
/// En cada lado (anterior/nuevo), solo uno de TecnicoId/SubcontratistaId debe estar establecido.
/// </summary>
[Table("obr_actividad_reasignaciones")]
public class ActividadReasignacion : BaseEntity<int>, IAuditTracked
{
    [Required]
    public int ActividadId { get; set; }
    public Actividad Actividad { get; set; } = null!;

    public int? TecnicoAnteriorId { get; set; }
    public Tecnico? TecnicoAnterior { get; set; }

    public int? SubcontratistaAnteriorId { get; set; }
    public Subcontratista? SubcontratistaAnterior { get; set; }

    public int? TecnicoNuevoId { get; set; }
    public Tecnico? TecnicoNuevo { get; set; }

    public int? SubcontratistaNuevoId { get; set; }
    public Subcontratista? SubcontratistaNuevo { get; set; }

    [Required]
    [StringLength(500)]
    public string Motivo { get; set; } = null!;

    [Required]
    public DateTime Fecha { get; set; }
}
