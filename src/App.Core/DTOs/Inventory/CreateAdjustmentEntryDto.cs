namespace App.Core.DTOs.Inventory;

public class CreateAdjustmentEntryDto
{
    public string AdjustmentType { get; set; } = null!;
    public int LocationId { get; set; }
    public string? Reference { get; set; }
    public string Reason { get; set; } = null!;
    public DateTime? AdjustmentDate { get; set; }
    public List<CreateAdjustmentEntryItemDto> Items { get; set; } = [];
}
