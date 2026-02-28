namespace App.Core.DTOs.Billing.Mexico;

public class UpdateMexicoBillingPreferencesDto
{
    public string InvoiceSerie { get; set; } = "A";
    public long StartFolio { get; set; } = 1;
    public int FolioLength { get; set; } = 0;
    public bool AutoInvoicePromptEnabled { get; set; }
    public bool AllowEditFiscalDataInPrompt { get; set; } = true;
}
