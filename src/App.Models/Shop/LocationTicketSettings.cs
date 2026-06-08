using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Base;
using App.Core.Interfaces;

namespace App.Models.Shop;

/// <summary>
/// Ticket printing and display settings per location
/// One configuration per location for sales tickets
/// </summary>
[Table("sh_location_ticket_settings")]
public class LocationTicketSettings : BaseEntity<int>, IAuditTracked
{
    [Required]
    public int LocationId { get; set; }

    // Printer configuration
    [StringLength(200)]
    public string? PrinterName { get; set; }

    public int PaperWidth { get; set; } = 80; // mm (58, 80)

    public bool AutoPrint { get; set; } = false;

    public int Copies { get; set; } = 1;

    // Content customization
    [StringLength(500)]
    public string? HeaderText { get; set; }

    [StringLength(500)]
    public string? FooterText { get; set; }

    [Column(TypeName = "LONGTEXT")]
    public string? LogoBase64 { get; set; }

    public bool ShowLogo { get; set; } = true;

    // Fiscal information (overrides global if specified)
    [StringLength(20)]
    public string? TaxId { get; set; } // RFC

    [StringLength(200)]
    public string? LegalName { get; set; } // Razón social

    // Address display
    public bool ShowFullAddress { get; set; } = true;

    // QR Code
    public bool ShowQrCode { get; set; } = false;

    [StringLength(500)]
    public string? QrCodeContent { get; set; }

    // Additional options
    public bool ShowPrices { get; set; } = true;

    public bool ShowTaxBreakdown { get; set; } = true;

    // Navigation properties
    [ForeignKey(nameof(LocationId))]
    public virtual Location Location { get; set; } = null!;
}
