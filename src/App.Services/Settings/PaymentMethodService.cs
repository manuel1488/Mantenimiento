using App.Core.Common;
using App.Core.DTOs.Settings;
using App.Core.Interfaces;
using App.Core.Interfaces.Settings;
using App.Models.Data.Contexts;
using App.Models.Settings;
using App.Shared.Services;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace App.Services.Settings;

public class PaymentMethodService : IPaymentMethodService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<PaymentMethodService> _logger;
    private readonly IStringLocalizer<PaymentMethodService> _localizer;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;

    public PaymentMethodService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMapper mapper,
        ILogger<PaymentMethodService> logger,
        IStringLocalizer<PaymentMethodService> localizer,
        ICurrentUserService currentUserService,
        IDateTime dateTime)
    {
        _contextFactory = contextFactory;
        _mapper = mapper;
        _logger = logger;
        _localizer = localizer;
        _currentUserService = currentUserService;
        _dateTime = dateTime;
    }

    public async Task<IList<PaymentMethodDto>> GetAllAsync(bool includeInactive = false)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var query = context.PaymentMethods.AsNoTracking();
            if (!includeInactive)
                query = query.Where(p => p.IsActive);

            var items = await query.OrderBy(p => p.SortOrder).ThenBy(p => p.Name).ToListAsync();
            return _mapper.Map<List<PaymentMethodDto>>(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment methods");
            throw;
        }
    }

    public async Task<IList<PaymentMethodDto>> GetActiveAsync()
        => await GetAllAsync(includeInactive: false);

    public async Task<PaymentMethodDto?> GetByIdAsync(int id)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var entity = await context.PaymentMethods.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            return entity == null ? null : _mapper.Map<PaymentMethodDto>(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment method {Id}", id);
            throw;
        }
    }

    public async Task<Result<PaymentMethodDto>> CreateAsync(CreatePaymentMethodDto dto)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var entity = _mapper.Map<PaymentMethod>(dto);
            await SetAuditFieldsAsync(entity);

            context.PaymentMethods.Add(entity);
            await context.SaveChangesAsync();

            return Result<PaymentMethodDto>.Success(_mapper.Map<PaymentMethodDto>(entity));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment method");
            return Result<PaymentMethodDto>.Failure(_localizer["Error creating payment method"]);
        }
    }

    public async Task<Result<PaymentMethodDto>> UpdateAsync(int id, UpdatePaymentMethodDto dto)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var entity = await context.PaymentMethods.FindAsync(id);
            if (entity == null)
                return Result<PaymentMethodDto>.Failure(_localizer["Payment method not found"]);

            _mapper.Map(dto, entity);
            entity.ModifiedBy = await _currentUserService.GetUserIdAsync() ?? "System";
            entity.ModifiedAt = _dateTime.Now;

            await context.SaveChangesAsync();

            return Result<PaymentMethodDto>.Success(_mapper.Map<PaymentMethodDto>(entity));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payment method {Id}", id);
            return Result<PaymentMethodDto>.Failure(_localizer["Error updating payment method"]);
        }
    }

    public async Task<Result> ToggleActiveAsync(int id)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var entity = await context.PaymentMethods.FindAsync(id);
            if (entity == null)
                return Result.Failure(_localizer["Payment method not found"]);

            entity.IsActive = !entity.IsActive;
            entity.ModifiedBy = await _currentUserService.GetUserIdAsync() ?? "System";
            entity.ModifiedAt = _dateTime.Now;

            await context.SaveChangesAsync();
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling payment method {Id}", id);
            return Result.Failure(_localizer["Error updating payment method"]);
        }
    }

    public async Task<Result> DeleteAsync(int id)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var entity = await context.PaymentMethods.FindAsync(id);
            if (entity == null)
                return Result.Failure(_localizer["Payment method not found"]);

            var isInUse = await context.SalePayments.AnyAsync(sp => sp.PaymentMethodId == id);
            if (isInUse)
                return Result.Failure(_localizer["Cannot delete a payment method that has been used in sales"]);

            context.PaymentMethods.Remove(entity);
            await context.SaveChangesAsync();
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting payment method {Id}", id);
            return Result.Failure(_localizer["Error deleting payment method"]);
        }
    }

    private async Task SetAuditFieldsAsync(PaymentMethod entity)
    {
        var user = await _currentUserService.GetUserIdAsync() ?? "System";
        var now = _dateTime.Now;
        entity.CreatedBy = user;
        entity.CreatedAt = now;
        entity.ModifiedBy = user;
        entity.ModifiedAt = now;
    }
}
