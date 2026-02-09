using App.Core.Interfaces;

using Microsoft.AspNetCore.Mvc;

namespace App.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TemplatesController : ControllerBase
{
    private readonly ITemplateService _templateService;
    private readonly ILogger<TemplatesController> _logger;

    public TemplatesController(ITemplateService templateService,
        ILogger<TemplatesController> logger)
    {
        _templateService = templateService;
        _logger = logger;
    }

    [HttpGet("inventory")]
    public async Task<IActionResult> GetInventoryTemplate()
    {
        var content = await _templateService.GenerateInventoryTemplateAsync();
        return File(
            content,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "inventory_template.xlsx");
    }


    [HttpGet("product")]
    public async Task<IActionResult> GetProductTemplate()
    {
        try
        {
            var content = await _templateService.GenerateProductTemplateAsync();
            return File(
                content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "product_template.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating product template");
            return StatusCode(500, "Error generating product template");
        }
    }
}