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

    /// <summary>Si es nulo, se usa el RendimientoDiasPorUnidad actual del catálogo de Servicio.</summary>
    public decimal? RendimientoDiasPorUnidadOverride { get; set; }

    /// <summary>Si es nulo, se usa la Descripcion actual del catálogo de Servicio.</summary>
    [StringLength(3000)]
    public string? Descripcion { get; set; }
}
