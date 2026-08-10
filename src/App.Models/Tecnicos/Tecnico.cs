using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Base;
using App.Core.Interfaces;

namespace App.Models.Tecnicos;

[Table("tec_tecnicos")]
public class Tecnico : BaseEntity<int>, IAuditTracked
{
    [Required]
    [StringLength(150)]
    public string Nombre { get; set; } = null!;

    [StringLength(30)]
    public string? Telefono { get; set; }

    [StringLength(150)]
    public string? Correo { get; set; }

    public bool Activo { get; set; } = true;
}
