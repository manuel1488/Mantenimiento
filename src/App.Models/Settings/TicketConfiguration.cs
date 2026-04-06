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
    /// Covers the printer's mechanical processing time (print head, cutter).
    /// </summary>
    public int PrintFlushDelayMs { get; set; } = 500;

    /// <summary>
    /// Milliseconds to wait after opening a fresh COM port before sending data.
    /// The Epson TM Virtual Port Driver asserts DTR/RTS on open and needs time to
    /// complete its internal USB initialization cycle. Bytes sent before this
    /// window expires are silently discarded, causing the first print to be
    /// truncated. 250 ms covers worst-case user-mode driver init on Windows.
    /// </summary>
    public int PortSettlingDelayMs { get; set; } = 250;

    /// <summary>
    /// Maximum bytes per Web Serial write chunk.  The TM-T20IV has a 4 KB
    /// receive buffer; keeping chunks below that lets USB back-pressure
    /// pace the data flow and prevents buffer overflows on large receipts.
    /// </summary>
    public int PrintChunkSize { get; set; } = 2048;

    // Cash drawer
    public bool CashDrawerEnabled { get; set; } = false;

    /// <summary>
    /// ESC/POS cash drawer command as space-separated hex bytes, e.g. "1B 70 00 19 FA".
    /// Sent via the same COM port as the printer after each completed sale.
    /// </summary>
    [StringLength(100)]
    public string CashDrawerCommand { get; set; } = "1B 70 00 19 FA";
}