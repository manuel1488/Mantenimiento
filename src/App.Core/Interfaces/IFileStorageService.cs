using App.Core.Common;

namespace App.Core.Interfaces;

public interface IFileStorageService
{
    /// <summary>
    /// Sube los bytes al bucket configurado y regresa la clave del objeto generada.
    /// </summary>
    Task<Result<string>> UploadAsync(
        byte[] data,
        string contentType,
        string keyPrefix,
        string extension,
        CancellationToken cancellationToken = default);

    Task<Result<string>> GetPresignedUrlAsync(string key, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(string key, CancellationToken cancellationToken = default);
}
