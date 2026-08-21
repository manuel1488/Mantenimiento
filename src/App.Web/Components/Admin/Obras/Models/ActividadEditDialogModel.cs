namespace App.Web.Components.Admin.Obras.Models;

public class ActividadEditDialogModel
{
    public int Id { get; set; }
    public string ServicioNombre { get; set; } = string.Empty;
    public string UnidadMedidaCodigo { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public decimal? PrecioUnitarioOverride { get; set; }
    public decimal? RendimientoDiasPorUnidadOverride { get; set; }
}
