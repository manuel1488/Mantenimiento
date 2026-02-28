namespace App.Core.DTOs.Billing.Mexico;

public class MexicoPacSettingsDto
{
    public int Id { get; set; }

    // PAC provider
    public string ProviderName { get; set; } = string.Empty;
    public string? User { get; set; }
    public bool HasPassword { get; set; }
    public bool HasToken { get; set; }
    public string ProductionUrl { get; set; } = string.Empty;
    public string? TestUrl { get; set; }
    public bool IsProduction { get; set; }

    // Invoice config — issuer fiscal data comes from TaxSettings (Fiscal tab)
    public string? InvoiceSerie { get; set; }
    public long StartFolio { get; set; } = 1;
    public int FolioLength { get; set; } = 0;

    // CSD certificate info
    public bool HasCsdCertificate { get; set; }
    public bool HasCsdPrivateKey { get; set; }
    public bool HasCsdPassword { get; set; }

    // Auto-invoice behavior
    public bool AutoInvoicePromptEnabled { get; set; }
    public bool AllowEditFiscalDataInPrompt { get; set; } = true;

    /// <summary>True when PAC credentials and CSD are present.</summary>
    public bool IsConfigured =>
        HasCsdCertificate &&
        HasCsdPrivateKey &&
        (HasToken || (!string.IsNullOrEmpty(User) && HasPassword));
}

public class UpdateMexicoPacSettingsDto
{
    public string ProviderName { get; set; } = "SW Sapien";
    public string? User { get; set; }
    public string? Password { get; set; }
    public string? Token { get; set; }
    public string ProductionUrl { get; set; } = "https://services.sw.com.mx";
    public string? TestUrl { get; set; } = "https://services.test.sw.com.mx";
    public bool IsProduction { get; set; }
    public string? InvoiceSerie { get; set; } = "A";
    public long StartFolio { get; set; } = 1;
    public int FolioLength { get; set; } = 0;
    public string? CsdCertificateBase64 { get; set; }
    public string? CsdPrivateKeyBase64 { get; set; }
    public string? CsdPassword { get; set; }
    public bool AutoInvoicePromptEnabled { get; set; }
    public bool AllowEditFiscalDataInPrompt { get; set; } = true;
}
