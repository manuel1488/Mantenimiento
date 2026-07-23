namespace App.Core.DTOs.Inventory;

public class BulkTransferLineResultDto
{
    public long ProductId { get; set; }
    public string ProductCode { get; set; } = null!;
    public string ProductName { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal PreviousBalance { get; set; }
    public decimal NewBalance { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
}

public class BulkTransferResultDto
{
    public Guid BatchId { get; set; }
    public int LocationId { get; set; }
    public string LocationName { get; set; } = null!;
    public int DestinationLocationId { get; set; }
    public string DestinationLocationName { get; set; } = null!;
    public string TransferType { get; set; } = null!;
    public string Reason { get; set; } = null!;
    public string? Reference { get; set; }
    public DateTime MovementDate { get; set; }
    public string CreatedBy { get; set; } = null!;
    public List<BulkTransferLineResultDto> Lines { get; set; } = new();
}
