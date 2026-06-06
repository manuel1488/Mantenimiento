namespace App.Services.Billing;

internal static class CfdiPdfHelper
{
    /// <summary>
    /// Injects a diagonal "CANCELADA" watermark and cancellation banner into a rendered CFDI HTML
    /// when the template itself does not already include them (legacy/custom DB templates).
    /// </summary>
    internal static string InjectCancellationWatermark(string html, string cancellationDate)
    {
        if (html.Contains("watermark-overlay", StringComparison.OrdinalIgnoreCase) ||
            html.Contains("cfdi-cancel-overlay", StringComparison.OrdinalIgnoreCase))
            return html;

        const string css = """
            .cfdi-cancel-overlay {
                position: fixed;
                top: 0; left: 0;
                width: 100%; height: 100%;
                pointer-events: none;
                z-index: 9999;
                display: flex;
                align-items: center;
                justify-content: center;
            }
            .cfdi-cancel-text {
                font-size: 100px;
                font-weight: 900;
                color: rgba(192, 57, 43, 0.20);
                text-transform: uppercase;
                letter-spacing: 8px;
                transform: rotate(-40deg);
                white-space: nowrap;
                font-family: Arial Black, Arial, sans-serif;
                user-select: none;
                text-align: center;
                line-height: 1.2;
            }
            .cfdi-cancel-banner {
                margin: 0 12px 0 12px;
                padding: 8px 14px;
                background-color: #fdecea;
                border-left: 4px solid #c0392b;
                font-size: 12px;
                color: #c0392b;
                font-weight: bold;
            }
            """;

        var dateSpan = string.IsNullOrEmpty(cancellationDate)
            ? string.Empty
            : $"<br><span style=\"font-size:28px;letter-spacing:4px;\">{cancellationDate}</span>";

        var overlay = $"""
            <div class="cfdi-cancel-overlay">
                <div class="cfdi-cancel-text">CANCELADA{dateSpan}</div>
            </div>
            """;

        var banner = $"""
            <div class="cfdi-cancel-banner">
                &#x26A0; FACTURA CANCELADA ante el SAT{(string.IsNullOrEmpty(cancellationDate) ? "" : $" — Fecha: {cancellationDate}")}
            </div>
            """;

        if (html.Contains("</style>", StringComparison.OrdinalIgnoreCase))
            html = html.Replace("</style>", css + "\n</style>", StringComparison.OrdinalIgnoreCase);
        else
            html = html.Replace("</head>", $"<style>\n{css}\n</style>\n</head>", StringComparison.OrdinalIgnoreCase);

        html = System.Text.RegularExpressions.Regex.Replace(
            html, @"<body([^>]*)>",
            m => m.Value + "\n" + overlay,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        html = System.Text.RegularExpressions.Regex.Replace(
            html, @"(<div[^>]+class=""container""[^>]*>)",
            m => m.Value + "\n" + banner,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return html;
    }

    internal static string InjectPreInvoiceWatermark(string html)
    {
        if (html.Contains("cfdi-preinvoice-overlay", StringComparison.OrdinalIgnoreCase))
            return html;

        const string css = """
            .cfdi-preinvoice-overlay {
                position: fixed;
                top: 0; left: 0;
                width: 100%; height: 100%;
                pointer-events: none;
                z-index: 9999;
                display: flex;
                align-items: center;
                justify-content: center;
            }
            .cfdi-preinvoice-text {
                font-size: 72px;
                font-weight: 900;
                color: rgba(80, 80, 80, 0.15);
                text-transform: uppercase;
                letter-spacing: 6px;
                transform: rotate(-40deg);
                white-space: nowrap;
                font-family: Arial Black, Arial, sans-serif;
                user-select: none;
                text-align: center;
                line-height: 1.3;
            }
            """;

        const string overlay = """
            <div class="cfdi-preinvoice-overlay">
                <div class="cfdi-preinvoice-text">SIN VALIDEZ<br>FISCAL</div>
            </div>
            """;

        if (html.Contains("</style>", StringComparison.OrdinalIgnoreCase))
            html = html.Replace("</style>", css + "\n</style>", StringComparison.OrdinalIgnoreCase);
        else
            html = html.Replace("</head>", $"<style>\n{css}\n</style>\n</head>", StringComparison.OrdinalIgnoreCase);

        html = System.Text.RegularExpressions.Regex.Replace(
            html, @"<body([^>]*)>",
            m => m.Value + "\n" + overlay,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return html;
    }
}
