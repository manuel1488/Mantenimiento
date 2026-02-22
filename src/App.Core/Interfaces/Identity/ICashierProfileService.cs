using App.Core.Common;
using App.Core.DTOs.Identity;

namespace App.Core.Interfaces.Identity;

public interface ICashierProfileService
{
    Task<(int Total, IList<CashierProfileDto> Items)> GetAllAsync(int page, int pageSize, bool? isActive = null);
    Task<bool> IsActiveCashierAsync(string userId);
    Task<CashierProfileDto?> GetByUserIdAsync(string userId);
    Task<Result<CashierProfileDto>> CreateAsync(CreateCashierProfileDto dto);
    Task<Result<CashierProfileDto>> UpdateAsync(long id, UpdateCashierProfileDto dto);
    Task<Result> DeleteAsync(long id);
}
