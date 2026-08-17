namespace App.Core.DTOs.Obras;

public class UpdateActividadDto
{
    public int Id { get; set; }
    public decimal Cantidad { get; set; }
    public decimal? PrecioUnitarioOverride { get; set; }
    public decimal? RendimientoDiasPorUnidadOverride { get; set; }
}
