namespace App.Core.Interfaces;

/// <summary>
/// Reserves the year/consecutive pair used to build an Obra's folio (see
/// <see cref="Common.CotizacionFolioFormatter"/>, reused as-is since the formatting logic is
/// entity-agnostic). The consecutive resets to 1 on each new año.
/// </summary>
public interface IObraFolioService
{
    Task<(int Anio, int Numero)> GenerarSiguienteFolioAsync(CancellationToken cancellationToken = default);
}
