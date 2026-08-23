using App.Core.Common;

using NUnit.Framework;

namespace App.Services.Tests.Cotizaciones;

[TestFixture]
public class ContactLinkFormatterTests
{
    // ── NormalizeUrl ──────────────────────────────────────────────────────────

    [TestCase("sitio.com", "https://sitio.com")]
    [TestCase("www.sitio.com", "https://www.sitio.com")]
    [TestCase("facebook.com/empresa", "https://facebook.com/empresa")]
    [TestCase("http://sitio.com", "http://sitio.com")]
    [TestCase("https://sitio.com", "https://sitio.com")]
    [TestCase("HTTPS://sitio.com", "HTTPS://sitio.com")]
    public void NormalizeUrl_AddsSchemeOnlyWhenMissing(string input, string expected)
    {
        Assert.That(ContactLinkFormatter.NormalizeUrl(input), Is.EqualTo(expected));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void NormalizeUrl_BlankInput_ReturnsNull(string? input)
    {
        Assert.That(ContactLinkFormatter.NormalizeUrl(input), Is.Null);
    }

    // ── BuildTelHref ──────────────────────────────────────────────────────────

    [Test]
    public void BuildTelHref_StripsFormattingCharacters()
    {
        Assert.That(ContactLinkFormatter.BuildTelHref("555-000-1111"), Is.EqualTo("tel:5550001111"));
    }

    [Test]
    public void BuildTelHref_KeepsLeadingPlus()
    {
        Assert.That(ContactLinkFormatter.BuildTelHref("+52 555 000 1111"), Is.EqualTo("tel:+525550001111"));
    }

    [Test]
    public void BuildTelHref_BlankInput_ReturnsNull()
    {
        Assert.That(ContactLinkFormatter.BuildTelHref(null), Is.Null);
        Assert.That(ContactLinkFormatter.BuildTelHref("   "), Is.Null);
    }

    // ── BuildMailtoHref ───────────────────────────────────────────────────────

    [Test]
    public void BuildMailtoHref_PrependsMailtoScheme()
    {
        Assert.That(ContactLinkFormatter.BuildMailtoHref("contacto@empresa.com"), Is.EqualTo("mailto:contacto@empresa.com"));
    }

    [Test]
    public void BuildMailtoHref_BlankInput_ReturnsNull()
    {
        Assert.That(ContactLinkFormatter.BuildMailtoHref(null), Is.Null);
    }

    // ── BuildWhatsAppUrl ──────────────────────────────────────────────────────

    [Test]
    public void BuildWhatsAppUrl_StripsAllNonDigits_IncludingPlus()
    {
        Assert.That(ContactLinkFormatter.BuildWhatsAppUrl("+52 555 222 3333"), Is.EqualTo("https://wa.me/525552223333"));
    }

    [Test]
    public void BuildWhatsAppUrl_BlankInput_ReturnsNull()
    {
        Assert.That(ContactLinkFormatter.BuildWhatsAppUrl(null), Is.Null);
        Assert.That(ContactLinkFormatter.BuildWhatsAppUrl("   "), Is.Null);
    }
}
