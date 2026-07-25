using AutoMapper;
using App.Core.Common;
using App.Core.DTOs.Settings;
using App.Core.Interfaces.Settings;
using App.Models.Data.Contexts;
using App.Models.Settings;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace App.Services.Settings;

public class InventorySettingsService : IInventorySettingsService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<InventorySettingsService> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;
    private readonly IStringLocalizer<InventorySettingsService> L;

    public InventorySettingsService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMapper mapper,
        ILogger<InventorySettingsService> logger,
        ICurrentUserService currentUserService,
        IDateTime dateTime,
        IStringLocalizer<InventorySettingsService> localizer)
    {
        _contextFactory = contextFactory;
        _mapper = mapper;
        _logger = logger;
        _currentUserService = currentUserService;
        _dateTime = dateTime;
        L = localizer;
    }

    public async Task<Result<InventorySettingsDto>> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            var settings = await context.InventorySettings
                .AsNoTracking()
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (settings == null)
            {
                return Result<InventorySettingsDto>.Success(new InventorySettingsDto
                {
                    ShowStockDuringPhysicalCount = true
                });
            }

            var dto = _mapper.Map<InventorySettingsDto>(settings);
            return Result<InventorySettingsDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving inventory settings");
            return Result<InventorySettingsDto>.Failure(L["An error occurred while retrieving inventory settings"]);
        }
    }

    public async Task<Result<InventorySettingsDto>> CreateOrUpdateSettingsAsync(
        UpdateInventorySettingsDto updateDto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            var settings = await context.InventorySettings
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (settings == null)
            {
                settings = new InventorySettings
                {
                    ShowStockDuringPhysicalCount = updateDto.ShowStockDuringPhysicalCount,
                    CreatedBy = await _currentUserService.GetFullNameAsync(),
                    CreatedAt = _dateTime.Now
                };

                context.InventorySettings.Add(settings);
            }
            else
            {
                settings.ShowStockDuringPhysicalCount = updateDto.ShowStockDuringPhysicalCount;
                settings.ModifiedBy = await _currentUserService.GetFullNameAsync();
                settings.ModifiedAt = _dateTime.Now;
            }

            await context.SaveChangesAsync(cancellationToken);

            var dto = _mapper.Map<InventorySettingsDto>(settings);
            return Result<InventorySettingsDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving inventory settings");
            return Result<InventorySettingsDto>.Failure(L["An error occurred while saving inventory settings"]);
        }
    }
}
