namespace App.Core.DTOs.Cotizaciones;

public class CotizacionLineaDto
{
    public int Id { get; set; }
    public int ServicioId { get; set; }
    public string ServicioNombre { get; set; } = null!;
    public string? Descripcion { get; set; }
    public string UnidadMedida { get; set; } = null!;
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
    public decimal RendimientoDiasPorUnidad { get; set; }
    public decimal TiempoEstimadoDias { get; set; }
}
