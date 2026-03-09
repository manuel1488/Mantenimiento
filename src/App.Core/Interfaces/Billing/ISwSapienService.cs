using App.Core.Common;
using App.Core.DTOs.Billing.Mexico;

namespace App.Core.Interfaces.Billing;

public interface ISwSapienService
{
    /// <summary>Stamps a signed CFDI XML string and returns UUID and digital seals.</summary>
    Task<Result<SwSapienStampData>> StampAsync(string signedXml);

    /// <summary>Tests authentication with the configured PAC endpoint.</summary>
    Task<Result> TestConnectionAsync();

    /// <summary>
    /// Fetches the stamp balance for the authenticated user from the SW Sapien
    /// Management API (GET /management/v2/api/users/balance).
    /// </summary>
    Task<Result<SwSapienStampBalanceData>> GetStampBalanceAsync();

    /// <summary>
    /// Sends a cancellation request to SAT via PAC (POST /cfdi33/cancel/csd/status).
    /// </summary>
    /// <param name="uuid">Invoice UUID to cancel.</param>
    /// <param name="issuerRfc">Issuer RFC.</param>
    /// <param name="receiverRfc">Receiver (customer) RFC.</param>
    /// <param name="total">Invoice total amount.</param>
    /// <param name="cancellationReason">SAT reason code: 01, 02, 03, 04.</param>
    /// <param name="replacementUuid">Required when reason is "01".</param>
    Task<Result<SwSapienCancelData>> CancelCfdiAsync(
        string uuid,
        string issuerRfc,
        string receiverRfc,
        decimal total,
        string cancellationReason,
        string? replacementUuid = null);

    /// <summary>
    /// Queries the current cancellation status of a CFDI from SAT via PAC
    /// (POST /cfdi33/cancel/csd/status — same endpoint as cancel, re-submitting refreshes status).
    /// </summary>
    Task<Result<SwSapienCancelData>> CheckCancellationStatusAsync(
        string uuid,
        string issuerRfc,
        string receiverRfc,
        decimal total,
        string cancellationReason,
        string? replacementUuid = null);
}
