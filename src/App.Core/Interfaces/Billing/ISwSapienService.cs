using App.Core.Common;
using App.Core.DTOs.Billing.Mexico;

namespace App.Core.Interfaces.Billing;

public interface ISwSapienService
{
    /// <summary>Stamps a signed CFDI XML string and returns UUID and digital seals.</summary>
    Task<Result<SwSapienStampData>> StampAsync(string signedXml);

    /// <summary>Tests authentication with the configured PAC endpoint.</summary>
    Task<Result> TestConnectionAsync();
}
