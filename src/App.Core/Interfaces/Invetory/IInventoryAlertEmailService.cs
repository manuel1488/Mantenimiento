namespace App.Core.Interfaces;

/// <summary>
/// Service for sending email alerts when inventory stock levels reach critical thresholds
/// </summary>
public interface IInventoryAlertEmailService
{
    /// <summary>
    /// Sends an email alert for low stock situation
    /// </summary>
    /// <param name="alertInfo">Information about the inventory alert</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the email sending operation</returns>
    Task SendLowStockAlertAsync(InventoryAlertInfo alertInfo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an email alert for over stock situation
    /// </summary>
    /// <param name="alertInfo">Information about the inventory alert</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the email sending operation</returns>
    Task SendOverStockAlertAsync(InventoryAlertInfo alertInfo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an email alert based on alert type (low stock or over stock)
    /// </summary>
    /// <param name="alertInfo">Information about the inventory alert</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the email sending operation</returns>
    Task SendInventoryAlertAsync(InventoryAlertInfo alertInfo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the list of users who should receive inventory alert emails
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of email addresses that should receive alerts</returns>
    Task<IList<string>> GetInventoryAlertRecipientsAsync(CancellationToken cancellationToken = default);
}