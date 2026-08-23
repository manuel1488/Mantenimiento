using App.Core.Common;
using App.Services.Cotizaciones;

using NUnit.Framework;

namespace App.Services.Tests.Cotizaciones;

[TestFixture]
public class CotizacionIntegrityHashServiceTests
{
    private CotizacionIntegrityHashService _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new CotizacionIntegrityHashService();

    [Test]
    public void Compute_MatchesStaticHasher_ForSameInputs()
    {
        var fecha = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var lineas = new[] { new CotizacionIntegrityLinea("Pintura", "M2", 10m, 100m, 1000m) };

        var fromService = _sut.Compute(2026, 1, 1, fecha, 1000m, true, 16m, 160m, 1160m, lineas);
        var fromStatic = CotizacionIntegrityHasher.Compute(2026, 1, 1, fecha, 1000m, true, 16m, 160m, 1160m, lineas);

        Assert.That(fromService, Is.EqualTo(fromStatic),
            "The service must not alter the algorithm — it exists only to make callers mockable");
    }

    [Test]
    public void Compute_IsDeterministic_AcrossCalls()
    {
        var fecha = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var lineas = new[] { new CotizacionIntegrityLinea("Pintura", "M2", 10m, 100m, 1000m) };

        var first = _sut.Compute(2026, 1, 1, fecha, 1000m, true, 16m, 160m, 1160m, lineas);
        var second = _sut.Compute(2026, 1, 1, fecha, 1000m, true, 16m, 160m, 1160m, lineas);

        Assert.That(first, Is.EqualTo(second));
    }
}
