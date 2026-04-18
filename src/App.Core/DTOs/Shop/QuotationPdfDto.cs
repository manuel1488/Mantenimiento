namespace App.Core.DTOs.Shop;

public class QuotationPdfDto
{
    // ── Document metadata ──────────────────────────────────────────────────────
    public string QuotationNumber { get; set; } = null!;
    public DateTime QuoteDate { get; set; }
    public DateTime ValidUntil { get; set; }
    public string? Notes { get; set; }

    // ── Totals ─────────────────────────────────────────────────────────────────
    public decimal Subtotal { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public List<QuotationDetailDto> Details { get; set; } = [];

    // ── Company ────────────────────────────────────────────────────────────────
    public string CompanyName { get; set; } = null!;
    public string? LogoBase64 { get; set; }
    public string CurrencySymbol { get; set; } = "$";

    // ── Customer — commercial data ────────────────────────────────────────────
    public string CustomerName { get; set; } = null!;
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }
    /// <summary>Commercial/delivery address (from Customer entity).</summary>
    public string? CustomerAddress { get; set; }

    // ── Customer — fiscal data (from CustomerFiscalProfile, null if no profile) ──
    public bool CustomerHasFiscalData { get; set; }
    public string? CustomerTaxId { get; set; }
    public string? CustomerLegalName { get; set; }
    public string? CustomerFiscalRegime { get; set; }
    /// <summary>Fiscal address — may differ from commercial address.</summary>
    public string? CustomerFiscalAddress { get; set; }

    // ── Payment terms (null = section hidden) ─────────────────────────────────
    public string? PaymentTermsText { get; set; }

    // ── Bank / wire-transfer details ──────────────────────────────────────────
    public bool ShowBankDetails { get; set; }
    public string? BankBeneficiary { get; set; }
    public string? BankRfc { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankClabeNumber { get; set; }
    public string? BankSwift { get; set; }

    // ── Contact / social media footer ─────────────────────────────────────────
    public bool ShowContactInfo { get; set; }
    public string? ContactWebsite { get; set; }
    public string? ContactFacebook { get; set; }
    public string? ContactInstagram { get; set; }
    public string? ContactWhatsapp { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }

    // ── Template customisation ────────────────────────────────────────────────
    public string? HtmlBody { get; set; }
    public string? CustomCss { get; set; }
}
