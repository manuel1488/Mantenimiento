using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Cotizaciones;

public class AprobarCotizacionDto
{
    [Required]
    [StringLength(150)]
    public string AprobadaPor { get; set; } = null!;

    [Required]
    [StringLength(200)]
    public string MedioAprobacion { get; set; } = null!;
}
