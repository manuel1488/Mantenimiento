using App.Core.Interfaces;
using App.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace App.Services.Images;

public class ImageService : IImageService
{
    private readonly ImageOptions _options;
    private readonly ILogger<ImageService> _logger;

    public ImageService(
        IOptions<ImageOptions> options,
        ILogger<ImageService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<(byte[] ImageData, string ContentType)> ProcessImageAsync(
        Stream imageStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ValidateImage(imageStream, fileName);

            using var image = await Image.LoadAsync(imageStream, cancellationToken);

            // Apply basic optimization
            image.Mutate(x => x.AutoOrient());

            // Convert to byte array with optimal settings
            var imageData = await ConvertToByteArrayWithOptimalSettings(image, contentType, cancellationToken);

            return (imageData, contentType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing image {FileName}", fileName);
            throw;
        }
    }

    public async Task<(byte[] ThumbnailData, string ContentType)> CreateThumbnailAsync(
        Stream imageStream,
        string fileName,
        string contentType,
        int maxWidth = 300,
        int maxHeight = 300,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ValidateImage(imageStream, fileName);

            using var image = await Image.LoadAsync(imageStream, cancellationToken);

            // Calculate dimensions maintaining aspect ratio
            var (width, height) = CalculateDimensions(
                image.Width, 
                image.Height,
                maxWidth,
                maxHeight);

            image.Mutate(x => x
                .AutoOrient()
                .Resize(width, height));

            // Convert to byte array with optimal settings
            var thumbnailData = await ConvertToByteArrayWithOptimalSettings(image, contentType, cancellationToken);

            return (thumbnailData, contentType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating thumbnail for {FileName}", fileName);
            throw;
        }
    }

    public async Task<(byte[] ImageData, byte[] ThumbnailData, string ContentType)> ProcessImageWithThumbnailAsync(
        Stream imageStream,
        string fileName,
        string contentType,
        int maxWidth = 300,
        int maxHeight = 300,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ValidateImage(imageStream, fileName);

            using var image = await Image.LoadAsync(imageStream, cancellationToken);

            // Process main image
            var mainImage = image.Clone(ctx => { });
            mainImage.Mutate(x => x.AutoOrient());
            var imageData = await ConvertToByteArrayWithOptimalSettings(mainImage, contentType, cancellationToken);

            // Process thumbnail
            var (width, height) = CalculateDimensions(
                image.Width, 
                image.Height,
                maxWidth,
                maxHeight);

            image.Mutate(x => x
                .AutoOrient()
                .Resize(width, height));

            var thumbnailData = await ConvertToByteArrayWithOptimalSettings(image, contentType, cancellationToken);

            return (imageData, thumbnailData, contentType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing image with thumbnail {FileName}", fileName);
            throw;
        }
    }

    public async Task<byte[]> ConvertFormatAsync(
        Stream imageStream,
        string fileName,
        string targetContentType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ValidateImage(imageStream, fileName);

            using var image = await Image.LoadAsync(imageStream, cancellationToken);

            // Apply basic optimization
            image.Mutate(x => x.AutoOrient());

            // Convert to target format — ConvertToByteArrayWithOptimalSettings itself rejects
            // any format it doesn't have an encoder for.
            var convertedData = await ConvertToByteArrayWithOptimalSettings(image, targetContentType, cancellationToken);

            return convertedData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting image {FileName} to {TargetFormat}",
                fileName, targetContentType);
            throw;
        }
    }

    private void ValidateImage(Stream stream, string fileName)
    {
        if (stream.Length > _options.MaxFileSize)
        {
            throw new ArgumentException($"File size exceeds maximum allowed ({_options.MaxFileSize} bytes)");
        }

        var extension = Path.GetExtension(fileName);
        if (!_options.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"File extension {extension} is not allowed");
        }
    }

    private async Task<byte[]> ConvertToByteArrayWithOptimalSettings(
        Image image,
        string contentType,
        CancellationToken cancellationToken)
    {
        IImageEncoder encoder = contentType.ToLower() switch
        {
            "image/jpeg" => new JpegEncoder
            {
                Quality = _options.JpegQuality
            },
            "image/png" => new PngEncoder
            {
                CompressionLevel = PngCompressionLevel.BestCompression
            },
            "image/webp" => new WebpEncoder
            {
                Quality = _options.JpegQuality
            },
            _ => throw new ArgumentException($"Unsupported image format: {contentType}")
        };

        using var memoryStream = new MemoryStream();
        await image.SaveAsync(memoryStream, encoder, cancellationToken);
        return memoryStream.ToArray();
    }

    private (int width, int height) CalculateDimensions(
        int originalWidth,
        int originalHeight,
        int maxWidth,
        int maxHeight)
    {
        var ratioX = (double)maxWidth / originalWidth;
        var ratioY = (double)maxHeight / originalHeight;
        var ratio = Math.Min(ratioX, ratioY);

        var newWidth = (int)(originalWidth * ratio);
        var newHeight = (int)(originalHeight * ratio);

        return (newWidth, newHeight);
    }
}