using App.Core.Common;
using App.Core.DTOs.Billing.Mexico;

namespace App.Core.Interfaces.Billing;

public interface IMexicoStampAlertService
{
    /// <summary>
    /// Fetches current stamp balance from SW Sapien and counts locally stamped invoices.
    /// </summary>
    Task<Result<MexicoStampBalanceDto>> GetBalanceAsync();

    /// <summary>Returns the alert configuration. Returns defaults if none saved yet.</summary>
    Task<StampAlertSettingsDto> GetAlertSettingsAsync();

    /// <summary>Persists the alert configuration.</summary>
    Task<Result<StampAlertSettingsDto>> SaveAlertSettingsAsync(UpdateStampAlertSettingsDto dto);

    /// <summary>
    /// Checks stamp balance; if below the configured threshold and cooldown has passed,
    /// sends email to all users with the ReceiveStampAlertEmails claim.
    /// Safe to call fire-and-forget — all exceptions are caught internally.
    /// </summary>
    Task CheckAndAlertIfNeededAsync();
}
