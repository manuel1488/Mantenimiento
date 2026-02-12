namespace App.Core.DTOs.Shop;

/// <summary>
/// DTO for product wholesale discount configuration.
/// </summary>
public class ProductWholesalePriceDto
{
    public long Id { get; set; }

    public long ProductId { get; set; }

    public int WholesaleTierId { get; set; }

    /// <summary>
    /// Tier display name.
    /// </summary>
    public string TierName { get; set; } = null!;

    /// <summary>
    /// Minimum quantity to qualify for this tier.
    /// </summary>
    public decimal MinQuantity { get; set; }

    /// <summary>
    /// Discount percentage (0-100) over the product's base price.
    /// </summary>
    public decimal DiscountPercentage { get; set; }

    /// <summary>
    /// Whether this configuration is active.
    /// </summary>
    public bool IsActive { get; set; }
}
