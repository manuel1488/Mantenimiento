namespace App.Core.DTOs.Fiscal;

public class UsoCfdiCatalogoDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = null!;
    public string Descripcion { get; set; } = null!;
    public string? CodigosRegimenFiscal { get; set; }
}
