using App.Core.Common;
using App.Core.DTOs.Obras;
using App.Core.Enums.Obras;

namespace App.Core.Interfaces;

public interface IActividadService
{
    Task<Result<List<ActividadDto>>> GetByObraIdAsync(int obraId);
    Task<Result<ActividadDto>> CreateAsync(CreateActividadDto dto);
    Task<Result<ActividadDto>> UpdateAsync(UpdateActividadDto dto);
    Task<Result<ActividadDto>> ActualizarAvanceAsync(int id, int porcentajeAvance);
    Task<Result> DeleteAsync(int id);

    Task<Result<ActividadEvidenciaFotoDto>> UploadEvidenciaAsync(
        int actividadId,
        TipoEvidencia tipo,
        byte[] data,
        string contentType,
        string fileName,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteEvidenciaAsync(int evidenciaId);

    Task<Result<List<ActividadEvidenciaFotoDto>>> GetEvidenciasAsync(int actividadId);
}
