using App.Core.Common;

namespace App.Web.Services;

public interface IThermalPrinterService
{
    /// <summary>
    /// Returns true if the browser supports Web Serial API.
    /// </summary>
    Task<bool> IsSupportedAsync();

    /// <summary>
    /// Opens the browser port picker so the user can grant access to a COM port.
    /// Must be called from a user-gesture context (button click).
    /// Returns (success, port description) or (false, empty) if cancelled.
    /// </summary>
    Task<Result<string>> RequestPortAsync();

    /// <summary>
    /// Prints a sale ticket directly to the thermal printer via Web Serial API.
    /// Returns Result.Success if printed, Result.Failure if direct print is disabled or failed.
    /// </summary>
    Task<Result> PrintSaleAsync(long saleId);

    /// <summary>
    /// Prints a withdrawal ticket directly to the thermal printer via Web Serial API.
    /// Returns Result.Success if printed, Result.Failure if direct print is disabled or failed.
    /// </summary>
    Task<Result> PrintWithdrawalAsync(long movementId);

    /// <summary>
    /// Sends a test page to verify printer connectivity.
    /// </summary>
    Task<Result> PrintTestPageAsync();

    /// <summary>
    /// Sends the configured ESC/POS cash drawer open command via the serial port.
    /// Returns Result.Failure if the drawer is disabled, not configured, or the port is unavailable.
    /// </summary>
    Task<Result> OpenCashDrawerAsync();
}
