using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Cotizaciones;

public class FirmarCotizacionDto
{
    [Required]
    [StringLength(150)]
    public string FirmanteNombre { get; set; } = null!;

    /// <summary>Data URL del canvas de firma (ej. "data:image/png;base64,...").</summary>
    [Required]
    public string SignatureDataUrl { get; set; } = null!;
}
