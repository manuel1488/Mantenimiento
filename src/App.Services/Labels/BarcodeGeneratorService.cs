using App.Core.Utils;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

using ZXing;
using ZXing.Common;

namespace App.Services.Labels;

/// <summary>
/// Generates Code 128 barcodes for bulk/variable-measure product labels.
///
/// Internal format: {ProductId}-{Quantity×1000:D6}-{TotalPrice×100}
/// Example: 156-020000-72414 → product id 156, 20.0 units, $724.14
///
/// Decoding:
///   parts[0] = ProductId (long) — always numeric, safe for all printer encodings
///   parts[1] / 1000 = Quantity  (e.g. 020000 → 20.000)
///   parts[2] / 100  = TotalPrice (e.g. 72414  → $724.14)
/// </summary>
public class BarcodeGeneratorService
{
    /// <summary>
    /// Generates a Code 128 barcode PNG encoded as Base64.
    /// </summary>
    public string GenerateBarcodeBase64(
        long productId,
        decimal quantity,
        decimal totalPrice,
        int widthPx = 500,
        int heightPx = 80)
    {
        var qtyMillis = ((long)Math.Round(quantity * 1000)).ToString("D6");
        var priceCents = ((long)Math.Round(totalPrice * 100)).ToString();
        var content = $"{productId}-{qtyMillis}-{priceCents}";

        var encodingOptions = new EncodingOptions
        {
            Width = widthPx,
            Height = heightPx,
            Margin = 8,
            PureBarcode = false
        };

        var writer = new BarcodeWriterPixelData
        {
            Format = BarcodeFormat.CODE_128,
            Options = encodingOptions
        };

        var pixelData = writer.Write(content);
        return ConvertPixelDataToPngBase64(pixelData.Pixels!, pixelData.Width, pixelData.Height);
    }

    /// <summary>
    /// Parses a bulk product barcode content string. Delegates to BarcodeParser (single source of truth).
    /// </summary>
    public static bool TryParseBarcodeContent(
        string content,
        out long productId,
        out decimal quantity,
        out decimal totalPrice) =>
        BarcodeParser.TryParseBulkBarcode(content, out productId, out quantity, out totalPrice);

    private static string ConvertPixelDataToPngBase64(byte[] pixels, int width, int height)
    {
        // ZXing.Net 0.16.x returns pixels as byte[] in BGRA format
        using var image = Image.LoadPixelData<Bgra32>(pixels, width, height);
        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return Convert.ToBase64String(ms.ToArray());
    }
}
