using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Core.Base;

namespace App.Models.Settings;

[Table("stg_ticket_configuration")]
public class TicketConfiguration : BaseEntity<int>
{
    [Required]
    [StringLength(100)]
    public string CompanyName { get; set; } = string.Empty;
    
    public string? CompanyLogoBase64 { get; set; }
    
    [StringLength(200)]
    public string? CompanyAddress { get; set; }
    
    [StringLength(20)]
    public string? CompanyPhone { get; set; }
    
    [StringLength(20)]
    public string? CompanyTaxId { get; set; }
    
    public bool ShowQRCode { get; set; } = true;
    
    public bool ShowCompanyLogo { get; set; } = true;
    
    public string? CustomHeader { get; set; }
    
    public string? CustomFooter { get; set; }
    
    public int TicketWidth { get; set; } = 80; // Ancho en mm
    
    public int DefaultCopies { get; set; } = 1;

    // Direct thermal printing via Web Serial API
    public bool DirectPrintEnabled { get; set; } = false;

    /// <summary>
    /// Milliseconds to wait after writing ESC/POS bytes before closing the serial port.
    /// Increase when printing logos or large receipts to avoid truncation.
    /// </summary>
    public int PrintFlushDelayMs { get; set; } = 200;

    // Cash drawer
    public bool CashDrawerEnabled { get; set; } = false;

    /// <summary>
    /// ESC/POS cash drawer command as space-separated hex bytes, e.g. "1B 70 00 19 FA".
    /// Sent via the same COM port as the printer after each completed sale.
    /// </summary>
    [StringLength(100)]
    public string CashDrawerCommand { get; set; } = "1B 70 00 19 FA";
}