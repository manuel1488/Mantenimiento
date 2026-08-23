using App.Core.Common;
using App.Core.Interfaces;

namespace App.Services.Cotizaciones;

public class CotizacionIntegrityHashService : ICotizacionIntegrityHashService
{
    public string Compute(
        int? folioAnio,
        int? folioNumero,
        int clienteId,
        DateTime fechaGeneracionUtc,
        decimal subtotal,
        bool incluirIva,
        decimal ivaTasa,
        decimal ivaMonto,
        decimal total,
        IEnumerable<CotizacionIntegrityLinea> lineas) => CotizacionIntegrityHasher.Compute(
            folioAnio, folioNumero, clienteId, fechaGeneracionUtc,
            subtotal, incluirIva, ivaTasa, ivaMonto, total, lineas);
}
