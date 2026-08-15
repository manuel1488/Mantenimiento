using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Servicios;

public class CreateUnidadMedidaDto
{
    [Required]
    [StringLength(20)]
    public string Codigo { get; set; } = null!;

    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = null!;

    [StringLength(200)]
    public string? Descripcion { get; set; }

    public int? ClaveUnidadSatId { get; set; }
}
