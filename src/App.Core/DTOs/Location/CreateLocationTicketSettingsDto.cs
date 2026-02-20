using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Location;

public class CreateLocationTicketSettingsDto
{
    [Required]
    public int LocationId { get; set; }

    // Printer configuration
    [StringLength(200)]
    public string? PrinterName { get; set; }

    [Range(58, 80)]
    public int PaperWidth { get; set; } = 80;

    public bool AutoPrint { get; set; } = false;

    [Range(1, 10)]
    public int Copies { get; set; } = 1;

    // Content customization
    [StringLength(500)]
    public string? HeaderText { get; set; }

    [StringLength(500)]
    public string? FooterText { get; set; }

    public string? LogoBase64 { get; set; }

    public bool ShowLogo { get; set; } = true;

    // Fiscal information
    [StringLength(20)]
    public string? TaxId { get; set; }

    [StringLength(200)]
    public string? LegalName { get; set; }

    // Display options
    public bool ShowFullAddress { get; set; } = true;
    public bool ShowQrCode { get; set; } = false;

    [StringLength(500)]
    public string? QrCodeContent { get; set; }

    public bool ShowPrices { get; set; } = true;
    public bool ShowTaxBreakdown { get; set; } = true;
}
