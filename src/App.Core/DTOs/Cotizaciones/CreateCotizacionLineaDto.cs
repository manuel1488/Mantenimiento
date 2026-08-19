using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Cotizaciones;

public class CreateCotizacionLineaDto
{
    [Required]
    public int ServicioId { get; set; }

    [Required]
    [Range(0.001, double.MaxValue)]
    public decimal Cantidad { get; set; }

    /// <summary>Si es nulo, se usa el PrecioUnitario actual del catálogo de Servicio.</summary>
    public decimal? PrecioUnitarioOverride { get; set; }
}
