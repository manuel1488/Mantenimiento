namespace App.Core.Models.Cfdi.V40;

/// <summary>
/// Helper for formatting CFDI values according to SAT specifications (MXN only).
/// </summary>
public static class CfdiFormatHelper
{
    private static readonly System.Globalization.CultureInfo Invariant =
        System.Globalization.CultureInfo.InvariantCulture;

    /// <summary>Formats a monetary amount with 2 decimal places (MXN standard). Used at document level.</summary>
    public static string FormatAmount(decimal amount) => amount.ToString("F2", Invariant);

    /// <summary>Formats a line-level amount with 6 decimal places (SAT Anexo 20 allows up to 6 for Concepto).</summary>
    public static string FormatLineAmount(decimal amount) => amount.ToString("F6", Invariant);

    /// <summary>Formats a unit price with 6 decimal places (SAT line item precision).</summary>
    public static string FormatUnitPrice(decimal amount) => amount.ToString("F6", Invariant);

    /// <summary>Formats a quantity with 6 decimal places.</summary>
    public static string FormatQuantity(decimal qty) => qty.ToString("F6", Invariant);

    /// <summary>Formats a tax rate with 6 decimal places.</summary>
    public static string FormatRate(decimal rate) => rate.ToString("F6", Invariant);
}
