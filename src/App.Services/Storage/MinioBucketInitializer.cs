using App.Core.Interfaces;

using Microsoft.Extensions.Logging;

using Minio;
using Minio.DataModel.Args;

namespace App.Services.Storage;

public class MinioBucketInitializer
{
    private readonly IMinioConfiguracionService _configService;
    private readonly ILogger<MinioBucketInitializer> _logger;

    public MinioBucketInitializer(
        IMinioConfiguracionService configService,
        ILogger<MinioBucketInitializer> logger)
    {
        _configService = configService;
        _logger = logger;
    }

    public async Task EnsureBucketExistsAsync(CancellationToken cancellationToken = default)
    {
        var config = await _configService.GetConfigAsync();
        if (config == null)
        {
            _logger.LogInformation("MinIO no está configurado todavía — omitiendo verificación del bucket al iniciar.");
            return;
        }

        var client = new MinioClient()
            .WithEndpoint(config.Endpoint)
            .WithCredentials(config.AccessKey, config.SecretKey)
            .WithRegion(config.Region)
            .WithSSL(config.UseSsl)
            .Build();

        var exists = await client.BucketExistsAsync(new BucketExistsArgs()
            .WithBucket(config.BucketName), cancellationToken);

        if (exists)
        {
            return;
        }

        await client.MakeBucketAsync(new MakeBucketArgs()
            .WithBucket(config.BucketName), cancellationToken);

        _logger.LogInformation("Created MinIO bucket {BucketName}", config.BucketName);
    }
}
