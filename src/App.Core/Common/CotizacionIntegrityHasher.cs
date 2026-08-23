using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace App.Core.Common;

/// <summary>
/// Computes a SHA-256 fingerprint over a Cotización's business data (folio, cliente, totales,
/// líneas). Stored on the Cotización at creation/edit time so it reflects a specific saved
/// snapshot — recomputing it later from the same stored values must always reproduce the same
/// hash, and any direct alteration of the stored data (bypassing the app) will not match it.
/// This is an integrity fingerprint, not a cryptographic signature: it does not use a private key
/// and cannot be verified by third parties without access to this same algorithm.
/// </summary>
public static class CotizacionIntegrityHasher
{
    public static string Compute(
        int? folioAnio,
        int? folioNumero,
        int clienteId,
        DateTime fechaGeneracionUtc,
        decimal subtotal,
        bool incluirIva,
        decimal ivaTasa,
        decimal ivaMonto,
        decimal total,
        IEnumerable<CotizacionIntegrityLinea> lineas)
    {
        var sb = new StringBuilder();

        Append(sb, folioAnio);
        Append(sb, folioNumero);
        Append(sb, clienteId);
        Append(sb, fechaGeneracionUtc.ToString("O", CultureInfo.InvariantCulture));
        Append(sb, subtotal);
        Append(sb, incluirIva);
        Append(sb, ivaTasa);
        Append(sb, ivaMonto);
        Append(sb, total);

        foreach (var linea in lineas)
        {
            Append(sb, linea.ServicioNombre);
            Append(sb, linea.UnidadMedida);
            Append(sb, linea.Cantidad);
            Append(sb, linea.PrecioUnitario);
            Append(sb, linea.Subtotal);
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static void Append(StringBuilder sb, object? value) =>
        sb.Append(Convert.ToString(value, CultureInfo.InvariantCulture)).Append('|');
}

public record CotizacionIntegrityLinea(
    string ServicioNombre,
    string UnidadMedida,
    decimal Cantidad,
    decimal PrecioUnitario,
    decimal Subtotal);
