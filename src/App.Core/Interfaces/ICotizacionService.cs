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

    /// <summary>Uploads and compresses a photo for the given Cotización. Fails if the Cotización
    /// already has CotizacionFotoOptions.MaxFotos photos.</summary>
    Task<Result<CotizacionFotoDto>> UploadFotoAsync(
        int cotizacionId, byte[] data, string contentType, string fileName, string? descripcion = null, CancellationToken cancellationToken = default);

    Task<Result> DeleteFotoAsync(int fotoId, CancellationToken cancellationToken = default);

    /// <summary>Updates only the caption of an existing photo.</summary>
    Task<Result> UpdateFotoDescripcionAsync(int fotoId, string? descripcion, CancellationToken cancellationToken = default);

    Task<Result<List<CotizacionFotoDto>>> GetFotosAsync(int cotizacionId, CancellationToken cancellationToken = default);
}
