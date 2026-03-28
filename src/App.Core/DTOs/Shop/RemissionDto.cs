using App.Core.DTOs.Shared;
using App.Core.Enums.Shop;

namespace App.Core.DTOs.Shop;

public class RemissionDto : AuditableDto
{
    public long Id { get; set; }
    public string RemissionNumber { get; set; } = null!;
    public long CustomerId { get; set; }
    public string CustomerName { get; set; } = null!;
    public DateTime RemissionDate { get; set; }
    public int LocationId { get; set; }
    public string LocationName { get; set; } = null!;
    public RemissionStatus Status { get; set; }
    public string? Notes { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public long? QuotationId { get; set; }
    public string? QuotationNumber { get; set; }

    public long? ConsolidatedSaleId { get; set; }
    public DateTime? ConsolidatedAt { get; set; }
    public string? ConsolidatedBy { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime? CancelledAt { get; set; }
    public List<RemissionDetailDto> Details { get; set; } = [];
}
