namespace App.Core.DTOs.Servicios;

public class ServicioDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }
    public int UnidadMedidaId { get; set; }
    public string UnidadMedidaCodigo { get; set; } = null!;
    public string UnidadMedidaNombre { get; set; } = null!;
    public decimal PrecioUnitario { get; set; }
    public decimal RendimientoDiasPorUnidad { get; set; }
}
