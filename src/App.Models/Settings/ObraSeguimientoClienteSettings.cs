using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Core.Base;
using App.Core.Interfaces;

namespace App.Models.Settings;

[Table("stg_obra_seguimiento_cliente_settings")]
public class ObraSeguimientoClienteSettings : BaseEntity<int>, IAuditTracked
{
    /// <summary>
    /// Días que el enlace público de seguimiento del Cliente sigue vigente después de que la Obra
    /// pasa a Finalizada (ver <see cref="Obras.Obra.FechaFinalizacion"/>). Mientras la Obra no esté
    /// Finalizada el enlace no expira por fecha — solo por deshabilitación manual.
    /// </summary>
    [Required]
    public int DiasVigenciaPostFinalizacion { get; set; } = 90;
}
