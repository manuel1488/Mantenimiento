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

    /// <summary>
    /// Launches the headless browser ahead of the first real PDF request, so that request doesn't
    /// pay the multi-second Chromium cold-start cost. Safe to call multiple times — a no-op if
    /// the browser is already running.
    /// </summary>
    Task WarmUpAsync(CancellationToken cancellationToken = default);
}