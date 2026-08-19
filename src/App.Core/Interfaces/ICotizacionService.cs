using App.Core.Common;
using App.Core.DTOs.Cotizaciones;

namespace App.Core.Interfaces;

public interface ICotizacionService
{
    Task<Result<CotizacionDto>> GenerarAsync(int obraId);
    Task<Result<CotizacionDto>> AprobarAsync(int cotizacionId, AprobarCotizacionDto dto);
    Task<Result<CotizacionDto>> RechazarAsync(int cotizacionId);
    Task<Result<CotizacionDto?>> GetLatestByObraIdAsync(int obraId);

    /// <summary>Renders the Cotización as a PDF using the effective (DB override or default) template.</summary>
    Task<Result<byte[]>> GetCotizacionPdfAsync(int cotizacionId, CancellationToken cancellationToken = default);
}
