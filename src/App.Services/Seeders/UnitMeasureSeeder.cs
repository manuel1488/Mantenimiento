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
            await using var _context = await _contextFactory.CreateDbContextAsync();
            
            if (!await _context.UnitMeasures.AnyAsync())
            {
                var now = DateTime.UtcNow;
                var metricUnits = new List<UnitMeasure>
                {
                    // Unidades de longitud métricas
                    CreateUnit("MM", "MX", "Milímetros", "Unidad de longitud métrica"),
                    CreateUnit("CM", "MX", "Centímetros", "Unidad de longitud métrica"),
                    CreateUnit("M", "MX", "Metros", "Unidad de longitud métrica"),
                    
                    // Unidades de peso métricas
                    CreateUnit("G", "MX", "Gramos", "Unidad de peso métrica"),
                    CreateUnit("KG", "MX", "Kilogramos", "Unidad de peso métrica"),
                    
                    // Unidades de volumen métricas
                    CreateUnit("ML", "MX", "Mililitros", "Unidad de volumen métrica"),
                    CreateUnit("L", "MX", "Litros", "Unidad de volumen métrica"),

                    // Unidades de área métricas
                    CreateUnit("M2", "MX", "Metros Cuadrados", "Unidad de área métrica"),
                    
                    // Otras unidades comunes
                    CreateUnit("PZA", "MX", "Piezas", "Unidad de conteo"),
                    CreateUnit("PAR", "MX", "Pares", "Unidad de conteo por pares"),
                    CreateUnit("JGO", "MX", "Juegos", "Unidad de conteo por juegos"),
                    CreateUnit("KIT", "MX", "Kit", "Conjunto de piezas relacionadas")
                };

                var imperialUnits = new List<UnitMeasure>
                {
                    // Unidades de longitud imperiales
                    CreateUnit("IN", "US", "Pulgadas", "Unidad de longitud imperial"),
                    CreateUnit("FT", "US", "Pies", "Unidad de longitud imperial"),
                    CreateUnit("YD", "US", "Yardas", "Unidad de longitud imperial"),
                    
                    // Unidades de peso imperiales
                    CreateUnit("OZ", "US", "Onzas", "Unidad de peso imperial"),
                    CreateUnit("LB", "US", "Libras", "Unidad de peso imperial"),
                    
                    // Unidades de volumen imperiales
                    CreateUnit("FLOZ", "US", "Onzas Fluidas", "Unidad de volumen imperial"),
                    CreateUnit("GAL", "US", "Galones", "Unidad de volumen imperial"),

                    // Unidades de área imperiales
                    CreateUnit("SQFT", "US", "Pies Cuadrados", "Unidad de área imperial"),
                    
                    // Otras unidades comunes
                    CreateUnit("PC", "US", "Piece", "Unidad de conteo"),
                    CreateUnit("PR", "US", "Pair", "Unidad de conteo por pares"),
                    CreateUnit("SET", "US", "Set", "Unidad de conteo por juegos"),
                    CreateUnit("KIT", "US", "Kit", "Conjunto de piezas relacionadas")
                };

                await _context.UnitMeasures.AddRangeAsync(metricUnits);
                await _context.UnitMeasures.AddRangeAsync(imperialUnits);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Unit measures seeded successfully");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding unit measures");
            throw;
        }
    }

    private UnitMeasure CreateUnit(string code, string countryCode, string name, string description)
    {
        return new UnitMeasure
        {
            Code = code,
            CountryCode = countryCode,
            Name = name,
            Description = description,
            CreatedBy = _systemUser,
            CreatedAt = DateTime.UtcNow
        };
    }
}