using App.Core.DTOs.Shared;

namespace App.Core.DTOs.Inventory;

public class InventoryMovementDto : AuditableDto
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public string BrandName { get; set; } = null!;
    public string? ProductDescription { get; set; }
    public string ProductCode { get; set; } = null!;
    public int LocationId { get; set; }
    public string LocationName { get; set; } = null!;
    public int? DestinationLocationId { get; set; }
    public string? DestinationLocationName { get; set; }
    public string MovementType { get; set; } = null!;
    public string MovementSubType { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal IndividualUnits { get; set; }
    public string? Reference { get; set; }
    public string Reason { get; set; } = null!;
    public decimal PreviousBalance { get; set; }
    public decimal NewBalance { get; set; }
    public decimal PreviousIndividualBalance { get; set; }
    public decimal NewIndividualBalance { get; set; }
    public string UnitMeasureName { get; set; } = null!;
    public decimal ProductContent { get; set; }
    public decimal TotalContent => Quantity * ProductContent;
    public DateTime MovementDate { get; set; }
    public Guid? BatchId { get; set; }
}