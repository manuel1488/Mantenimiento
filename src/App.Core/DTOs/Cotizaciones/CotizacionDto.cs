using App.Core.Enums.Cotizaciones;

namespace App.Core.DTOs.Cotizaciones;

public class CotizacionDto
{
    public int Id { get; set; }
    public int ObraId { get; set; }
    public int Version { get; set; }
    public DateTime FechaGeneracion { get; set; }
    public decimal Total { get; set; }
    public CotizacionEstado Estado { get; set; }
    public DateTime? FechaAprobacion { get; set; }
    public string? AprobadaPor { get; set; }
    public string? MedioAprobacion { get; set; }
    public List<CotizacionLineaDto> Lineas { get; set; } = [];
}
