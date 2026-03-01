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
    /// Genera un PDF de ticket para un retiro de caja
    /// </summary>
    Task<byte[]> GenerateWithdrawalTicketPdfAsync(long movementId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene la URL del ticket para un retiro (para imprimir desde el navegador)
    /// </summary>
    string GetWithdrawalTicketUrl(long movementId);

    /// <summary>
    /// Genera un PDF de ticket para el reporte de cierre de caja
    /// </summary>
    Task<byte[]> GenerateCashRegisterReportTicketPdfAsync(long cashRegisterId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene la URL del ticket para un reporte de caja (para imprimir desde el navegador)
    /// </summary>
    string GetCashRegisterReportTicketUrl(long cashRegisterId);

    /// <summary>
    /// Genera un PDF en tamaño carta para el reporte de cierre de caja
    /// </summary>
    Task<byte[]> GenerateCashRegisterReportLetterPdfAsync(long cashRegisterId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene la URL del reporte de caja en tamaño carta
    /// </summary>
    string GetCashRegisterReportLetterUrl(long cashRegisterId);
    
    /// <summary>
    /// Obtiene la configuración de tickets actual
    /// </summary>
    Task<TicketConfigurationDto> GetTicketConfigurationAsync();
    
    /// <summary>
    /// Actualiza la configuración de tickets
    /// </summary>
    Task<TicketConfigurationDto> UpdateTicketConfigurationAsync(UpdateTicketConfigurationDto updateDto);
}