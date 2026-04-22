using App.Core.DTOs.Shared;
using App.Core.Enums.Shop;

namespace App.Core.DTOs.Shop;

public class QuotationDto : AuditableDto
{
    public long Id { get; set; }
    public string QuotationNumber { get; set; } = null!;
    public long CustomerId { get; set; }
    public string CustomerName { get; set; } = null!;
    public string? CustomerEmail { get; set; }
    public DateTime QuoteDate { get; set; }
    public DateTime ValidUntil { get; set; }
    public QuotationStatus Status { get; set; }
    public string? Notes { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public DateTime? SentAt { get; set; }
    public string? SentToEmail { get; set; }
    public long? ConvertedSaleId { get; set; }
    public long? ConvertedRemissionId { get; set; }
    public string? RejectionReason { get; set; }
    public List<QuotationDetailDto> Details { get; set; } = [];
}
