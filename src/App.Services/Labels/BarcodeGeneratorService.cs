using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

using ZXing;
using ZXing.Common;

namespace App.Services.Labels;

/// <summary>
/// Generates Code 128 barcodes for bulk/variable-measure product labels.
///
/// Internal format: {ProductCode}|{Quantity×1000:D6}|{TotalPrice×100}
/// Example: P0034|012500|22500 → product P0034, 12.5 units, $225.00
///
/// Decoding:
///   parts[0] = ProductCode
///   parts[1] / 1000 = Quantity  (e.g. 012500 → 12.500)
///   parts[2] / 100  = TotalPrice (e.g. 22500  → $225.00)
/// </summary>
public class BarcodeGeneratorService
{
    /// <summary>
    /// Generates a Code 128 barcode PNG encoded as Base64.
    /// </summary>
    public string GenerateBarcodeBase64(
        string productCode,
        decimal quantity,
        decimal totalPrice,
        int widthPx = 500,
        int heightPx = 80)
    {
        var qtyMillis = ((long)Math.Round(quantity * 1000)).ToString("D6");
        var priceCents = ((long)Math.Round(totalPrice * 100)).ToString();
        var content = $"{productCode}|{qtyMillis}|{priceCents}";

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
    /// Parses a bulk product barcode content string.
    /// Returns true if the format is valid; false otherwise.
    /// </summary>
    public static bool TryParseBarcodeContent(
        string content,
        out string productCode,
        out decimal quantity,
        out decimal totalPrice)
    {
        productCode = string.Empty;
        quantity = 0;
        totalPrice = 0;

        if (string.IsNullOrWhiteSpace(content)) return false;

        var parts = content.Split('|');
        if (parts.Length != 3) return false;
        if (!long.TryParse(parts[1], out var qtyMillis)) return false;
        if (!long.TryParse(parts[2], out var priceCents)) return false;

        productCode = parts[0].Trim();
        quantity = qtyMillis / 1000m;
        totalPrice = priceCents / 100m;
        return true;
    }

    private static string ConvertPixelDataToPngBase64(byte[] pixels, int width, int height)
    {
        // ZXing.Net 0.16.x returns pixels as byte[] in BGRA format
        using var image = Image.LoadPixelData<Bgra32>(pixels, width, height);
        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return Convert.ToBase64String(ms.ToArray());
    }
}
