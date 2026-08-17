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

    private static IMinioClient BuildClient(MinioConfiguracionDto config) =>
        new MinioClient()
            .WithEndpoint(config.Endpoint)
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
}
