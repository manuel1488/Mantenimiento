using App.Core.Interfaces;
using App.Models.Data.Contexts;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.Services.Seeders;

public class UnidadMedidaSatLinker : IUnidadMedidaSatLinker
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<UnidadMedidaSatLinker> _logger;
    private readonly IDateTime _dateTimeService;
    private const string SystemUser = "System";

    /// <summary>Mapeo Código de UnidadMedida -> Código de clave SAT, solo para las unidades sembradas por defecto.</summary>
    private static readonly Dictionary<string, string> DefaultSatCodeByUnidadCodigo = new()
    {
        ["PZA"] = "H87",
        ["SRV"] = "E48",
        ["KIT"] = "KT",
        ["M"] = "MTR",
        ["M2"] = "MTK",
        ["M3"] = "MTQ",
        ["KM"] = "KMT",
        ["KG"] = "KGM",
        ["TON"] = "TNE",
        ["L"] = "LTR",
        ["HR"] = "HUR",
        ["DIA"] = "DAY",
        ["MES"] = "MON",
        ["JGO"] = "SET",
        ["VIS"] = "E48",
    };

    public UnidadMedidaSatLinker(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<UnidadMedidaSatLinker> logger,
        IDateTime dateTimeService)
    {
        _contextFactory = contextFactory;
        _logger = logger;
        _dateTimeService = dateTimeService;
    }

    public async Task LinkAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var pendingUnidades = await context.UnidadesMedida
                .Where(u => u.ClaveUnidadSatId == null && DefaultSatCodeByUnidadCodigo.Keys.Contains(u.Codigo))
                .ToListAsync();

            if (pendingUnidades.Count == 0)
                return;

            var satIds = await context.ClavesUnidadSatCatalogo
                .AsNoTracking()
                .ToDictionaryAsync(c => c.Codigo, c => c.Id);

            var now = _dateTimeService.Now;
            var linkedCount = 0;
            foreach (var unidad in pendingUnidades)
            {
                if (DefaultSatCodeByUnidadCodigo.TryGetValue(unidad.Codigo, out var satCode)
                    && satIds.TryGetValue(satCode, out var satId))
                {
                    unidad.ClaveUnidadSatId = satId;
                    unidad.ModifiedBy = SystemUser;
                    unidad.ModifiedAt = now;
                    linkedCount++;
                }
            }

            if (linkedCount > 0)
            {
                await context.SaveChangesAsync();
                _logger.LogInformation("Linked {Count} default unit measures to their SAT unit code", linkedCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error linking default unit measures to SAT unit codes");
            throw;
        }
    }
}
