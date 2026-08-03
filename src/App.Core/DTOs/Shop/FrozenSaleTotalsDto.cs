namespace App.Core.DTOs.Shop;

/// <summary>
/// Pre-computed document-level totals that a sale must reproduce exactly, bypassing
/// re-derivation from its reconstructed line items. See <see cref="CreateSaleDto.FrozenTotals"/>.
/// </summary>
public class FrozenSaleTotalsDto
{
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
}
