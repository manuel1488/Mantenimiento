namespace App.Web.Components.Admin.Obras.Models;

public class ObraDialogModel
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public string Direccion { get; set; } = string.Empty;
    public bool Urgente { get; set; }
}
