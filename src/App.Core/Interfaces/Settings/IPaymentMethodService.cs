using App.Core.Common;
using App.Core.DTOs.Settings;

namespace App.Core.Interfaces.Settings;

public interface IPaymentMethodService
{
    Task<IList<PaymentMethodDto>> GetAllAsync(bool includeInactive = false);
    Task<IList<PaymentMethodDto>> GetActiveAsync();
    Task<PaymentMethodDto?> GetByIdAsync(int id);
    Task<Result<PaymentMethodDto>> CreateAsync(CreatePaymentMethodDto dto);
    Task<Result<PaymentMethodDto>> UpdateAsync(int id, UpdatePaymentMethodDto dto);
    Task<Result> ToggleActiveAsync(int id);
    Task<Result> DeleteAsync(int id);
}
