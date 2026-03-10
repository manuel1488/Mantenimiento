using App.Core.Constants;
using App.Core.Interfaces.Shop;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class QuotationsController : ControllerBase
{
    private readonly IQuotationService _quotationService;
    private readonly ILogger<QuotationsController> _logger;

    public QuotationsController(IQuotationService quotationService, ILogger<QuotationsController> logger)
    {
        _quotationService = quotationService;
        _logger = logger;
    }

    [HttpGet("{id:long}/pdf")]
    [Authorize(Policy = ApplicationClaims.Shop.ViewQuotations)]
    public async Task<IActionResult> GetPdf(long id)
    {
        try
        {
            var bytes = await _quotationService.GeneratePdfAsync(id);
            return File(bytes, "application/pdf"); // inline — browser previews instead of downloading
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF for quotation {Id}", id);
            return StatusCode(500, "Error generating PDF");
        }
    }
}
