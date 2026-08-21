using App.Core.Interfaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Controllers;

[ApiController]
[Route("api/cotizaciones")]
[Authorize]
public class CotizacionPdfController : ControllerBase
{
    private readonly ICotizacionService _cotizacionService;
    private readonly ILogger<CotizacionPdfController> _logger;

    public CotizacionPdfController(
        ICotizacionService cotizacionService,
        ILogger<CotizacionPdfController> logger)
    {
        _cotizacionService = cotizacionService;
        _logger = logger;
    }

    /// <summary>
    /// Returns the Cotización PDF. By default (<paramref name="inline"/> = false) the response is
    /// sent as an attachment so the browser downloads it; pass <c>?inline=true</c> to have it render
    /// directly in the browser/tab instead (used by the "Ver PDF" action, which opens it via a plain
    /// anchor with <c>target="_blank"</c> — a real hyperlink, not a JS-triggered popup, so it isn't
    /// blocked by Safari's popup blocker on iPad/iPhone).
    /// </summary>
    [HttpGet("{id:int}/pdf")]
    public async Task<IActionResult> GetPdf([FromRoute] int id, [FromQuery] bool inline = false)
    {
        try
        {
            var result = await _cotizacionService.GetCotizacionPdfAsync(id);
            if (!result.IsSuccess)
                return NotFound(result.Error);

            var fileName = $"cotizacion-{id}.pdf";

            if (inline)
            {
                Response.Headers.ContentDisposition = $"inline; filename=\"{fileName}\"";
                return File(result.Value!, "application/pdf");
            }

            return File(result.Value!, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF for cotizacion {Id}", id);
            return StatusCode(500, "Error generating cotizacion PDF.");
        }
    }
}
