using App.Core.Interfaces;
using App.Models.Data.Contexts;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;

namespace App.Services.Obras;

public class ObraFolioService : IObraFolioService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IDateTime _dateTimeService;
    private readonly ICompanySettingsService _companySettingsService;

    public ObraFolioService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IDateTime dateTimeService,
        ICompanySettingsService companySettingsService)
    {
        _contextFactory = contextFactory;
        _dateTimeService = dateTimeService;
        _companySettingsService = companySettingsService;
    }

    public async Task<(int Anio, int Numero)> GenerarSiguienteFolioAsync(CancellationToken cancellationToken = default)
    {
        var timeZone = await _companySettingsService.GetCurrentTimeZoneAsync();
        var anio = TimeZoneInfo.ConvertTimeFromUtc(_dateTimeService.Now, timeZone).Year;

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var maxNumero = await context.Obras
            .Where(o => o.FolioAnio == anio)
            .Select(o => (int?)o.FolioNumero)
            .MaxAsync(cancellationToken) ?? 0;

        return (anio, maxNumero + 1);
    }
}
