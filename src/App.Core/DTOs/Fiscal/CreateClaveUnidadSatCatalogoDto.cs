namespace App.Core.DTOs.Fiscal;

public class CreateClaveUnidadSatCatalogoDto
{
    public string Codigo { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string? Simbolo { get; set; }
}
