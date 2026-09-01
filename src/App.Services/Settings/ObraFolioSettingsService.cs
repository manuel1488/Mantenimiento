using AutoMapper;
using App.Core.DTOs.Settings;
using App.Core.Interfaces;
using App.Models.Data.Contexts;
using App.Models.Settings;
using App.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.Services.Settings;

public class ObraFolioSettingsService : IObraFolioSettingsService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<ObraFolioSettingsService> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;

    public ObraFolioSettingsService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMapper mapper,
        ILogger<ObraFolioSettingsService> logger,
        ICurrentUserService currentUserService,
        IDateTime dateTime)
    {
        _contextFactory = contextFactory;
        _mapper = mapper;
        _logger = logger;
        _currentUserService = currentUserService;
        _dateTime = dateTime;
    }

    public async Task<ObraFolioSettingsDto> GetSettingsAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var settings = await context.ObraFolioSettings
                .AsNoTracking()
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync();

            return settings != null ? _mapper.Map<ObraFolioSettingsDto>(settings) : new ObraFolioSettingsDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Obra folio settings");
            throw;
        }
    }

    public async Task<ObraFolioSettingsDto> UpdateSettingsAsync(UpdateObraFolioSettingsDto updateDto)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var settings = await context.ObraFolioSettings.OrderBy(x => x.Id).FirstOrDefaultAsync();

            if (settings == null)
            {
                settings = new ObraFolioSettings
                {
                    CreatedBy = await _currentUserService.GetFullNameAsync() ?? "Unknown",
                    CreatedAt = _dateTime.Now
                };
                context.ObraFolioSettings.Add(settings);
            }

            _mapper.Map(updateDto, settings);

            settings.ModifiedBy = await _currentUserService.GetFullNameAsync() ?? "Unknown";
            settings.ModifiedAt = _dateTime.Now;

            await context.SaveChangesAsync();

            return _mapper.Map<ObraFolioSettingsDto>(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating Obra folio settings");
            throw;
        }
    }
}
