namespace App.Core.DTOs.Fiscal;

public class ClaveUnidadSatCatalogoDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string? Simbolo { get; set; }
}
