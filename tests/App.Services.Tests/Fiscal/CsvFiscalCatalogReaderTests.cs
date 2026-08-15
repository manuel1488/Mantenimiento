using App.Services.Data;

using Microsoft.Extensions.Logging.Abstractions;

using NUnit.Framework;

namespace App.Services.Tests.Fiscal;

/// <summary>
/// Verifies CsvFiscalCatalogReader against the actual SAT catalog CSVs shipped in
/// src/App.Web/Data/FiscalCatalogs (copied into the test output via the csproj Content item).
/// Also protects against the CSV files being edited/corrupted without noticing.
/// </summary>
[TestFixture]
[Category("Integration")]
public class CsvFiscalCatalogReaderTests
{
    private static string DataPath => Path.Combine(AppContext.BaseDirectory, "Data", "FiscalCatalogs");

    private CsvFiscalCatalogReader _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new CsvFiscalCatalogReader(DataPath, NullLogger<CsvFiscalCatalogReader>.Instance);
    }

    [Test]
    public async Task GetRegimenesFiscalesAsync_ReturnsAllOfficialSatRegimes()
    {
        var regimenes = (await _sut.GetRegimenesFiscalesAsync()).ToList();

        Assert.That(regimenes, Has.Count.EqualTo(19), "The official SAT catalog has 19 fiscal regimes");
        Assert.That(regimenes.Select(r => r.Codigo), Does.Contain("601"));
        Assert.That(regimenes.Select(r => r.Codigo), Does.Contain("626"));

        var regimenSimplificado = regimenes.Single(r => r.Codigo == "626");
        Assert.That(regimenSimplificado.Descripcion, Is.EqualTo("Régimen Simplificado de Confianza"));
    }

    [Test]
    public async Task GetUsosCfdiAsync_ReturnsAllOfficialSatUses()
    {
        var usos = (await _sut.GetUsosCfdiAsync()).ToList();

        Assert.That(usos, Has.Count.EqualTo(24), "The official SAT catalog has 24 CFDI uses");
        Assert.That(usos.Select(u => u.Codigo), Does.Contain("G03"));
        Assert.That(usos.Select(u => u.Codigo), Does.Contain("CN01"));
    }

    [Test]
    public async Task GetUsosCfdiAsync_CN01_OnlyAppliesToPayrollRegime()
    {
        // CN01 (Nómina) is the narrowest entry in the catalog — a good canary for
        // fiscal_regime_codes parsing regressions.
        var usos = await _sut.GetUsosCfdiAsync();

        var nomina = usos.Single(u => u.Codigo == "CN01");
        Assert.That(nomina.CodigosRegimenFiscal, Is.EqualTo("605"));
    }

    [Test]
    public async Task GetUsosCfdiAsync_G03_AppliesToMultipleRegimes()
    {
        var usos = await _sut.GetUsosCfdiAsync();

        var gastosEnGeneral = usos.Single(u => u.Codigo == "G03");
        var regimenes = gastosEnGeneral.CodigosRegimenFiscal!.Split(',');

        Assert.That(regimenes, Does.Contain("601"));
        Assert.That(regimenes, Does.Contain("626"));
        Assert.That(regimenes, Does.Not.Contain("605"), "G03 does not apply to payroll income");
    }

    [Test]
    public async Task MissingDataDirectory_ReturnsEmptyLists_DoesNotThrow()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"fiscal-catalogs-missing-{Guid.NewGuid():N}");
        var reader = new CsvFiscalCatalogReader(missingPath, NullLogger<CsvFiscalCatalogReader>.Instance);

        try
        {
            var regimenes = await reader.GetRegimenesFiscalesAsync();
            var usos = await reader.GetUsosCfdiAsync();

            Assert.That(regimenes, Is.Empty);
            Assert.That(usos, Is.Empty);
        }
        finally
        {
            if (Directory.Exists(missingPath))
                Directory.Delete(missingPath, recursive: true);
        }
    }
}
