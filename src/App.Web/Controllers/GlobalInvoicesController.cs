using App.Core.Constants;
using App.Core.Interfaces.Billing;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/global-invoices")]
public class GlobalInvoicesController : ControllerBase
{
    private readonly IGlobalInvoiceService _globalInvoiceService;
    private readonly ILogger<GlobalInvoicesController> _logger;

    public GlobalInvoicesController(IGlobalInvoiceService globalInvoiceService,
        ILogger<GlobalInvoicesController> logger)
    {
        _globalInvoiceService = globalInvoiceService;
        _logger = logger;
    }

    [HttpGet("{id:long}/pdf")]
    [Authorize(Policy = ApplicationClaims.Admin.ViewGlobalInvoices)]
    public async Task<IActionResult> GetPdf(long id)
    {
        try
        {
            var result = await _globalInvoiceService.GetPdfAsync(id);
            if (!result.IsSuccess)
                return NotFound(result.Error);

            return File(result.Value!, "application/pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF for global invoice {Id}", id);
            return StatusCode(500, "Error generating PDF");
        }
    }
}
