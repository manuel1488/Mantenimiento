namespace App.Web.Services;

using System.Globalization;

using App.Core.Options;

using Microsoft.Extensions.Options;

using MudBlazor;

public class CurrentThemeService
{
    private readonly MudTheme _theme;

    public CurrentThemeService(IOptions<BrandingOptions> brandingOptions)
    {
        var branding = brandingOptions.Value;
        var primary = branding.PrimaryColor;
        var secondary = branding.SecondaryColor;

        _theme = new MudTheme
        {
            PaletteLight = new PaletteLight
            {
                Primary = primary,
                PrimaryDarken = Darken(primary),
                PrimaryLighten = Lighten(primary),
                Secondary = secondary,
                SecondaryDarken = Darken(secondary),
                SecondaryLighten = Lighten(secondary),
                Background = "#F5F9F9",
                Surface = "#FFFFFF",
                AppbarBackground = primary,
                AppbarText = "#FFFFFF",
                DrawerBackground = "#FFFFFF",
                DrawerText = primary,
                Success = "#4CAF50",
                Warning = "#FF9800",
                Error = "#D32F2F",
                Info = Lighten(primary)
            },
            PaletteDark = new PaletteDark
            {
                Primary = Lighten(primary),
                PrimaryDarken = primary,
                PrimaryLighten = Lighten(primary, 0.55),
                Secondary = Lighten(secondary),
                Background = "#0F1E1E",
                Surface = "#162828",
                AppbarBackground = "#0F1E1E",
                AppbarText = Lighten(primary),
                DrawerBackground = "#162828",
                DrawerText = Lighten(primary),
                Success = "#4CAF50",
                Warning = "#FF9800",
                Error = "#EF5350",
                Info = Lighten(primary)
            },
            Typography = new Typography
            {
                Default = new DefaultTypography
                {
                    FontFamily = new[] { "Roboto", "sans-serif" },
                    FontSize = "14px",
                    FontWeight = "400",
                    LineHeight = "1.43",
                    LetterSpacing = ".01071em"
                },
                H1 = new H1Typography
                {
                    FontFamily = new[] { "Poppins", "Roboto", "sans-serif" },
                    FontSize = "24px",
                    FontWeight = "700",
                    LineHeight = "1.167",
                    LetterSpacing = "-.01562em"
                },
                H2 = new H2Typography
                {
                    FontFamily = new[] { "Poppins", "Roboto", "sans-serif" },
                    FontSize = "20px",
                    FontWeight = "600",
                    LineHeight = "1.2",
                    LetterSpacing = "-.00833em"
                },
                H3 = new H3Typography
                {
                    FontFamily = new[] { "Poppins", "Roboto", "sans-serif" },
                    FontSize = "18px",
                    FontWeight = "600",
                    LineHeight = "1.25"
                },
                Body1 = new Body1Typography
                {
                    FontSize = "14px",
                    FontWeight = "400",
                    LineHeight = "1.5",
                    LetterSpacing = ".00938em"
                },
                Caption = new CaptionTypography
                {
                    FontSize = "12px",
                    FontWeight = "400",
                    LineHeight = "1.66",
                    LetterSpacing = ".03333em"
                }
            }
        };
    }

    public MudTheme Theme => _theme;

    // Blends the color toward black so any configured brand color still yields a usable "darken" variant.
    private static string Darken(string hex, double amount = 0.2) => Blend(hex, 0, 0, 0, amount);

    // Blends the color toward white so any configured brand color still yields a usable "lighten" variant.
    private static string Lighten(string hex, double amount = 0.4) => Blend(hex, 255, 255, 255, amount);

    private static string Blend(string hex, int targetR, int targetG, int targetB, double amount)
    {
        var (r, g, b) = ParseHex(hex);
        var blendedR = (int)Math.Round(r + (targetR - r) * amount);
        var blendedG = (int)Math.Round(g + (targetG - g) * amount);
        var blendedB = (int)Math.Round(b + (targetB - b) * amount);
        return $"#{blendedR:X2}{blendedG:X2}{blendedB:X2}";
    }

    private static (int R, int G, int B) ParseHex(string hex)
    {
        var value = hex.TrimStart('#');
        var r = int.Parse(value[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        var g = int.Parse(value[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        var b = int.Parse(value[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return (r, g, b);
    }
}
