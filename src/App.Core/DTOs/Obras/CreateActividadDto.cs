namespace App.Core.DTOs.Obras;

public class CreateActividadDto
{
    public int ObraId { get; set; }
    public int ServicioId { get; set; }
    public decimal Cantidad { get; set; }
    public decimal? PrecioUnitarioOverride { get; set; }
    public decimal? RendimientoDiasPorUnidadOverride { get; set; }
}
