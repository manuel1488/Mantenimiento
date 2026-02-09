using App.Core.Common;
using App.Core.DTOs.Shop;

namespace App.Core.Interfaces.Shop;

/// <summary>
/// Service for managing product partial surcharge configurations.
/// </summary>
public interface IProductPartialSurchargeService
{
    /// <summary>
    /// Gets all surcharge configurations for a product.
    /// </summary>
    Task<Result<IList<ProductPartialSurchargeDto>>> GetSurchargesForProductAsync(long productId);

    /// <summary>
    /// Gets the surcharge percentage for a specific product and fraction.
    /// </summary>
    /// <returns>The surcharge percentage, or 0 if not configured.</returns>
    Task<Result<decimal>> GetSurchargePercentageAsync(long productId, int fractionId);

    /// <summary>
    /// Updates surcharge configurations for a product (bulk update).
    /// Replaces all existing configurations with the new ones.
    /// </summary>
    Task<Result> UpdateProductSurchargesAsync(UpdateProductPartialSurchargesDto dto);

    /// <summary>
    /// Calculates the final price for a fractional sale, including surcharge.
    /// </summary>
    /// <param name="productId">The product being sold.</param>
    /// <param name="quantity">The quantity being sold (in individual units, e.g., liters).</param>
    /// <param name="productContent">The product's content (e.g., 19 for a 19L container).</param>
    /// <param name="productPrice">The product's full unit price.</param>
    /// <param name="fractionId">The selected fraction ID (optional).</param>
    /// <returns>A calculation result with base price, surcharge, and final price.</returns>
    Task<Result<FractionalPriceCalculationDto>> CalculateFractionalPriceAsync(
        long productId,
        decimal quantity,
        decimal productContent,
        decimal productPrice,
        int? fractionId = null);
}
