namespace App.Services.Billing;

/// <summary>
/// Resolves the display dates ("FECHA DE EMISIÓN" / "FECHA DE CERTIFICACIÓN") shown on the
/// CFDI PDF representation, converted from UTC storage to the issuer's local timezone.
/// </summary>
public static class CfdiPdfDateResolver
{
    /// <summary>
    /// Computes the local "issue" and "stamp" dates for the CFDI PDF.
    /// </summary>
    /// <param name="requestedInvoiceDateUtc">
    /// <see cref="App.Models.Billing.MexicoInvoice.RequestedInvoiceDate"/> — set only when the invoice
    /// was antedatada (e.g. to the sale/entry date). This is the same value used to build the
    /// CFDI XML's <c>Fecha</c> node, so the PDF must show the same date, not the stamp date.
    /// </param>
    /// <param name="stampDateUtc">The actual PAC stamping timestamp (UTC).</param>
    /// <param name="issuerTimeZone">The issuer's timezone (postal-code based, falling back to company timezone).</param>
    public static (DateTime? IssueDateLocal, DateTime? StampDateLocal) Resolve(
        DateTime? requestedInvoiceDateUtc,
        DateTime? stampDateUtc,
        TimeZoneInfo issuerTimeZone)
    {
        var issueDateLocal = requestedInvoiceDateUtc.HasValue
            ? TimeZoneInfo.ConvertTimeFromUtc(requestedInvoiceDateUtc.Value, issuerTimeZone)
            : stampDateUtc.HasValue
                ? TimeZoneInfo.ConvertTimeFromUtc(stampDateUtc.Value, issuerTimeZone)
                : (DateTime?)null;

        var stampDateLocal = stampDateUtc.HasValue
            ? TimeZoneInfo.ConvertTimeFromUtc(stampDateUtc.Value, issuerTimeZone)
            : (DateTime?)null;

        return (issueDateLocal, stampDateLocal);
    }
}
