using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Base;
using App.Core.Interfaces;

namespace App.Models.Obras;

/// <summary>
/// Enlace público de solo lectura ("vista de Cliente") de una Obra: 1:1, identificado por un token
/// no adivinable. Se genera automáticamente al crear la Obra. El Coordinador puede deshabilitarlo
/// (<see cref="Habilitado"/>) o regenerarlo (nuevo <see cref="Token"/>, invalidando el anterior) sin
/// afectar al otro. La vigencia por fecha no vive aquí — se calcula en tiempo de consulta a partir
/// de <see cref="Obra.FechaFinalizacion"/> más los días configurables en
/// <see cref="Settings.ObraSeguimientoClienteSettings"/>.
/// </summary>
[Table("obr_obra_cliente_accesos")]
public class ObraClienteAcceso : BaseEntity<int>, IAuditTracked
{
    [Required]
    public int ObraId { get; set; }
    public Obra Obra { get; set; } = null!;

    [Required]
    [StringLength(64)]
    public string Token { get; set; } = null!;

    public bool Habilitado { get; set; } = true;

    [Required]
    public DateTime TokenGeneradoEn { get; set; }
}
