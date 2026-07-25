namespace App.Core.DTOs.Inventory;

public class BulkTransferPdfLineDto
{
    public string ProductCode { get; set; } = null!;
    public string ProductName { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal PreviousBalance { get; set; }
    public decimal NewBalance { get; set; }
}

public class BulkTransferPdfDto
{
    public string CompanyName { get; set; } = null!;
    public string LogoBase64 { get; set; } = string.Empty;
    public Guid BatchId { get; set; }
    public string BatchNumber { get; set; } = null!;
    public string LocationName { get; set; } = null!;
    public string DestinationLocationName { get; set; } = null!;
    public string TransferTypeDisplay { get; set; } = null!;
    public string Reason { get; set; } = null!;
    public string? Reference { get; set; }
    public DateTime MovementDate { get; set; }
    public string CreatedBy { get; set; } = null!;
    public List<BulkTransferPdfLineDto> Lines { get; set; } = new();
}
