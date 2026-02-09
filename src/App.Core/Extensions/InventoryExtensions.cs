using App.Core.Resources;

namespace App.Core.Extensions;

public static class InventoryExtensions
{
    public static string GetLocalizedInventoryDescription(this string value)
    {
        return value.GetLocalizedDescription(ResourceTypes.Constants.Inventory);
    }
}