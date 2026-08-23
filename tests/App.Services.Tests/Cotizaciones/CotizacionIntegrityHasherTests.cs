using App.Core.Common;

using NUnit.Framework;

namespace App.Services.Tests.Cotizaciones;

[TestFixture]
public class CotizacionIntegrityHasherTests
{
    private static readonly DateTime Fecha = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    private static string ComputeSample(
        int? folioAnio = 2026,
        int? folioNumero = 1,
        int clienteId = 1,
        DateTime? fecha = null,
        decimal subtotal = 1000m,
        bool incluirIva = true,
        decimal ivaTasa = 16m,
        decimal ivaMonto = 160m,
        decimal total = 1160m,
        IEnumerable<CotizacionIntegrityLinea>? lineas = null) => CotizacionIntegrityHasher.Compute(
            folioAnio, folioNumero, clienteId, fecha ?? Fecha,
            subtotal, incluirIva, ivaTasa, ivaMonto, total,
            lineas ?? [new CotizacionIntegrityLinea("Pintura", "M2", 10m, 100m, 1000m)]);

    [Test]
    public void Compute_SameInputs_ProducesSameHash()
    {
        var hash1 = ComputeSample();
        var hash2 = ComputeSample();

        Assert.That(hash1, Is.EqualTo(hash2), "The hash must be deterministic for identical inputs");
    }

    [Test]
    public void Compute_ReturnsSha256HexString()
    {
        var hash = ComputeSample();

        Assert.That(hash.Length, Is.EqualTo(64), "SHA-256 hex-encoded output must always be 64 characters");
        Assert.That(hash, Does.Match("^[0-9a-f]{64}$"), "Must be lowercase hex");
    }

    [Test]
    public void Compute_DifferentLineaCantidad_ProducesDifferentHash()
    {
        var original = ComputeSample();
        var changed = ComputeSample(lineas: [new CotizacionIntegrityLinea("Pintura", "M2", 11m, 100m, 1100m)]);

        Assert.That(changed, Is.Not.EqualTo(original), "Any change to a línea must change the fingerprint");
    }

    [Test]
    public void Compute_DifferentTotal_ProducesDifferentHash()
    {
        var original = ComputeSample();
        var changed = ComputeSample(total: 9999m);

        Assert.That(changed, Is.Not.EqualTo(original));
    }

    [Test]
    public void Compute_DifferentClienteId_ProducesDifferentHash()
    {
        var original = ComputeSample();
        var changed = ComputeSample(clienteId: 2);

        Assert.That(changed, Is.Not.EqualTo(original));
    }

    [Test]
    public void Compute_DifferentFolio_ProducesDifferentHash()
    {
        var original = ComputeSample();
        var changed = ComputeSample(folioNumero: 2);

        Assert.That(changed, Is.Not.EqualTo(original));
    }

    [Test]
    public void Compute_NullFolio_DoesNotThrow()
    {
        // Cotizaciones created before the folio feature existed have no FolioAnio/FolioNumero.
        Assert.DoesNotThrow(() => ComputeSample(folioAnio: null, folioNumero: null));
    }

    [Test]
    public void Compute_ManyLineas_StillProducesFixedLengthHash()
    {
        var manyLineas = Enumerable.Range(1, 500)
            .Select(i => new CotizacionIntegrityLinea($"Servicio {i}", "M2", i, i * 10m, i * i * 10m));

        var hash = ComputeSample(lineas: manyLineas);

        Assert.That(hash.Length, Is.EqualTo(64), "Hash length must not grow with the number of línea items");
    }

    [Test]
    public void Compute_DifferentLineaOrder_ProducesDifferentHash()
    {
        var lineaA = new CotizacionIntegrityLinea("Pintura", "M2", 10m, 100m, 1000m);
        var lineaB = new CotizacionIntegrityLinea("Yeso", "M2", 5m, 50m, 250m);

        var orderAb = ComputeSample(lineas: [lineaA, lineaB]);
        var orderBa = ComputeSample(lineas: [lineaB, lineaA]);

        Assert.That(orderAb, Is.Not.EqualTo(orderBa),
            "Línea order is part of the saved snapshot, so swapping it must change the fingerprint");
    }
}
