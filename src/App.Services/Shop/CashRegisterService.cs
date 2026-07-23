using App.Core.Common;
using App.Core.DTOs.Shop;
using App.Core.Enums.Shop;
using App.Core.Interfaces.Identity;
using App.Core.Interfaces.Shop;
using App.Models.Data.Contexts;
using App.Models.Settings;
using App.Models.Shop;
using App.Shared.Services;

using AutoMapper;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

using PaymentMethodType = App.Core.Enums.Shop.PaymentMethodType;
using SaleStatus = App.Core.Enums.Shop.SaleStatus;

namespace App.Services.Shop;

public class CashRegisterService : ICashRegisterService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<CashRegisterService> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;
    private readonly IStringLocalizer<CashRegisterService> L;
    private readonly ICashierProfileService _cashierProfileService;

    // MXN denomination catalog — static, no DB needed since MXN denominations never change
    private static readonly (DenominationType Type, decimal Value)[] DenominationCatalog =
    [
        (DenominationType.Bill, 1000m),
        (DenominationType.Bill,  500m),
        (DenominationType.Bill,  200m),
        (DenominationType.Bill,  100m),
        (DenominationType.Bill,   50m),
        (DenominationType.Bill,   20m),
        (DenominationType.Coin,   20m),
        (DenominationType.Coin,   10m),
        (DenominationType.Coin,    5m),
        (DenominationType.Coin,    2m),
        (DenominationType.Coin,    1m),
        (DenominationType.Coin, 0.50m),
        (DenominationType.Coin, 0.20m),
        (DenominationType.Coin, 0.10m),
    ];

    public CashRegisterService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMapper mapper,
        ILogger<CashRegisterService> logger,
        ICurrentUserService currentUserService,
        IDateTime dateTime,
        IStringLocalizer<CashRegisterService> localizer,
        ICashierProfileService cashierProfileService)
    {
        _contextFactory = contextFactory;
        _mapper = mapper;
        _logger = logger;
        _currentUserService = currentUserService;
        _dateTime = dateTime;
        L = localizer;
        _cashierProfileService = cashierProfileService;
    }

    public async Task<Result<CashRegisterDto?>> GetActiveCashRegisterAsync(int locationId, string userId)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var cashRegister = await context.CashRegisters
                .AsNoTracking()
                .Include(c => c.Location)
                .Include(c => c.CashStation)
                .Include(c => c.Movements)
                .Include(c => c.Denominations)
                .FirstOrDefaultAsync(c =>
                    c.LocationId == locationId &&
                    c.UserId == userId &&
                    c.Status == CashRegisterStatus.Open);

            if (cashRegister == null)
                return Result<CashRegisterDto?>.Success(null);

            var dto = await BuildCashRegisterDtoAsync(context, cashRegister);
            return Result<CashRegisterDto?>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active cash register for location {LocationId}, user {UserId}", locationId, userId);
            return Result<CashRegisterDto?>.Failure(L["Error retrieving cash register"]);
        }
    }

    public async Task<Result<CashRegisterDto?>> GetActiveCashRegisterByUserAsync(string userId)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var cashRegister = await context.CashRegisters
                .AsNoTracking()
                .Include(c => c.Location)
                .Include(c => c.CashStation)
                .Include(c => c.Movements)
                .Include(c => c.Denominations)
                .FirstOrDefaultAsync(c =>
                    c.UserId == userId &&
                    c.Status == CashRegisterStatus.Open);

            if (cashRegister == null)
                return Result<CashRegisterDto?>.Success(null);

            var dto = await BuildCashRegisterDtoAsync(context, cashRegister);
            return Result<CashRegisterDto?>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active cash register for user {UserId}", userId);
            return Result<CashRegisterDto?>.Failure(L["Error retrieving cash register"]);
        }
    }

    public async Task<Result<CashRegisterDto>> OpenCashRegisterAsync(OpenCashRegisterDto dto)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var userId = await _currentUserService.GetUserIdAsync();
            var now = _dateTime.Now;

            if (!await _currentUserService.GetIsGlobalAccessAsync())
            {
                var isCashier = await _cashierProfileService.IsActiveCashierAsync(userId!);
                if (!isCashier)
                    return Result<CashRegisterDto>.Failure(L["You are not authorized as a cashier. Please contact an administrator."]);
            }

            var existing = await context.CashRegisters
                .AnyAsync(c =>
                    c.LocationId == dto.LocationId &&
                    c.UserId == userId &&
                    c.Status == CashRegisterStatus.Open);

            if (existing)
                return Result<CashRegisterDto>.Failure(L["A cash register is already open for this location"]);

            // Validate and verify cash station
            var station = await context.CashStations
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == dto.CashStationId && s.IsActive);

            if (station == null)
                return Result<CashRegisterDto>.Failure(L["Cash station not found or inactive"]);

            if (!await _currentUserService.GetIsGlobalAccessAsync())
            {
                var profile = await context.CashierProfiles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (profile == null || station.LocationId != profile.LocationId)
                    return Result<CashRegisterDto>.Failure(L["Cash station does not belong to your assigned location"]);
            }

            var stationBusy = await context.CashRegisters
                .AnyAsync(c => c.CashStationId == dto.CashStationId && c.Status == CashRegisterStatus.Open);

            if (stationBusy)
                return Result<CashRegisterDto>.Failure(L["This cash station is already open"]);

            var cashRegister = new CashRegister
            {
                LocationId = station.LocationId,
                CashStationId = dto.CashStationId,
                UserId = userId,
                Status = CashRegisterStatus.Open,
                InitialFund = dto.InitialFund,
                OpeningNotes = dto.Notes,
                OpenedAt = now,
                CreatedBy = await _currentUserService.GetFullNameAsync(),
                CreatedAt = now,
                ModifiedBy = await _currentUserService.GetFullNameAsync(),
                ModifiedAt = now
            };

            context.CashRegisters.Add(cashRegister);
            await context.SaveChangesAsync();

            var saved = await context.CashRegisters
                .AsNoTracking()
                .Include(c => c.Location)
                .Include(c => c.CashStation)
                .Include(c => c.Movements)
                .Include(c => c.Denominations)
                .FirstAsync(c => c.Id == cashRegister.Id);

            var result = await BuildCashRegisterDtoAsync(context, saved);
            return Result<CashRegisterDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening cash register for location {LocationId}", dto.LocationId);
            return Result<CashRegisterDto>.Failure(L["Error opening cash register"]);
        }
    }

    public async Task<Result<CashRegisterDto>> CloseCashRegisterAsync(CloseCashRegisterDto dto)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                var cashRegister = await context.CashRegisters
                    .Include(c => c.Location)
                    .Include(c => c.Movements)
                    .Include(c => c.Denominations)
                    .FirstOrDefaultAsync(c => c.Id == dto.CashRegisterId);

                if (cashRegister == null)
                    return Result<CashRegisterDto>.Failure(L["Cash register not found"]);

                if (cashRegister.Status == CashRegisterStatus.Closed)
                    return Result<CashRegisterDto>.Failure(L["Cash register is already closed"]);

                var now = _dateTime.Now;

                // Save denomination counts (only non-zero quantities)
                foreach (var (denominationValue, quantity) in dto.DenominationCounts.Where(kvp => kvp.Value > 0))
                {
                    var catalogEntry = DenominationCatalog.FirstOrDefault(d => d.Value == denominationValue);
                    if (catalogEntry == default) continue;

                    context.CashRegisterDenominations.Add(new CashRegisterDenomination
                    {
                        CashRegisterId = cashRegister.Id,
                        DenominationType = catalogEntry.Type,
                        DenominationValue = denominationValue,
                        Quantity = quantity,
                        CreatedBy = await _currentUserService.GetFullNameAsync(),
                        CreatedAt = now,
                        ModifiedBy = await _currentUserService.GetFullNameAsync(),
                        ModifiedAt = now
                    });
                }

                cashRegister.Status = CashRegisterStatus.Closed;
                cashRegister.ClosingNotes = dto.ClosingNotes;
                cashRegister.ClosedAt = now;
                cashRegister.ModifiedBy = await _currentUserService.GetFullNameAsync();
                cashRegister.ModifiedAt = now;

                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Reload denominations after save
                await context.Entry(cashRegister).Collection(c => c.Denominations).LoadAsync();

                var result = await BuildCashRegisterDtoAsync(context, cashRegister);
                return Result<CashRegisterDto>.Success(result);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error closing cash register {Id}", dto.CashRegisterId);
                return Result<CashRegisterDto>.Failure(L["Error closing cash register"]);
            }
        });
    }

    public async Task<Result<CashRegisterMovementDto>> AddMovementAsync(AddCashRegisterMovementDto dto)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var cashRegister = await context.CashRegisters
                .FirstOrDefaultAsync(c => c.Id == dto.CashRegisterId && c.Status == CashRegisterStatus.Open);

            if (cashRegister == null)
                return Result<CashRegisterMovementDto>.Failure(L["Active cash register not found"]);

            if (dto.Type == CashRegisterMovementType.Withdrawal)
            {
                var settingsResult = await GetSettingsAsync();
                if (settingsResult.IsSuccess && settingsResult.Value != null)
                {
                    var s = settingsResult.Value;
                    if (s.MaxWithdrawalAmount.HasValue &&
                        dto.Amount > s.MaxWithdrawalAmount.Value &&
                        s.IsStrictWithdrawalLimit)
                    {
                        return Result<CashRegisterMovementDto>.Failure(
                            L["Withdrawal amount exceeds the maximum allowed ({0:C})", s.MaxWithdrawalAmount.Value]);
                    }
                }
            }

            int? withdrawalNumber = null;
            if (dto.Type == CashRegisterMovementType.Withdrawal)
            {
                var existingCount = await context.CashRegisterMovements
                    .CountAsync(m => m.CashRegisterId == dto.CashRegisterId &&
                                     m.MovementType == CashRegisterMovementType.Withdrawal);
                withdrawalNumber = existingCount + 1;
            }

            var now = _dateTime.Now;
            var movement = new CashRegisterMovement
            {
                CashRegisterId = dto.CashRegisterId,
                MovementType = dto.Type,
                Amount = dto.Amount,
                Reason = dto.Reason,
                WithdrawalNumber = withdrawalNumber,
                CreatedBy = await _currentUserService.GetFullNameAsync(),
                CreatedAt = now,
                ModifiedBy = await _currentUserService.GetFullNameAsync(),
                ModifiedAt = now
            };

            context.CashRegisterMovements.Add(movement);
            await context.SaveChangesAsync();

            return Result<CashRegisterMovementDto>.Success(_mapper.Map<CashRegisterMovementDto>(movement));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding movement to cash register {Id}", dto.CashRegisterId);
            return Result<CashRegisterMovementDto>.Failure(L["Error adding movement"]);
        }
    }

    public async Task<Result<CashRegisterReportDto>> GetReportDataAsync(long cashRegisterId)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var cashRegister = await context.CashRegisters
                .AsNoTracking()
                .Include(c => c.Location)
                .Include(c => c.Movements)
                .Include(c => c.Denominations)
                .FirstOrDefaultAsync(c => c.Id == cashRegisterId);

            if (cashRegister == null)
                return Result<CashRegisterReportDto>.Failure(L["Cash register not found"]);

            var cashMethodIds = await context.PaymentMethods
                .AsNoTracking()
                .Where(pm => pm.Type == PaymentMethodType.Cash)
                .Select(pm => pm.Id)
                .ToListAsync();

            var salePayments = await context.SalePayments
                .AsNoTracking()
                .Include(sp => sp.PaymentMethod)
                .Where(sp => sp.Sale.CashRegisterId == cashRegisterId &&
                             sp.Sale.Status != SaleStatus.Cancelled)
                .ToListAsync();

            var totalCashSales = salePayments
                .Where(sp => cashMethodIds.Contains(sp.PaymentMethodId))
                .Sum(sp => sp.Amount);

            var totalDeposits = cashRegister.Movements
                .Where(m => m.MovementType == CashRegisterMovementType.Deposit)
                .Sum(m => m.Amount);

            var totalWithdrawals = cashRegister.Movements
                .Where(m => m.MovementType == CashRegisterMovementType.Withdrawal)
                .Sum(m => m.Amount);

            var expectedCash = cashRegister.InitialFund + totalCashSales + totalDeposits - totalWithdrawals;
            var countedCash = cashRegister.Denominations.Sum(d => d.DenominationValue * d.Quantity);

            var paymentSummary = salePayments
                .GroupBy(sp => new { sp.PaymentMethodId, sp.PaymentMethod.Name, sp.PaymentMethod.Icon })
                .Select(g => new PaymentMethodSummaryDto
                {
                    PaymentMethodId = g.Key.PaymentMethodId,
                    PaymentMethodName = g.Key.Name,
                    PaymentMethodIcon = g.Key.Icon,
                    TotalAmount = g.Sum(sp => sp.Amount),
                    TransactionCount = g.Count()
                }).ToList();

            var report = new CashRegisterReportDto
            {
                CashRegisterId = cashRegister.Id,
                LocationName = cashRegister.Location?.Name ?? string.Empty,
                CashierName = cashRegister.CreatedBy,
                CompanyName = string.Empty,
                OpenedAt = cashRegister.OpenedAt,
                ClosedAt = cashRegister.ClosedAt,
                InitialFund = cashRegister.InitialFund,
                TotalCashSales = totalCashSales,
                TotalDeposits = totalDeposits,
                TotalWithdrawals = totalWithdrawals,
                ExpectedCash = expectedCash,
                CountedCash = countedCash,
                Difference = countedCash - expectedCash,
                PaymentSummary = paymentSummary,
                Movements = _mapper.Map<List<CashRegisterMovementDto>>(cashRegister.Movements.OrderBy(m => m.CreatedAt).ToList()),
                Denominations = _mapper.Map<List<CashRegisterDenominationDto>>(cashRegister.Denominations.ToList()),
                OpeningNotes = cashRegister.OpeningNotes,
                ClosingNotes = cashRegister.ClosingNotes
            };

            return Result<CashRegisterReportDto>.Success(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting report data for cash register {Id}", cashRegisterId);
            return Result<CashRegisterReportDto>.Failure(L["Error generating report"]);
        }
    }

    public async Task<(int TotalCount, IList<CashRegisterDto> Items)> GetHistoryAsync(
        int? locationId,
        int page = 1,
        int pageSize = 20,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var query = context.CashRegisters
                .AsNoTracking()
                .Include(c => c.Location)
                .Include(c => c.Movements)
                .Include(c => c.Denominations)
                .AsQueryable();

            if (locationId.HasValue && locationId.Value > 0)
                query = query.Where(c => c.LocationId == locationId.Value);

            if (startDate.HasValue)
                query = query.Where(c => c.OpenedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(c => c.OpenedAt <= endDate.Value.AddDays(1));

            var total = await query.CountAsync();

            var registers = await query
                .OrderByDescending(c => c.OpenedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = new List<CashRegisterDto>();
            foreach (var reg in registers)
            {
                var dto = await BuildCashRegisterDtoAsync(context, reg);
                dtos.Add(dto);
            }

            return (total, dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cash register history for location {LocationId}", locationId);
            return (0, []);
        }
    }

    public async Task<Result<CashRegisterSettingsDto>> GetSettingsAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var settings = await context.CashRegisterSettings
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (settings == null)
            {
                // Return default settings without seeding — caller decides if they need to persist
                return Result<CashRegisterSettingsDto>.Success(new CashRegisterSettingsDto
                {
                    Id = 0,
                    MaxWithdrawalAmount = null,
                    IsStrictWithdrawalLimit = false,
                    MaxCashLimit = null,
                    IsStrictCashLimit = false,
                    DefaultInitialFund = null
                });
            }

            return Result<CashRegisterSettingsDto>.Success(_mapper.Map<CashRegisterSettingsDto>(settings));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cash register settings");
            return Result<CashRegisterSettingsDto>.Failure(L["Error retrieving settings"]);
        }
    }

    public async Task<Result<CashRegisterSettingsDto>> UpdateSettingsAsync(UpdateCashRegisterSettingsDto dto)
    {
        try
        {
            if (dto.MaxWithdrawalAmount.HasValue && dto.MaxWithdrawalAmount <= 0)
                return Result<CashRegisterSettingsDto>.Failure(L["Maximum withdrawal amount must be greater than zero"]);

            if (dto.IsStrictWithdrawalLimit && !dto.MaxWithdrawalAmount.HasValue)
                return Result<CashRegisterSettingsDto>.Failure(L["Withdrawal limit amount is required when strict withdrawal limit is enabled"]);

            if (dto.IsStrictCashLimit && (dto.MaxCashLimit == null || dto.MaxCashLimit <= 0))
                return Result<CashRegisterSettingsDto>.Failure(L["Cash limit amount must be greater than zero when strict cash limit is enabled"]);

            await using var context = await _contextFactory.CreateDbContextAsync();
            var now = _dateTime.Now;

            var settings = await context.CashRegisterSettings.FirstOrDefaultAsync();

            if (settings == null)
            {
                settings = new CashRegisterSettings
                {
                    MaxWithdrawalAmount = dto.MaxWithdrawalAmount,
                    IsStrictWithdrawalLimit = dto.IsStrictWithdrawalLimit,
                    MaxCashLimit = dto.MaxCashLimit,
                    IsStrictCashLimit = dto.IsStrictCashLimit,
                    DefaultInitialFund = dto.DefaultInitialFund,
                    CreatedBy = await _currentUserService.GetFullNameAsync(),
                    CreatedAt = now,
                    ModifiedBy = await _currentUserService.GetFullNameAsync(),
                    ModifiedAt = now
                };
                context.CashRegisterSettings.Add(settings);
            }
            else
            {
                settings.MaxWithdrawalAmount = dto.MaxWithdrawalAmount;
                settings.IsStrictWithdrawalLimit = dto.IsStrictWithdrawalLimit;
                settings.MaxCashLimit = dto.MaxCashLimit;
                settings.IsStrictCashLimit = dto.IsStrictCashLimit;
                settings.DefaultInitialFund = dto.DefaultInitialFund;
                settings.ModifiedBy = await _currentUserService.GetFullNameAsync();
                settings.ModifiedAt = now;
            }

            await context.SaveChangesAsync();
            return Result<CashRegisterSettingsDto>.Success(_mapper.Map<CashRegisterSettingsDto>(settings));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cash register settings");
            return Result<CashRegisterSettingsDto>.Failure(L["Error saving settings"]);
        }
    }

    // ────────────────────────────────────────────────────────────────────────────
    // Private helpers
    // ────────────────────────────────────────────────────────────────────────────

    private async Task<CashRegisterDto> BuildCashRegisterDtoAsync(
        ApplicationDbContext context,
        CashRegister cashRegister)
    {
        var dto = _mapper.Map<CashRegisterDto>(cashRegister);
        dto.UserName = cashRegister.CreatedBy;

        if (cashRegister.CashStation != null)
            dto.CashStationName = cashRegister.CashStation.Name;
        else if (cashRegister.CashStationId.HasValue)
        {
            var stationName = await context.CashStations
                .AsNoTracking()
                .Where(s => s.Id == cashRegister.CashStationId.Value)
                .Select(s => s.Name)
                .FirstOrDefaultAsync();
            dto.CashStationName = stationName;
        }

        var cashMethodIds = await context.PaymentMethods
            .AsNoTracking()
            .Where(pm => pm.Type == PaymentMethodType.Cash)
            .Select(pm => pm.Id)
            .ToListAsync();

        var salePayments = await context.SalePayments
            .AsNoTracking()
            .Include(sp => sp.PaymentMethod)
            .Where(sp => sp.Sale.CashRegisterId == cashRegister.Id &&
                         sp.Sale.Status != SaleStatus.Cancelled)
            .ToListAsync();

        dto.TotalCashSales = salePayments
            .Where(sp => cashMethodIds.Contains(sp.PaymentMethodId))
            .Sum(sp => sp.Amount);

        dto.TotalDeposits = cashRegister.Movements
            .Where(m => m.MovementType == CashRegisterMovementType.Deposit)
            .Sum(m => m.Amount);

        dto.TotalWithdrawals = cashRegister.Movements
            .Where(m => m.MovementType == CashRegisterMovementType.Withdrawal)
            .Sum(m => m.Amount);

        dto.ExpectedCash = cashRegister.InitialFund
                           + dto.TotalCashSales
                           + dto.TotalDeposits
                           - dto.TotalWithdrawals;

        dto.CountedCash = cashRegister.Denominations?.Sum(d => d.DenominationValue * d.Quantity) ?? 0;
        dto.Difference = dto.CountedCash - dto.ExpectedCash;

        dto.PaymentSummary = salePayments
            .GroupBy(sp => new { sp.PaymentMethodId, sp.PaymentMethod.Name, sp.PaymentMethod.Icon })
            .Select(g => new PaymentMethodSummaryDto
            {
                PaymentMethodId = g.Key.PaymentMethodId,
                PaymentMethodName = g.Key.Name,
                PaymentMethodIcon = g.Key.Icon,
                TotalAmount = g.Sum(sp => sp.Amount),
                TransactionCount = g.Count()
            }).ToList();

        return dto;
    }
}
