namespace App.Web.Components.Admin.Servicios.Models;

public class ServicioDialogModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string UnidadMedida { get; set; } = string.Empty;
    public decimal PrecioUnitario { get; set; }
    public decimal RendimientoDiasPorUnidad { get; set; }
}
