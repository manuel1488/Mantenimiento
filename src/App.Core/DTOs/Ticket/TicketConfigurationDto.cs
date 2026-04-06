namespace App.Core.DTOs.Ticket;

public class TicketConfigurationDto
{
    public string CompanyName { get; set; } = string.Empty;
    public string? CompanyLogoBase64 { get; set; }
    public string? CompanyAddress { get; set; }
    public string? CompanyPhone { get; set; }
    public string? CompanyTaxId { get; set; }
    
    public bool ShowQRCode { get; set; } = true;
    public bool ShowCompanyLogo { get; set; } = true;
    
    public string? CustomHeader { get; set; }
    public string? CustomFooter { get; set; }
    
    public int TicketWidth { get; set; } = 80; // Ancho en mm
    public int DefaultCopies { get; set; } = 1;

    public bool DirectPrintEnabled { get; set; } = false;
    public int PrintFlushDelayMs { get; set; } = 500;
    public int PrintChunkSize { get; set; } = 2048;
    public int PortSettlingDelayMs { get; set; } = 250;

    public bool CashDrawerEnabled { get; set; } = false;
    public string CashDrawerCommand { get; set; } = "1B 70 00 19 FA";
}