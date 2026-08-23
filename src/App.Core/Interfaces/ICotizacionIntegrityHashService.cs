using App.Core.Common;

namespace App.Core.Interfaces;

/// <summary>
/// Computes the integrity fingerprint stored on a Cotización (see CotizacionIntegrityHasher for the
/// exact algorithm and what it does/doesn't guarantee). Wrapped as a service — rather than calling
/// the static hasher directly — so callers (CotizacionService) can be unit-tested with a mock.
/// </summary>
public interface ICotizacionIntegrityHashService
{
    string Compute(
        int? folioAnio,
        int? folioNumero,
        int clienteId,
        DateTime fechaGeneracionUtc,
        decimal subtotal,
        bool incluirIva,
        decimal ivaTasa,
        decimal ivaMonto,
        decimal total,
        IEnumerable<CotizacionIntegrityLinea> lineas);
}
