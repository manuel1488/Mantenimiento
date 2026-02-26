using App.Core.Common;
using App.Core.DTOs.Billing.Mexico;

namespace App.Core.Interfaces.Billing;

public interface IMexicoPacSettingsService
{
    Task<MexicoPacSettingsDto?> GetAsync();
    Task<Result<MexicoPacSettingsDto>> SaveAsync(UpdateMexicoPacSettingsDto dto);

    /// <summary>Returns the raw CSD certificate bytes (from Base64 stored value).</summary>
    Task<Result<byte[]>> GetCsdCertificateBytesAsync();

    /// <summary>Returns the raw CSD private key bytes (from Base64 stored value).</summary>
    Task<Result<byte[]>> GetCsdPrivateKeyBytesAsync();

    /// <summary>Returns the CSD password.</summary>
    Task<Result<string>> GetCsdPasswordAsync();
}
