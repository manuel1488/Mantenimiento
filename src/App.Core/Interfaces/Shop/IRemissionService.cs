using App.Core.Common;
using App.Core.DTOs.Shop;

namespace App.Core.Interfaces.Shop;

public interface IRemissionService
{
    Task<(int TotalCount, IList<RemissionDto> Items)> GetRemissionsAsync(
        int page = 1,
        int pageSize = 10,
        string? search = null,
        long? customerId = null,
        string? status = null,
        int? locationId = null,
        DateTime? startDate = null,
        DateTime? endDate = null);

    Task<RemissionDto?> GetByIdAsync(long id);

    Task<Result<RemissionDto>> CreateAsync(CreateRemissionDto dto);

    Task<Result> CancelAsync(long id, string reason);

    // Consolidation
    Task<Result<List<RemissionDto>>> GetPendingByCustomerAsync(long customerId);

    Task<Result<long>> ConsolidateAsync(ConsolidateRemissionsDto dto);

    // PDF
    Task<byte[]> GeneratePdfAsync(long id);
}
