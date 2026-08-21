using App.Core.Common;
using App.Core.DTOs.Cotizaciones;

namespace App.Core.Interfaces;

public interface ICotizacionService
{
    Task<Result<List<CotizacionDto>>> GetAllAsync();
    Task<Result<List<CotizacionDto>>> GetByClienteIdAsync(int clienteId);
    Task<Result<CotizacionDto>> GetByIdAsync(int id);
    Task<Result<CotizacionDto>> CreateAsync(CreateCotizacionDto dto);

    /// <summary>Replaces the Cliente and líneas of a Cotización. Only allowed while Estado == Pendiente.</summary>
    Task<Result<CotizacionDto>> UpdateAsync(int cotizacionId, UpdateCotizacionDto dto);
    Task<Result<CotizacionDto>> AprobarAsync(int cotizacionId, AprobarCotizacionDto dto);
    Task<Result<CotizacionDto>> RechazarAsync(int cotizacionId);
    Task<Result> DeleteAsync(int cotizacionId);

    /// <summary>Renders the Cotización as a PDF using the effective (DB override or default) template.</summary>
    Task<Result<byte[]>> GetCotizacionPdfAsync(int cotizacionId, CancellationToken cancellationToken = default);

    /// <summary>Emails the Cotización PDF as an attachment to the given recipient.</summary>
    Task<Result> SendCotizacionEmailAsync(int cotizacionId, string recipientEmail, CancellationToken cancellationToken = default);
}
