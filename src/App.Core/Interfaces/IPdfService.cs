namespace App.Core.Interfaces;

public interface IPdfService
{
    Task<byte[]> GeneratePdfFromHtmlAsync(
        string html,
        CancellationToken cancellationToken = default);

    Task<byte[]> GeneratePdfFromViewAsync<TModel>(
        string viewPath,
        TModel model,
        CancellationToken cancellationToken = default);

    Task<byte[]> GenerateThermalTicketPdfFromHtmlAsync(
        string html, 
        int widthInMm = 80,
        CancellationToken cancellationToken = default);
        
    Task<byte[]> GenerateThermalTicketPdfFromViewAsync<TModel>(
        string viewPath, 
        TModel model, 
        int widthInMm = 80,
        CancellationToken cancellationToken = default);
}