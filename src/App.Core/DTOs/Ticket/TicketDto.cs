using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Ticket;

public class TicketDto<T>
{
    /// <summary>
    /// The main data object (e.g., SaleDto)
    /// </summary>
    public T Data { get; set; } = default!;

    /// <summary>
    /// Company name to display on the ticket
    /// </summary>
    [Required]
    [StringLength(100)]
    public string CompanyName { get; set; } = string.Empty;
    
    /// <summary>
    /// Company logo in Base64 format
    /// </summary>
    public string? CompanyLogoBase64 { get; set; }
    
    /// <summary>
    /// Company address to display on the ticket
    /// </summary>
    [StringLength(200)]
    public string? CompanyAddress { get; set; }
    
    /// <summary>
    /// Company phone number
    /// </summary>
    [StringLength(20)]
    public string? CompanyPhone { get; set; }
    
    /// <summary>
    /// Company tax ID (RFC in Mexico)
    /// </summary>
    [StringLength(20)]
    public string? CompanyTaxId { get; set; }
    
    /// <summary>
    /// Whether to show QR code on the ticket
    /// </summary>
    public bool ShowQRCode { get; set; } = true;
    
    /// <summary>
    /// Whether to show company logo on the ticket
    /// </summary>
    public bool ShowCompanyLogo { get; set; } = true;
    
    /// <summary>
    /// Custom header text to display on the ticket
    /// </summary>
    public string? CustomHeader { get; set; }
    
    /// <summary>
    /// Custom footer text to display on the ticket
    /// </summary>
    public string? CustomFooter { get; set; }
    
    /// <summary>
    /// QR code data in Base64 format
    /// </summary>
    public string? QRCodeData { get; set; }
    
    /// <summary>
    /// Ticket width in millimeters (for thermal printers)
    /// </summary>
    public int TicketWidth { get; set; } = 80;
    
    /// <summary>
    /// Number of copies to print by default
    /// </summary>
    public int Copies { get; set; } = 1;
    
    /// <summary>
    /// Company timezone for date formatting
    /// </summary>
    public TimeZoneInfo? TimeZone { get; set; }

    /// <summary>
    /// When true, prices are displayed with tax included (visual only — total is unchanged).
    /// </summary>
    public bool ShowPricesWithTax { get; set; } = false;

    /// <summary>
    /// Effective tax rate (e.g. 0.16 for 16%). Used for IVA-inclusive price display.
    /// </summary>
    public decimal TaxRate { get; set; }
}