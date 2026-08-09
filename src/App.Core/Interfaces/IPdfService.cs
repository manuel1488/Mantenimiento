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
}