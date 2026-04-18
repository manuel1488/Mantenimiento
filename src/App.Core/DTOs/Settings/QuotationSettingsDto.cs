namespace App.Core.DTOs.Settings;

public class QuotationSettingsDto
{
    public long Id { get; set; }

    // Payment terms
    public string? PaymentTermsText { get; set; }

    // Bank details
    public bool ShowBankDetails { get; set; }
    public string? BankBeneficiary { get; set; }
    public string? BankRfc { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankClabeNumber { get; set; }
    public string? BankSwift { get; set; }

    // Contact / social
    public bool ShowContactInfo { get; set; }
    public string? ContactWebsite { get; set; }
    public string? ContactFacebook { get; set; }
    public string? ContactInstagram { get; set; }
    public string? ContactWhatsapp { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }

    // Template
    public string? HtmlBody { get; set; }
    public string? CustomCss { get; set; }
}
