namespace App.Core.DTOs.Ticket;

public class UpdateTicketConfigurationDto
{
    public string? CompanyName { get; set; }
    public string? CompanyLogoBase64 { get; set; }
    public string? CompanyAddress { get; set; }
    public string? CompanyPhone { get; set; }
    public string? CompanyTaxId { get; set; }
    
    public bool? ShowQRCode { get; set; }
    public bool? ShowCompanyLogo { get; set; }
    
    public string? CustomHeader { get; set; }
    public string? CustomFooter { get; set; }
    
    public int? TicketWidth { get; set; }
    public int? DefaultCopies { get; set; }
}