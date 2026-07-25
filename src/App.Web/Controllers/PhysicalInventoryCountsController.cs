using App.Core.Constants;
using App.Core.Interfaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/physical-inventory-counts")]
public class PhysicalInventoryCountsController : ControllerBase
{
    private readonly IPhysicalInventoryCountService _physicalInventoryCountService;
    private readonly ILogger<PhysicalInventoryCountsController> _logger;

    public PhysicalInventoryCountsController(
        IPhysicalInventoryCountService physicalInventoryCountService,
        ILogger<PhysicalInventoryCountsController> logger)
    {
        _physicalInventoryCountService = physicalInventoryCountService;
        _logger = logger;
    }

    [HttpGet("batch/{batchId:guid}/pdf")]
    [Authorize(Policy = ApplicationClaims.Shop.ViewPhysicalCounts)]
    public async Task<IActionResult> GetBatchPdf(Guid batchId)
    {
        try
        {
            var bytes = await _physicalInventoryCountService.GeneratePdfAsync(batchId);
            return File(bytes, "application/pdf");
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF for physical inventory count batch {BatchId}", batchId);
            return StatusCode(500, "Error generating PDF");
        }
    }
}
