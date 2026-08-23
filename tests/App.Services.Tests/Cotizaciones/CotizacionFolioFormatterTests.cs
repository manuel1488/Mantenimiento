using App.Core.Common;

using NUnit.Framework;

namespace App.Services.Tests.Cotizaciones;

[TestFixture]
public class CotizacionFolioFormatterTests
{
    [Test]
    public void Format_WithAnioAndNumero_UsesPrefixAndPadding()
    {
        var folio = CotizacionFolioFormatter.Format(id: 99, folioAnio: 2026, folioNumero: 42, prefijo: "COT", digitos: 4);

        Assert.That(folio, Is.EqualTo("COT-2026-0042"));
    }

    [Test]
    public void Format_CustomPrefixAndDigits_AppliesBoth()
    {
        var folio = CotizacionFolioFormatter.Format(id: 1, folioAnio: 2026, folioNumero: 7, prefijo: "PRESUP", digitos: 6);

        Assert.That(folio, Is.EqualTo("PRESUP-2026-000007"));
    }

    [Test]
    public void Format_NumeroWiderThanDigits_IsNotTruncated()
    {
        var folio = CotizacionFolioFormatter.Format(id: 1, folioAnio: 2026, folioNumero: 123456, prefijo: "COT", digitos: 4);

        Assert.That(folio, Is.EqualTo("COT-2026-123456"));
    }

    [Test]
    public void Format_MissingFolio_FallsBackToRawId()
    {
        var folio = CotizacionFolioFormatter.Format(id: 4, folioAnio: null, folioNumero: null, prefijo: "COT", digitos: 4);

        Assert.That(folio, Is.EqualTo("#4"));
    }

    [Test]
    public void Format_BlankPrefijo_FallsBackToDefault()
    {
        var folio = CotizacionFolioFormatter.Format(id: 1, folioAnio: 2026, folioNumero: 1, prefijo: "   ", digitos: 4);

        Assert.That(folio, Is.EqualTo("COT-2026-0001"));
    }

    [Test]
    public void Format_NonPositiveDigitos_FallsBackToDefault()
    {
        var folio = CotizacionFolioFormatter.Format(id: 1, folioAnio: 2026, folioNumero: 1, prefijo: "COT", digitos: 0);

        Assert.That(folio, Is.EqualTo("COT-2026-0001"));
    }
}
