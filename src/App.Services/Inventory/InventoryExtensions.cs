namespace App.Services.Inventory;

internal static class InventoryExtensions
{
    /// <summary>
    /// Returns the stock in individual units (e.g. liters) derived exclusively from
    /// <see cref="App.Models.Shop.Inventory.Quantity"/> × <see cref="App.Models.Shop.Product.Content"/>.
    /// This is the single source of truth — never read a stored field for this calculation.
    /// </summary>
    internal static decimal GetAvailableIndividualUnits(this App.Models.Shop.Inventory inventory)
    {
        if (inventory.Product.IsPartialSaleAllowed && inventory.Product.Content > 0)
            return inventory.Quantity * inventory.Product.Content;

        return inventory.Quantity;
    }
}
