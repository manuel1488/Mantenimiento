namespace App.Core.Interfaces;

public interface IImageService
{
    /// <summary>
    /// Procesa una imagen aplicando optimizaciones básicas
    /// </summary>
    /// <param name="imageStream">Stream de la imagen a procesar</param>
    /// <param name="fileName">Nombre del archivo (usado para logging)</param>
    /// <param name="contentType">Tipo de contenido de la imagen</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Datos de la imagen procesada y su tipo de contenido</returns>
    Task<(byte[] ImageData, string ContentType)> ProcessImageAsync(
        Stream imageStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Crea una miniatura de la imagen
    /// </summary>
    /// <param name="imageStream">Stream de la imagen original</param>
    /// <param name="fileName">Nombre del archivo (usado para logging)</param>
    /// <param name="contentType">Tipo de contenido de la imagen</param>
    /// <param name="maxWidth">Ancho máximo de la miniatura</param>
    /// <param name="maxHeight">Alto máximo de la miniatura</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Datos de la miniatura y su tipo de contenido</returns>
    Task<(byte[] ThumbnailData, string ContentType)> CreateThumbnailAsync(
        Stream imageStream,
        string fileName,
        string contentType,
        int maxWidth = 300,
        int maxHeight = 300,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Procesa una imagen y genera su miniatura en una sola operación
    /// </summary>
    /// <param name="imageStream">Stream de la imagen a procesar</param>
    /// <param name="fileName">Nombre del archivo (usado para logging)</param>
    /// <param name="contentType">Tipo de contenido de la imagen</param>
    /// <param name="maxWidth">Ancho máximo de la miniatura</param>
    /// <param name="maxHeight">Alto máximo de la miniatura</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Datos de la imagen procesada, miniatura y tipo de contenido</returns>
    Task<(byte[] ImageData, byte[] ThumbnailData, string ContentType)> ProcessImageWithThumbnailAsync(
        Stream imageStream,
        string fileName,
        string contentType,
        int maxWidth = 300,
        int maxHeight = 300,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Convierte una imagen de un formato a otro
    /// </summary>
    /// <param name="imageStream">Stream de la imagen original</param>
    /// <param name="originalContentType">Tipo de contenido original</param>
    /// <param name="targetContentType">Tipo de contenido destino</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Datos de la imagen convertida</returns>
    Task<byte[]> ConvertFormatAsync(
        Stream imageStream,
        string originalContentType,
        string targetContentType,
        CancellationToken cancellationToken = default);
}