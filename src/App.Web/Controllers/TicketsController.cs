using App.Core.Constants;
using App.Core.DTOs.Ticket;
using App.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Controllers;

[Route("api/tickets")]
[ApiController]
[Authorize]
public class TicketsController : ControllerBase
{
    private readonly ITicketService _ticketService;
    private readonly ILogger<TicketsController> _logger;

    public TicketsController(
        ITicketService ticketService,
        ILogger<TicketsController> logger)
    {
        _ticketService = ticketService;
        _logger = logger;
    }

    [HttpGet("sale/{id}")]
    [Authorize(Policy = ApplicationClaims.Shop.ViewSales)]
    public async Task<IActionResult> GetSaleTicket(long id, bool download = false)
    {
        try
        {
            var pdfBytes = await _ticketService.GenerateSaleTicketPdfAsync(id);
            
            // Si es descarga, devolver como archivo adjunto, sino como inline para vista previa/impresión
            var contentDisposition = download 
                ? $"attachment; filename=ticket_{id}.pdf" 
                : $"inline; filename=ticket_{id}.pdf";
                
            Response.Headers.Append("Content-Disposition", contentDisposition);
            return File(pdfBytes, "application/pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generando ticket para venta {Id}", id);
            return StatusCode(500, "Error generando ticket");
        }
    }

    [HttpGet("configuration")]
    [Authorize(Policy = ApplicationClaims.Admin.ViewSettings)]
    public async Task<ActionResult<TicketConfigurationDto>> GetConfiguration()
    {
        try
        {
            var config = await _ticketService.GetTicketConfigurationAsync();
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo configuración de tickets");
            return StatusCode(500, "Error obteniendo configuración");
        }
    }

    [HttpPost("configuration")]
    [Authorize(Policy = ApplicationClaims.Admin.ManageSettings)]
    public async Task<ActionResult<TicketConfigurationDto>> UpdateConfiguration(UpdateTicketConfigurationDto updateDto)
    {
        try
        {
            var config = await _ticketService.UpdateTicketConfigurationAsync(updateDto);
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error actualizando configuración de tickets");
            return StatusCode(500, "Error actualizando configuración");
        }
    }
}