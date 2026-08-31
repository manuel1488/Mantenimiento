using App.Core.Common;
using App.Core.DTOs.Obras;
using App.Core.Enums.Obras;

namespace App.Core.Interfaces;

public interface IObraService
{
    Task<Result<List<ObraDto>>> GetAllAsync();
    Task<Result<ObraDto>> GetByIdAsync(int id);
    Task<Result<ObraDto>> CreateAsync(CreateObraDto dto);
    Task<Result<ObraDto>> UpdateAsync(UpdateObraDto dto);
    Task<Result> DeleteAsync(int id);
    Task<Result<ObraDto>> FinalizarAsync(int id);

    /// <summary>Transitions an Aprobada Obra to EnProceso and notifies the client (best-effort).</summary>
    Task<Result<ObraDto>> IniciarAsync(int id);
    Task<Result<ObraDto>> CreateFromCotizacionAsync(int cotizacionId, ConvertirCotizacionAObraDto dto);

    /// <summary>Uploads and compresses a general photo for the given Obra (not tied to any
    /// Actividad).</summary>
    Task<Result<ObraFotoGeneralDto>> UploadFotoGeneralAsync(
        int obraId, byte[] data, string contentType, string fileName, string? descripcion = null, CancellationToken cancellationToken = default);

    Task<Result> DeleteFotoGeneralAsync(int fotoId, CancellationToken cancellationToken = default);

    /// <summary>Updates only the caption of an existing general photo.</summary>
    Task<Result> UpdateFotoGeneralDescripcionAsync(int fotoId, string? descripcion, CancellationToken cancellationToken = default);

    Task<Result<List<ObraFotoGeneralDto>>> GetFotosGeneralesAsync(int obraId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a message/alert to the Obra's Cliente through every notification channel the Cliente has
    /// contact data for (best-effort, channel-agnostic — see <see cref="Interfaces.Notifications.INotificationService"/>),
    /// optionally with a photo attached, and records it in the Obra's message history.
    /// </summary>
    Task<Result<ObraMensajeDto>> SendMensajeAsync(
        int obraId, TipoObraMensaje tipo, string asunto, string cuerpo,
        byte[]? fotoData = null, string? fotoContentType = null, string? fotoFileName = null,
        CancellationToken cancellationToken = default);

    Task<Result<List<ObraMensajeDto>>> GetMensajesAsync(int obraId, CancellationToken cancellationToken = default);
}
