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

    /// <summary>
    /// Copia un objeto existente a una nueva clave (server-side, sin descargar/resubir bytes).
    /// Usado para desacoplar un archivo que sigue en uso por su dueño original de una copia que
    /// otro registro guarda como snapshot propio.
    /// </summary>
    Task<Result<string>> CopyAsync(string sourceKey, string keyPrefix, string extension, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Prueba la conectividad contra un endpoint MinIO/S3 con las credenciales dadas
    /// (no necesariamente las ya guardadas), verificando que el bucket exista y sea accesible.
    /// </summary>
    Task<Result> TestConnectionAsync(
        string endpoint,
        string bucketName,
        string accessKey,
        string secretKey,
        bool useSsl,
        string region,
        CancellationToken cancellationToken = default);
}
