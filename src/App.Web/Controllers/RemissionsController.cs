using App.Core.Constants;
using App.Core.Interfaces.Shop;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class RemissionsController : ControllerBase
{
    private readonly IRemissionService _remissionService;
    private readonly ILogger<RemissionsController> _logger;

    public RemissionsController(IRemissionService remissionService, ILogger<RemissionsController> logger)
    {
        _remissionService = remissionService;
        _logger = logger;
    }

    [HttpGet("{id:long}/pdf")]
    [Authorize(Policy = ApplicationClaims.Shop.ViewRemissions)]
    public async Task<IActionResult> GetPdf(long id)
    {
        try
        {
            var bytes = await _remissionService.GeneratePdfAsync(id);
            return File(bytes, "application/pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF for remission {Id}", id);
            return StatusCode(500, "Error generating PDF");
        }
    }
}
