namespace App.Core.Interfaces;

public interface IPdfService
{
    /// <summary>
    /// Renders <paramref name="html"/> to a PDF. When <paramref name="footerHtml"/> is provided, it's
    /// rendered by the browser's native print footer (Chromium's own header/footer pipeline, isolated
    /// from the page's own CSS/JS) so it repeats identically on every page — unlike a div placed inside
    /// <paramref name="html"/>'s own flow, which only appears once wherever it happens to land.
    /// </summary>
    Task<byte[]> GeneratePdfFromHtmlAsync(
        string html,
        CancellationToken cancellationToken = default,
        string? footerHtml = null);

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