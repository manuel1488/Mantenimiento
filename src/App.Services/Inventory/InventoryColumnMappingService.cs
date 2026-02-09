using System.Globalization;

using App.Core.Interfaces;

using Microsoft.Extensions.Localization;

namespace App.Services.Inventory;

public class InventoryColumnMappingService : IInventoryColumnMappingService
{
    private readonly IStringLocalizer<InventoryColumnMappingService> _localizer;
    private readonly IStringLocalizerFactory _localizerFactory;

    // Definir las claves que deben existir en los archivos .resx
    private readonly string[] _columnKeys = new[]
    {
        "ProductCode",
        "Quantity",
        "MinStock",
        "MaxStock"
    };

    public InventoryColumnMappingService(
        IStringLocalizer<InventoryColumnMappingService> localizer,
        IStringLocalizerFactory localizerFactory)
    {
        _localizer = localizer;
        _localizerFactory = localizerFactory;
    }

    public Dictionary<string, string> GetColumnMappingForCurrentCulture()
    {
        var mapping = new Dictionary<string, string>();

        foreach (var key in _columnKeys)
        {
            var translatedValue = _localizer[key].Value;
            mapping[key] = translatedValue;
        }

        return mapping;
    }

    public Dictionary<string, string> GetColumnMappingForCulture(string cultureName)
    {
        var mapping = new Dictionary<string, string>();

        // Crear un localizer para la cultura específica
        var cultureLocalizer = _localizerFactory.Create(typeof(InventoryColumnMappingService));

        // Cambiar temporalmente la cultura para obtener las traducciones
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUICulture = CultureInfo.CurrentUICulture;

        try
        {
            var targetCulture = new CultureInfo(cultureName);
            CultureInfo.CurrentCulture = targetCulture;
            CultureInfo.CurrentUICulture = targetCulture;

            foreach (var key in _columnKeys)
            {
                var translatedValue = cultureLocalizer[key].Value;
                mapping[key] = translatedValue;
            }
        }
        finally
        {
            // Restaurar la cultura original
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUICulture;
        }

        return mapping;
    }

    public Dictionary<string, string> GetReverseMapping()
    {
        // Crea el mapeo inverso: Header Traducido -> Property Name
        var forwardMapping = GetColumnMappingForCurrentCulture();
        var reverseMapping = new Dictionary<string, string>();

        foreach (var kvp in forwardMapping)
        {
            reverseMapping[kvp.Value] = kvp.Key;
        }

        return reverseMapping;
    }
}