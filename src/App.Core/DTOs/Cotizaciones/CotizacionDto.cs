using App.Core.Enums.Cotizaciones;

namespace App.Core.DTOs.Cotizaciones;

public class CotizacionDto
{
    public int Id { get; set; }
    public int? FolioAnio { get; set; }
    public int? FolioNumero { get; set; }
    public int ClienteId { get; set; }
    public string ClienteNombre { get; set; } = null!;
    public string? ClienteCorreo { get; set; }
    public DateTime FechaGeneracion { get; set; }
    public decimal Subtotal { get; set; }
    public bool IncluirIva { get; set; }
    public decimal IvaTasa { get; set; }
    public decimal IvaMonto { get; set; }
    public decimal Total { get; set; }
    public CotizacionEstado Estado { get; set; }
    public DateTime? FechaAprobacion { get; set; }
    public string? AprobadaPor { get; set; }
    public string? MedioAprobacion { get; set; }
    public List<CotizacionLineaDto> Lineas { get; set; } = [];
    public string IntegridadHash { get; set; } = string.Empty;
}
