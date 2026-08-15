namespace App.Web.Components.Admin.Servicios.UnidadesMedida.Models;

public class UnidadMedidaDialogModel
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int? ClaveUnidadSatId { get; set; }
    public string? ClaveUnidadSatCodigo { get; set; }
    public string? ClaveUnidadSatNombre { get; set; }
}
