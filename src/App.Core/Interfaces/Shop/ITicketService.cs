using App.Core.DTOs.Shop;
using App.Core.DTOs.Ticket;

namespace App.Core.Interfaces;

public interface ITicketService
{
    /// <summary>
    /// Genera un PDF de ticket para una venta
    /// </summary>
    Task<byte[]> GenerateSaleTicketPdfAsync(long saleId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Obtiene la URL del ticket para una venta (para imprimir desde el navegador)
    /// </summary>
    string GetSaleTicketUrl(long saleId);
    
    /// <summary>
    /// Obtiene la configuración de tickets actual
    /// </summary>
    Task<TicketConfigurationDto> GetTicketConfigurationAsync();
    
    /// <summary>
    /// Actualiza la configuración de tickets
    /// </summary>
    Task<TicketConfigurationDto> UpdateTicketConfigurationAsync(UpdateTicketConfigurationDto updateDto);
}