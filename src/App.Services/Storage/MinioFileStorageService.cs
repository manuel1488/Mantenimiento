using App.Core.Common;
using App.Core.DTOs.Settings;
using App.Core.Interfaces;

using Microsoft.Extensions.Logging;

using Minio;
using Minio.DataModel.Args;

namespace App.Services.Storage;

public class MinioFileStorageService : IFileStorageService
{
    private readonly IMinioConfiguracionService _configService;
    private readonly ILogger<MinioFileStorageService> _logger;

    public MinioFileStorageService(
        IMinioConfiguracionService configService,
        ILogger<MinioFileStorageService> logger)
    {
        _configService = configService;
        _logger = logger;
    }

    /// <summary>
    /// El SDK de MinIO espera solo el host (sin esquema) en WithEndpoint — el esquema se controla
    /// aparte con WithSSL. Si el usuario captura "https://host" en la configuración, se lo quitamos aquí.
    /// </summary>
    private static string NormalizeEndpoint(string endpoint) =>
        endpoint
            .Replace("https://", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("http://", string.Empty, StringComparison.OrdinalIgnoreCase)
            .TrimEnd('/');

    private static IMinioClient BuildClient(MinioConfiguracionDto config) =>
        new MinioClient()
            .WithEndpoint(NormalizeEndpoint(config.Endpoint))
            .WithCredentials(config.AccessKey, config.SecretKey)
            .WithRegion(config.Region)
            .WithSSL(config.UseSsl)
            .Build();

    private const string NotConfiguredMessage = "MinIO no está configurado. Configúralo en Administración > Configuración.";

    public async Task<Result<string>> UploadAsync(
        byte[] data,
        string contentType,
        string keyPrefix,
        string extension,
        CancellationToken cancellationToken = default)
    {
        var config = await _configService.GetConfigAsync();
        if (config == null)
            return Result<string>.Failure(NotConfiguredMessage);

        try
        {
            var client = BuildClient(config);
            var now = DateTime.UtcNow;
            var key = $"{keyPrefix}/{now:yyyy}/{now:MM}/{Guid.NewGuid():N}.{extension.TrimStart('.')}";

            using var stream = new MemoryStream(data);
            await client.PutObjectAsync(new PutObjectArgs()
                .WithBucket(config.BucketName)
                .WithObject(key)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length)
                .WithContentType(contentType), cancellationToken);

            return Result<string>.Success(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading object with prefix {KeyPrefix} to bucket {BucketName}", keyPrefix, config.BucketName);
            return Result<string>.Failure("Error al subir el archivo al almacenamiento");
        }
    }

    public async Task<Result<string>> GetPresignedUrlAsync(string key, CancellationToken cancellationToken = default)
    {
        var config = await _configService.GetConfigAsync();
        if (config == null)
            return Result<string>.Failure(NotConfiguredMessage);

        try
        {
            var client = BuildClient(config);
            var url = await client.PresignedGetObjectAsync(new PresignedGetObjectArgs()
                .WithBucket(config.BucketName)
                .WithObject(key)
                .WithExpiry((int)TimeSpan.FromHours(config.PresignedUrlExpiryHours).TotalSeconds));

            return Result<string>.Success(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating presigned URL for key {Key}", key);
            return Result<string>.Failure("Error al generar la URL del archivo");
        }
    }

    public async Task<Result> DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        var config = await _configService.GetConfigAsync();
        if (config == null)
            return Result.Failure(NotConfiguredMessage);

        try
        {
            var client = BuildClient(config);
            await client.RemoveObjectAsync(new RemoveObjectArgs()
                .WithBucket(config.BucketName)
                .WithObject(key), cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting object with key {Key}", key);
            return Result.Failure("Error al eliminar el archivo del almacenamiento");
        }
    }

    public async Task<Result> TestConnectionAsync(
        string endpoint,
        string bucketName,
        string accessKey,
        string secretKey,
        bool useSsl,
        string region,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = new MinioClient()
                .WithEndpoint(NormalizeEndpoint(endpoint))
                .WithCredentials(accessKey, secretKey)
                .WithRegion(region)
                .WithSSL(useSsl)
                .Build();

            var exists = await client.BucketExistsAsync(new BucketExistsArgs()
                .WithBucket(bucketName), cancellationToken);

            return exists
                ? Result.Success()
                : Result.Failure("Conexión exitosa, pero el bucket no existe.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MinIO connection test failed for endpoint {Endpoint}", endpoint);
            return Result.Failure($"No se pudo conectar: {ex.Message}");
        }
    }
}
