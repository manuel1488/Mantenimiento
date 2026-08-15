namespace App.Core.DTOs.Servicios;

public class UnidadMedidaDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }
    public int? ClaveUnidadSatId { get; set; }
    public string? ClaveUnidadSatCodigo { get; set; }
    public string? ClaveUnidadSatNombre { get; set; }
}
