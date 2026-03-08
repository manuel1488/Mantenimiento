using PuppeteerSharp;
using Razor.Templating.Core;
using Microsoft.Extensions.Logging;
using PuppeteerSharp.Media;
using App.Core.Interfaces;

namespace App.Services.Reports;

public class PdfService : IPdfService
{
    private readonly ILogger<PdfService> _logger;
    private static IBrowser? _browser;
    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    public PdfService(ILogger<PdfService> logger)
    {
        _logger = logger;
    }

    public async Task<byte[]> GeneratePdfFromHtmlAsync(
        string html,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Ensure browser is installed and launched
            await EnsureBrowserIsReadyAsync(cancellationToken);

            // Create a new page
            await using var page = await _browser!.NewPageAsync();

            // Set content and wait for network idle to ensure all resources are loaded
            await page.SetContentAsync(html, new NavigationOptions
            {
                WaitUntil = new[] { WaitUntilNavigation.Networkidle0 }
            });

            // Generate PDF
            var pdfOptions = new PdfOptions
            {
                Format = PaperFormat.Letter,
                PrintBackground = true,
                PreferCSSPageSize = true,
                DisplayHeaderFooter = true,
                HeaderTemplate = "<span></span>",
                FooterTemplate = "<div style=\"width:100%;font-family:Arial,sans-serif;font-size:8px;color:#999;text-align:center;padding:0 20px;\">" +
                                 "Página <span class=\"pageNumber\"></span> de <span class=\"totalPages\"></span>" +
                                 "</div>",
                MarginOptions = new MarginOptions
                {
                    Top = "20px",
                    Right = "20px",
                    Bottom = "32px",
                    Left = "20px"
                }
            };

            var pdfBuffer = await page.PdfDataAsync(pdfOptions);
            return pdfBuffer;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF from HTML");
            throw;
        }
    }

    public async Task<byte[]> GeneratePdfFromViewAsync<TModel>(
        string viewPath,
        TModel model,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Render Razor view to HTML
            var html = await RazorTemplateEngine.RenderAsync(viewPath, model);
            
            // Generate PDF from HTML
            return await GeneratePdfFromHtmlAsync(html, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF from view {ViewPath}", viewPath);
            throw;
        }
    }

    public async Task<byte[]> GenerateThermalTicketPdfFromHtmlAsync(
        string html, 
        int widthInMm = 80,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Ensure browser is installed and launched
            await EnsureBrowserIsReadyAsync(cancellationToken);

            // Create a new page
            await using var page = await _browser!.NewPageAsync();

            // Set content and wait for network idle to ensure all resources are loaded
            await page.SetContentAsync(html, new NavigationOptions
            {
                WaitUntil = new[] { WaitUntilNavigation.Networkidle0 }
            });

            // Configure PDF options specifically for thermal tickets
            var pdfOptions = new PdfOptions
            {
                Format = null,
                PrintBackground = true,
                PreferCSSPageSize = true,
                Width = $"{widthInMm}mm",
                MarginOptions = new MarginOptions
                {
                    Top = "3mm",
                    Right = "3mm",
                    Bottom = "3mm",
                    Left = "0mm"  // Sin margen izquierdo para maximizar el espacio
                }
            };

            var pdfBuffer = await page.PdfDataAsync(pdfOptions);
            return pdfBuffer;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating thermal ticket PDF from HTML");
            throw;
        }
    }
    
    public async Task<byte[]> GenerateThermalTicketPdfFromViewAsync<TModel>(
        string viewPath,
        TModel model,
        int widthInMm = 80,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Render Razor view to HTML
            var html = await RazorTemplateEngine.RenderAsync(viewPath, model);
            
            // Generate thermal ticket PDF from HTML
            return await GenerateThermalTicketPdfFromHtmlAsync(html, widthInMm, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating thermal ticket PDF from view {ViewPath}", viewPath);
            throw;
        }
    }

    private async Task EnsureBrowserIsReadyAsync(CancellationToken cancellationToken = default)
    {
        // Check if browser exists and is still connected
        if (_browser != null && _browser.IsConnected) return;

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock
            if (_browser != null && _browser.IsConnected) return;

            // Dispose old browser if it exists but is disconnected
            if (_browser != null)
            {
                _logger.LogWarning("Browser was disconnected, disposing and recreating...");
                try
                {
                    await _browser.DisposeAsync();
                }
                catch
                {
                    // Ignore disposal errors
                }
                _browser = null;
            }

            // Download and install Chrome
            var browserFetcherOptions = new BrowserFetcherOptions();
            await new BrowserFetcher().DownloadAsync();

            // Launch browser
            _browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                Args = new[]
                {
                    "--no-sandbox",
                    "--disable-setuid-sandbox",
                    "--disable-dev-shm-usage",
                    "--disable-gpu"
                }
            });

            _logger.LogInformation("Puppeteer browser launched successfully");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    ~PdfService()
    {
        _browser?.Dispose();
        _semaphore.Dispose();
    }
}