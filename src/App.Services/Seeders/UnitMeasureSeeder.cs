using App.Core.Interfaces;
using App.Models.Data.Contexts;
using App.Models.Shop;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.Services.Seeders;

public class UnitMeasureSeeder : IUnitMeasureSeeder
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<UnitMeasureSeeder> _logger;
    private readonly string _systemUser = "System";

    public UnitMeasureSeeder(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<UnitMeasureSeeder> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            if (!await context.UnitMeasures.AnyAsync())
            {
                // Load all SAT units into a lookup dictionary
                var satUnitIds = await context.MexicoSatUnits
                    .AsNoTracking()
                    .ToDictionaryAsync(u => u.Code, u => u.Id);

                var metricUnits = new List<UnitMeasure>
                {
                    // Longitud
                    CreateUnit("MM",  "MX", "Milímetros",      "Unidad de longitud métrica",  satUnitIds, "MMT"),
                    CreateUnit("CM",  "MX", "Centímetros",      "Unidad de longitud métrica",  satUnitIds, "CMT"),
                    CreateUnit("M",   "MX", "Metros",           "Unidad de longitud métrica",  satUnitIds, "MTR"),
                    // Peso
                    CreateUnit("G",   "MX", "Gramos",           "Unidad de peso métrica",      satUnitIds, "GRM"),
                    CreateUnit("KG",  "MX", "Kilogramos",       "Unidad de peso métrica",      satUnitIds, "KGM"),
                    // Volumen
                    CreateUnit("ML",  "MX", "Mililitros",       "Unidad de volumen métrica",   satUnitIds, "MLT"),
                    CreateUnit("L",   "MX", "Litros",           "Unidad de volumen métrica",   satUnitIds, "LTR"),
                    // Área
                    CreateUnit("M2",  "MX", "Metros Cuadrados", "Unidad de área métrica",      satUnitIds, "MTK"),
                    // Conteo
                    CreateUnit("PZA", "MX", "Piezas",           "Unidad de conteo",            satUnitIds, "H87"),
                    CreateUnit("PAR", "MX", "Pares",            "Unidad de conteo por pares",  satUnitIds, "PR"),
                    CreateUnit("JGO", "MX", "Juegos",           "Unidad de conteo por juegos", satUnitIds, "SET"),
                    CreateUnit("KIT", "MX", "Kit",              "Conjunto de piezas relacionadas", satUnitIds, "KT"),
                };

                var imperialUnits = new List<UnitMeasure>
                {
                    CreateUnit("IN",   "US", "Pulgadas",      "Unidad de longitud imperial"),
                    CreateUnit("FT",   "US", "Pies",          "Unidad de longitud imperial"),
                    CreateUnit("YD",   "US", "Yardas",        "Unidad de longitud imperial"),
                    CreateUnit("OZ",   "US", "Onzas",         "Unidad de peso imperial"),
                    CreateUnit("LB",   "US", "Libras",        "Unidad de peso imperial"),
                    CreateUnit("FLOZ", "US", "Onzas Fluidas", "Unidad de volumen imperial"),
                    CreateUnit("GAL",  "US", "Galones",       "Unidad de volumen imperial"),
                    CreateUnit("SQFT", "US", "Pies Cuadrados","Unidad de área imperial"),
                    CreateUnit("PC",   "US", "Piece",         "Unidad de conteo"),
                    CreateUnit("PR",   "US", "Pair",          "Unidad de conteo por pares"),
                    CreateUnit("SET",  "US", "Set",           "Unidad de conteo por juegos"),
                    CreateUnit("KIT",  "US", "Kit",           "Conjunto de piezas relacionadas"),
                };

                await context.UnitMeasures.AddRangeAsync(metricUnits);
                await context.UnitMeasures.AddRangeAsync(imperialUnits);
                await context.SaveChangesAsync();

                _logger.LogInformation("Unit measures seeded successfully");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding unit measures");
            throw;
        }
    }

    private UnitMeasure CreateUnit(string code, string countryCode, string name, string description,
        Dictionary<string, int>? satUnitIds = null, string? satCode = null)
    {
        int? satUnitId = null;
        if (satUnitIds != null && satCode != null && satUnitIds.TryGetValue(satCode, out var satId))
            satUnitId = satId;

        return new UnitMeasure
        {
            Code = code,
            CountryCode = countryCode,
            Name = name,
            Description = description,
            MexicoSatUnitId = satUnitId,
            CreatedBy = _systemUser,
            CreatedAt = DateTime.UtcNow
        };
    }
}