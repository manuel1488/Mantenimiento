namespace App.Core.DTOs.Fiscal;

public class CreateUsoCfdiCatalogoDto
{
    public string Codigo { get; set; } = null!;
    public string Descripcion { get; set; } = null!;
    public string? CodigosRegimenFiscal { get; set; }
}
