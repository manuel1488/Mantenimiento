namespace App.Web.Components.Admin.Obras.Models;

public class ActividadAvanceDialogModel
{
    public int Id { get; set; }
    public string ServicioNombre { get; set; } = string.Empty;
    public int PorcentajeAvance { get; set; }
    public string? Observaciones { get; set; }
    public byte[]? FotoData { get; set; }
    public string? FotoContentType { get; set; }
    public string? FotoFileName { get; set; }
}
