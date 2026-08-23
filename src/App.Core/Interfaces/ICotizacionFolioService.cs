namespace App.Core.Interfaces;

/// <summary>
/// Reserves the year/consecutive pair used to build a Cotización's folio (see
/// <see cref="Common.CotizacionFolioFormatter"/>). The consecutive resets to 1 on each new año.
/// </summary>
public interface ICotizacionFolioService
{
    Task<(int Anio, int Numero)> GenerarSiguienteFolioAsync(CancellationToken cancellationToken = default);
}
