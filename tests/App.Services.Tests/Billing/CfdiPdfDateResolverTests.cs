using NUnit.Framework;

using App.Services.Billing;

namespace App.Services.Tests.Billing;

/// <summary>
/// Pure unit tests for the date resolution used to render "FECHA DE EMISIÓN" and
/// "FECHA DE CERTIFICACIÓN" on the CFDI PDF representation.
///
/// Bug context (invoice A183): the PDF showed both dates as the raw UTC stamp timestamp,
/// while the CFDI XML's Fecha node (and the invoice list UI) correctly showed the backdated
/// entry date converted to the issuer's local timezone.
/// </summary>
[TestFixture]
public class CfdiPdfDateResolverTests
{
    private static readonly TimeZoneInfo MexicoCityTz =
        TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City");

    [Test]
    public void Resolve_BackdatedInvoice_IssueDateUsesRequestedInvoiceDate_NotStampDate()
    {
        // 30/Jun/2026 13:30 local (UTC-6) = 19:30 UTC — the backdated entry date requested by the user
        var requestedUtc = new DateTime(2026, 6, 30, 19, 30, 0, DateTimeKind.Utc);
        // 01/Jul/2026 15:57 local (UTC-6) = 21:57 UTC — the actual PAC stamping time
        var stampUtc = new DateTime(2026, 7, 1, 21, 57, 0, DateTimeKind.Utc);

        var (issueDateLocal, stampDateLocal) = CfdiPdfDateResolver.Resolve(requestedUtc, stampUtc, MexicoCityTz);

        Assert.That(issueDateLocal, Is.EqualTo(new DateTime(2026, 6, 30, 13, 30, 0)),
            "issue_date must equal the backdated RequestedInvoiceDate converted to local time, matching the CFDI's Fecha node");
        Assert.That(stampDateLocal, Is.EqualTo(new DateTime(2026, 7, 1, 15, 57, 0)),
            "stamp_date must equal the PAC stamp time converted to local time");
    }

    [Test]
    public void Resolve_NotBackdated_IssueDateFallsBackToStampDate()
    {
        var stampUtc = new DateTime(2026, 7, 2, 23, 28, 0, DateTimeKind.Utc);

        var (issueDateLocal, stampDateLocal) = CfdiPdfDateResolver.Resolve(
            requestedInvoiceDateUtc: null, stampUtc, MexicoCityTz);

        Assert.That(issueDateLocal, Is.EqualTo(stampDateLocal),
            "When the invoice was not backdated, issue_date must equal the (converted) stamp date");
        Assert.That(issueDateLocal, Is.EqualTo(new DateTime(2026, 7, 2, 17, 28, 0)));
    }

    [Test]
    public void Resolve_NoStampDateYet_BothDatesAreNull()
    {
        var (issueDateLocal, stampDateLocal) = CfdiPdfDateResolver.Resolve(
            requestedInvoiceDateUtc: null, stampDateUtc: null, MexicoCityTz);

        Assert.That(issueDateLocal, Is.Null);
        Assert.That(stampDateLocal, Is.Null);
    }

    [Test]
    public void Resolve_RequestedDateWithoutStampDate_IssueDateStillResolves()
    {
        // Draft state: backdated date chosen but stamping hasn't happened yet.
        var requestedUtc = new DateTime(2026, 6, 30, 19, 30, 0, DateTimeKind.Utc);

        var (issueDateLocal, stampDateLocal) = CfdiPdfDateResolver.Resolve(
            requestedUtc, stampDateUtc: null, MexicoCityTz);

        Assert.That(issueDateLocal, Is.EqualTo(new DateTime(2026, 6, 30, 13, 30, 0)));
        Assert.That(stampDateLocal, Is.Null);
    }

    [Test]
    public void Resolve_DifferentTimeZone_ConvertsCorrectly()
    {
        // Hermosillo does not observe DST and is fixed at UTC-7.
        var hermosilloTz = TimeZoneInfo.FindSystemTimeZoneById("America/Hermosillo");
        var stampUtc = new DateTime(2026, 7, 1, 20, 0, 0, DateTimeKind.Utc);

        var (_, stampDateLocal) = CfdiPdfDateResolver.Resolve(null, stampUtc, hermosilloTz);

        Assert.That(stampDateLocal, Is.EqualTo(new DateTime(2026, 7, 1, 13, 0, 0)));
    }
}
