using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Cotizaciones;

public class UpdateCotizacionDto
{
    [Required]
    public int ClienteId { get; set; }

    public bool IncluirIva { get; set; }

    [Required]
    [MinLength(1)]
    public List<CreateCotizacionLineaDto> Lineas { get; set; } = [];
}
