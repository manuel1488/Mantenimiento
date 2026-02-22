using App.Core.Common;
using App.Core.DTOs.Shop.CashStation;

namespace App.Core.Interfaces.Shop;

public interface ICashStationService
{
    Task<IList<CashStationDto>> GetByLocationAsync(int locationId, bool? isActive = null);
    Task<(int Total, IList<CashStationDto> Items)> GetAllAsync(int page, int pageSize, int? locationId = null);
    Task<Result<CashStationDto>> CreateAsync(CreateCashStationDto dto);
    Task<Result<CashStationDto>> UpdateAsync(int id, UpdateCashStationDto dto);
    Task<Result> DeleteAsync(int id);
}
