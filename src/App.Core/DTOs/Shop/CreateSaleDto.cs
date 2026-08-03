using System.ComponentModel.DataAnnotations;
using App.Core.Constants;

namespace App.Core.DTOs.Shop;

public class CreateSaleDto
{
    [Required]
    public long CustomerId { get; set; }

    public DateTime? SaleDate { get; set; }

    public SaleType SaleType { get; set; } = SaleType.Public;

    public long? QuotationId { get; set; }

    public int? LocationId { get; set; }

    /// <summary>
    /// Whether cash-rounding should be applied to the document total. Set to
    /// <c>false</c> when the sale originates from a document whose total was
    /// already frozen without rounding (a converted quotation or a consolidated
    /// remission) — the customer already paid that exact locked amount, and
    /// applying rounding here would make the payment appear insufficient.
    /// </summary>
    public bool ApplyRounding { get; set; } = true;

    /// <summary>
    /// Forces the sale's document-level totals (Subtotal, DiscountAmount, TaxAmount, Total)
    /// to these exact values instead of re-deriving them from the reconstructed line items.
    /// Set when converting a document whose total was already frozen and paid (a consolidated
    /// remission) — line items only persist 2-decimal precision, so re-deriving totals from
    /// them can drift a cent from the frozen amount. When set, this is authoritative.
    /// </summary>
    public FrozenSaleTotalsDto? FrozenTotals { get; set; }

    [Range(0, 100)]
    public decimal DiscountPercentage { get; set; } = 0;

    public string? DiscountAuthorizedBy { get; set; }
    public string? DiscountAuthorizerId { get; set; }
    public DateTime? DiscountAuthorizedAt { get; set; }

    [Required]
    [MinLength(1)]
    public List<CreateSalePaymentDto> Payments { get; set; } = new();

    [Required]
    public List<CreateSaleDetailDto> Details { get; set; } = new();
}
