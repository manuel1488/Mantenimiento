using App.Core.Common;
using App.Core.DTOs.Shop;

namespace App.Core.Interfaces.Shop;

/// <summary>
/// Service for managing product wholesale discount configurations.
/// </summary>
public interface IProductWholesalePriceService
{
    /// <summary>
    /// Gets all wholesale discount configurations for a product.
    /// </summary>
    Task<Result<IList<ProductWholesalePriceDto>>> GetWholesalePricesForProductAsync(long productId);

    /// <summary>
    /// Gets the discount percentage for a specific product and tier.
    /// </summary>
    /// <returns>The discount percentage, or 0 if not configured.</returns>
    Task<Result<decimal>> GetDiscountPercentageAsync(long productId, int tierId);

    /// <summary>
    /// Updates wholesale discount configurations for a product (bulk update).
    /// Replaces all existing configurations with the new ones.
    /// </summary>
    Task<Result> UpdateProductWholesalePricesAsync(UpdateProductWholesalePricesDto dto);
}
