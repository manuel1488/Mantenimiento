using App.Core.Common;
using App.Core.DTOs.Cotizaciones;

namespace App.Core.Interfaces;

public interface ICotizacionService
{
    Task<Result<List<CotizacionDto>>> GetAllAsync();
    Task<Result<List<CotizacionDto>>> GetByClienteIdAsync(int clienteId);
    Task<Result<CotizacionDto>> GetByIdAsync(int id);
    Task<Result<CotizacionDto>> CreateAsync(CreateCotizacionDto dto);
    Task<Result<CotizacionDto>> AprobarAsync(int cotizacionId, AprobarCotizacionDto dto);
    Task<Result<CotizacionDto>> RechazarAsync(int cotizacionId);
    Task<Result> DeleteAsync(int cotizacionId);

    /// <summary>Renders the Cotización as a PDF using the effective (DB override or default) template.</summary>
    Task<Result<byte[]>> GetCotizacionPdfAsync(int cotizacionId, CancellationToken cancellationToken = default);
}
