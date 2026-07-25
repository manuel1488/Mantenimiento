namespace App.Core.DTOs.Inventory;

public class PhysicalCountLineDto
{
    public long ProductId { get; set; }
    public decimal CountedQuantity { get; set; }
}

public class CreatePhysicalInventoryCountDto
{
    public int LocationId { get; set; }
    public string Reason { get; set; } = null!;
    public string? Reference { get; set; }
    public List<PhysicalCountLineDto> Lines { get; set; } = new();
}

public class PhysicalCountLineResultDto
{
    public long ProductId { get; set; }
    public string ProductCode { get; set; } = null!;
    public string ProductName { get; set; } = null!;
    public decimal SystemQuantity { get; set; }
    public decimal CountedQuantity { get; set; }
    public decimal Difference { get; set; }
    public bool MovementApplied { get; set; }
}

public class PhysicalInventoryCountResultDto
{
    public long Id { get; set; }
    public Guid BatchId { get; set; }
    public string BatchNumber { get; set; } = null!;
    public int LocationId { get; set; }
    public string LocationName { get; set; } = null!;
    public DateTime CountDate { get; set; }
    public string CreatedBy { get; set; } = null!;
    public string Reason { get; set; } = null!;
    public string? Reference { get; set; }
    public List<PhysicalCountLineResultDto> Lines { get; set; } = new();
}
