namespace App.Core.DTOs.Inventory;

public class CreateStockEntryDto
{
    public string MovementType { get; set; } = null!;
    public string MovementSubType { get; set; } = null!;
    public int LocationId { get; set; }
    public long? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public string? DocumentNumber { get; set; }
    public string? Reference { get; set; }
    public string Reason { get; set; } = null!;
    public DateTime? EntryDate { get; set; }
    public string? AttachmentFileName { get; set; }
    public string? AttachmentMimeType { get; set; }
    public byte[]? AttachmentData { get; set; }
    public List<CreateStockEntryItemDto> Items { get; set; } = [];
}
