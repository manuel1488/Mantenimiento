using App.Core.Common;
using App.Core.DTOs.Shop;

namespace App.Core.Interfaces.Shop;

public interface ICashRegisterService
{
    /// <summary>
    /// Gets the active cash register for a specific user at a location.
    /// Returns null in Value if none is open.
    /// </summary>
    Task<Result<CashRegisterDto?>> GetActiveCashRegisterAsync(int locationId, string userId);

    /// <summary>
    /// Gets the active cash register for a specific user regardless of location.
    /// Returns null in Value if none is open.
    /// </summary>
    Task<Result<CashRegisterDto?>> GetActiveCashRegisterByUserAsync(string userId);

    /// <summary>
    /// Opens a new cash register session for the given user + location.
    /// Fails if the user already has an open register at the same location.
    /// </summary>
    Task<Result<CashRegisterDto>> OpenCashRegisterAsync(OpenCashRegisterDto dto);

    /// <summary>
    /// Closes an open cash register session with denomination counts.
    /// The cash difference is informational only and does not block closing.
    /// </summary>
    Task<Result<CashRegisterDto>> CloseCashRegisterAsync(CloseCashRegisterDto dto);

    /// <summary>
    /// Adds a withdrawal or deposit movement to an open register.
    /// Withdrawal amounts are validated against CashRegisterSettings.MaxWithdrawalAmount.
    /// </summary>
    Task<Result<CashRegisterMovementDto>> AddMovementAsync(AddCashRegisterMovementDto dto);

    /// <summary>
    /// Gets all data needed to generate the cash register report.
    /// </summary>
    Task<Result<CashRegisterReportDto>> GetReportDataAsync(long cashRegisterId);

    /// <summary>
    /// Gets paginated history of cash register sessions for a location.
    /// Pass null or 0 for locationId to return sessions across all locations (admin use).
    /// </summary>
    Task<(int TotalCount, IList<CashRegisterDto> Items)> GetHistoryAsync(
        int? locationId,
        int page = 1,
        int pageSize = 20,
        DateTime? startDate = null,
        DateTime? endDate = null);

    /// <summary>
    /// Gets current cash register settings. Seeds default row if none exists.
    /// </summary>
    Task<Result<CashRegisterSettingsDto>> GetSettingsAsync();

    /// <summary>
    /// Updates cash register settings (max withdrawal, default initial fund).
    /// </summary>
    Task<Result<CashRegisterSettingsDto>> UpdateSettingsAsync(UpdateCashRegisterSettingsDto dto);
}
