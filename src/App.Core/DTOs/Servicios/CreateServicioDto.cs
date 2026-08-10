using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Servicios;

public class CreateServicioDto
{
    [Required]
    [StringLength(150)]
    public string Nombre { get; set; } = null!;

    [StringLength(500)]
    public string? Descripcion { get; set; }

    [Required]
    [StringLength(20)]
    public string UnidadMedida { get; set; } = null!;

    [Required]
    [Range(0, double.MaxValue)]
    public decimal PrecioUnitario { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal RendimientoDiasPorUnidad { get; set; }
}
