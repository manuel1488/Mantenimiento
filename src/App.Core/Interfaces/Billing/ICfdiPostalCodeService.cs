using App.Core.Common;
using App.Core.DTOs.Billing.Mexico;

namespace App.Core.Interfaces.Billing;

public interface ICfdiPostalCodeService
{
    Task<Result<CfdiPostalCodeDto>> GetByCodeAsync(string postalCode);
    Task<bool> ExistsAsync(string postalCode);
}
