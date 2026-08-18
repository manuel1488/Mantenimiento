namespace App.Web.Components.Admin.Servicios.Models;

public class ServicioDialogModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int UnidadMedidaId { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal RendimientoDiasPorUnidad { get; set; }
    public int? ClaveProdServSatId { get; set; }
    public string? ClaveProdServSatCodigo { get; set; }
    public string? ClaveProdServSatDescripcion { get; set; }
}
