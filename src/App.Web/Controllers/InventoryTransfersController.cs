using App.Core.Constants;
using App.Core.Interfaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/inventory-transfers")]
public class InventoryTransfersController : ControllerBase
{
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<InventoryTransfersController> _logger;

    public InventoryTransfersController(IInventoryService inventoryService, ILogger<InventoryTransfersController> logger)
    {
        _inventoryService = inventoryService;
        _logger = logger;
    }

    [HttpGet("batch/{batchId:guid}/pdf")]
    [Authorize(Policy = ApplicationClaims.Shop.ViewInventoryTransfers)]
    public async Task<IActionResult> GetBatchPdf(Guid batchId)
    {
        try
        {
            var bytes = await _inventoryService.GenerateBulkTransferPdfAsync(batchId);
            return File(bytes, "application/pdf");
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF for bulk transfer batch {BatchId}", batchId);
            return StatusCode(500, "Error generating PDF");
        }
    }
}
