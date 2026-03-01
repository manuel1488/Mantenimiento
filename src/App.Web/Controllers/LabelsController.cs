using App.Core.Constants;
using App.Core.DTOs.Label;
using App.Core.Interfaces.Shop;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Controllers;

[Route("api/labels")]
[ApiController]
[Authorize]
public class LabelsController : ControllerBase
{
    private readonly IBulkLabelService _bulkLabelService;
    private readonly ILogger<LabelsController> _logger;

    public LabelsController(
        IBulkLabelService bulkLabelService,
        ILogger<LabelsController> logger)
    {
        _bulkLabelService = bulkLabelService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a bulk label job (saves to DB) and returns the job data.
    /// </summary>
    [HttpPost("bulk")]
    [Authorize(Policy = ApplicationClaims.Labels.PrintLabels)]
    public async Task<ActionResult<BulkLabelJobDto>> CreateBulkLabel(
        [FromBody] CreateBulkLabelJobDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _bulkLabelService.CreateAsync(dto, cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    /// <summary>
    /// Generates a label PDF preview without saving to database.
    /// Useful for showing a preview before printing.
    /// </summary>
    [HttpPost("bulk/preview")]
    [Authorize(Policy = ApplicationClaims.Labels.PrintLabels)]
    public async Task<IActionResult> PreviewBulkLabel(
        [FromBody] CreateBulkLabelJobDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _bulkLabelService.PreviewLabelPdfAsync(dto, cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(result.Error);

        Response.Headers.Append("Content-Disposition", "inline; filename=label_preview.pdf");
        return File(result.Value, "application/pdf");
    }

    /// <summary>
    /// Gets the PDF for a previously saved label job.
    /// </summary>
    [HttpGet("bulk/{id}/pdf")]
    [Authorize(Policy = ApplicationClaims.Labels.PrintLabels)]
    public async Task<IActionResult> GetBulkLabelPdf(
        long id,
        bool download = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _bulkLabelService.GetLabelPdfAsync(id, cancellationToken);
        if (!result.IsSuccess)
            return NotFound(result.Error);

        var contentDisposition = download
            ? $"attachment; filename=label_{id}.pdf"
            : $"inline; filename=label_{id}.pdf";

        Response.Headers.Append("Content-Disposition", contentDisposition);
        return File(result.Value, "application/pdf");
    }

    /// <summary>
    /// Gets a list of recent bulk label jobs.
    /// </summary>
    [HttpGet("bulk")]
    [Authorize(Policy = ApplicationClaims.Labels.ViewLabels)]
    public async Task<ActionResult<List<BulkLabelJobDto>>> GetRecentBulkLabels(
        int count = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _bulkLabelService.GetRecentAsync(count, cancellationToken);
        if (!result.IsSuccess)
            return StatusCode(500, result.Error);

        return Ok(result.Value);
    }

    /// <summary>
    /// Gets a specific bulk label job by ID.
    /// </summary>
    [HttpGet("bulk/{id}")]
    [Authorize(Policy = ApplicationClaims.Labels.ViewLabels)]
    public async Task<ActionResult<BulkLabelJobDto>> GetBulkLabelJob(
        long id,
        CancellationToken cancellationToken = default)
    {
        var result = await _bulkLabelService.GetByIdAsync(id, cancellationToken);
        if (!result.IsSuccess)
            return NotFound(result.Error);

        return Ok(result.Value);
    }
}
