using App.Core.DTOs.Shared;

namespace App.Core.DTOs.Location;

public class LocationTicketSettingsDto : AuditableDto
{
    public int Id { get; set; }
    public int LocationId { get; set; }
    public string? LocationName { get; set; }

    // Printer configuration
    public string? PrinterName { get; set; }
    public int PaperWidth { get; set; } = 80;
    public bool AutoPrint { get; set; } = false;
    public int Copies { get; set; } = 1;

    // Content customization
    public string? HeaderText { get; set; }
    public string? FooterText { get; set; }
    public string? LogoBase64 { get; set; }
    public bool ShowLogo { get; set; } = true;

    // Fiscal information
    public string? TaxId { get; set; }
    public string? LegalName { get; set; }

    // Display options
    public bool ShowFullAddress { get; set; } = true;
    public bool ShowQrCode { get; set; } = false;
    public string? QrCodeContent { get; set; }
    public bool ShowPrices { get; set; } = true;
    public bool ShowTaxBreakdown { get; set; } = true;
}
