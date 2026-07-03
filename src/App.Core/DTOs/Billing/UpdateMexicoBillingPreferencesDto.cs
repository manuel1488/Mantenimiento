using App.Core.Enums.Billing;

namespace App.Core.DTOs.Billing.Mexico;

public class UpdateMexicoBillingPreferencesDto
{
    public string InvoiceSerie { get; set; } = "A";
    public long StartFolio { get; set; } = 1;
    public int FolioLength { get; set; } = 0;
    public string GlobalInvoiceSerie { get; set; } = "G";
    public long GlobalInvoiceStartFolio { get; set; } = 1;
    public int GlobalInvoiceFolioLength { get; set; } = 0;
    public bool AutoInvoicePromptEnabled { get; set; }
    public bool AllowEditFiscalDataInPrompt { get; set; } = true;
    public MultiPaymentFormPolicy MultiPaymentFormPolicy { get; set; } = MultiPaymentFormPolicy.UseHighestAmount;
    public bool AllowPdfRegenerationForStampedInvoices { get; set; }
}
