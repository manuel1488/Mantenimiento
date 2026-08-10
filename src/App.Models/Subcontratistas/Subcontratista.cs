using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Base;
using App.Core.Interfaces;

namespace App.Models.Subcontratistas;

[Table("sub_subcontratistas")]
public class Subcontratista : BaseEntity<int>, IAuditTracked
{
    [Required]
    [StringLength(150)]
    public string Nombre { get; set; } = null!;

    [StringLength(150)]
    public string? Contacto { get; set; }

    [StringLength(30)]
    public string? Telefono { get; set; }

    [StringLength(150)]
    public string? Correo { get; set; }

    public bool Activo { get; set; } = true;
}
