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

    [HttpGet("{id:int}/pdf")]
    public async Task<IActionResult> GetPdf([FromRoute] int id)
    {
        try
        {
            var result = await _cotizacionService.GetCotizacionPdfAsync(id);
            if (!result.IsSuccess)
                return NotFound(result.Error);

            return File(result.Value!, "application/pdf", $"cotizacion-{id}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF for cotizacion {Id}", id);
            return StatusCode(500, "Error generating cotizacion PDF.");
        }
    }
}
