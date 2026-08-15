using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Servicios;

public class UpdateServicioDto
{
    [Required]
    [StringLength(150)]
    public string Nombre { get; set; } = null!;

    [StringLength(500)]
    public string? Descripcion { get; set; }

    [Required]
    public int UnidadMedidaId { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal PrecioUnitario { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal RendimientoDiasPorUnidad { get; set; }
}
