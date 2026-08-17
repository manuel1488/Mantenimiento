using AutoMapper;

using App.Core.DTOs.Settings;
using App.Core.Interfaces;
using App.Models.Data.Contexts;
using App.Models.Settings;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.Services.Settings;

public class MinioConfiguracionService : IMinioConfiguracionService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<MinioConfiguracionService> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;

    public MinioConfiguracionService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMapper mapper,
        ILogger<MinioConfiguracionService> logger,
        ICurrentUserService currentUserService,
        IDateTime dateTime)
    {
        _contextFactory = contextFactory;
        _mapper = mapper;
        _logger = logger;
        _currentUserService = currentUserService;
        _dateTime = dateTime;
    }

    public async Task<MinioConfiguracionDto?> GetConfigAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var config = await context.MinioConfiguraciones
                .AsNoTracking()
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync();

            return config != null ? _mapper.Map<MinioConfiguracionDto>(config) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting MinIO configuration");
            throw;
        }
    }

    public async Task<MinioConfiguracionDto> UpdateConfigAsync(UpdateMinioConfiguracionDto updateDto)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var config = await context.MinioConfiguraciones
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync();

            if (config == null)
            {
                config = new MinioConfiguracion
                {
                    CreatedBy = await _currentUserService.GetFullNameAsync() ?? "Unknown",
                    CreatedAt = _dateTime.Now
                };
                context.MinioConfiguraciones.Add(config);
            }

            _mapper.Map(updateDto, config);

            config.ModifiedBy = await _currentUserService.GetFullNameAsync() ?? "Unknown";
            config.ModifiedAt = _dateTime.Now;

            await context.SaveChangesAsync();

            _logger.LogInformation("MinIO configuration updated by {User}", config.ModifiedBy);

            return _mapper.Map<MinioConfiguracionDto>(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating MinIO configuration");
            throw;
        }
    }
}
