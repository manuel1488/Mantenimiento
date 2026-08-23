using System.Text.RegularExpressions;

namespace App.Core.Common;

/// <summary>
/// Builds clickable hrefs from free-text contact fields (SitioWeb, Telefono, CorreoElectronico,
/// WhatsApp, Facebook, Instagram) so the Cotización PDF footer can link to them directly.
/// </summary>
public static class ContactLinkFormatter
{
    /// <summary>Prepends "https://" when the value has no scheme yet (e.g. "sitio.com" or "facebook.com/empresa").</summary>
    public static string? NormalizeUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var trimmed = value.Trim();
        return trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? trimmed
            : $"https://{trimmed}";
    }

    public static string? BuildTelHref(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return null;

        var digits = Regex.Replace(phone, @"[^\d+]", "");
        return string.IsNullOrEmpty(digits) ? null : $"tel:{digits}";
    }

    public static string? BuildMailtoHref(string? email) =>
        string.IsNullOrWhiteSpace(email) ? null : $"mailto:{email.Trim()}";

    /// <summary>Builds a https://wa.me/{digits} link — WhatsApp ignores non-digit characters anyway.</summary>
    public static string? BuildWhatsAppUrl(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return null;

        var digits = Regex.Replace(phone, @"\D", "");
        return string.IsNullOrEmpty(digits) ? null : $"https://wa.me/{digits}";
    }
}
