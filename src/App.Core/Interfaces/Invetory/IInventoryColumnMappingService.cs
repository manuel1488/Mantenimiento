namespace App.Core.Interfaces;

public interface IInventoryColumnMappingService
{
    Dictionary<string, string> GetColumnMappingForCurrentCulture();
    Dictionary<string, string> GetColumnMappingForCulture(string cultureName);
    Dictionary<string, string> GetReverseMapping();
}