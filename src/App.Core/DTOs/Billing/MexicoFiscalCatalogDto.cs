namespace App.Core.DTOs.Billing.Mexico;

public abstract class MexicoFiscalCatalogDto
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Description { get; set; } = null!;
}