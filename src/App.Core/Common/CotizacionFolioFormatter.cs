namespace App.Core.Common;

/// <summary>
/// Formats a Cotización folio from its stored year/consecutive number plus the configurable
/// prefix/padding. Cotizaciones created before the folio feature existed have no FolioAnio/FolioNumero
/// and fall back to "#{Id}".
/// </summary>
public static class CotizacionFolioFormatter
{
    public const string DefaultPrefijo = "COT";
    public const int DefaultDigitos = 4;

    public static string Format(int id, int? folioAnio, int? folioNumero, string? prefijo, int? digitos)
    {
        if (folioAnio is null || folioNumero is null)
            return $"#{id}";

        var effectivePrefijo = string.IsNullOrWhiteSpace(prefijo) ? DefaultPrefijo : prefijo;
        var effectiveDigitos = digitos is > 0 ? digitos.Value : DefaultDigitos;

        return $"{effectivePrefijo}-{folioAnio}-{folioNumero.Value.ToString().PadLeft(effectiveDigitos, '0')}";
    }
}
