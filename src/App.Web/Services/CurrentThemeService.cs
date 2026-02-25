namespace App.Web.Services;

using MudBlazor;

public class CurrentThemeService
{
    private readonly MudTheme _theme = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#1A6868",
            PrimaryDarken = "#155454",
            PrimaryLighten = "#7DDCD6",
            Secondary = "#7B3FA0",
            SecondaryDarken = "#5C2E78",
            SecondaryLighten = "#B07ACC",
            Background = "#F5F9F9",
            Surface = "#FFFFFF",
            AppbarBackground = "#1A6868",
            AppbarText = "#FFFFFF",
            DrawerBackground = "#FFFFFF",
            DrawerText = "#1A6868",
            Success = "#4CAF50",
            Warning = "#FF9800",
            Error = "#D32F2F",
            Info = "#7DDCD6"
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#7DDCD6",
            PrimaryDarken = "#1A6868",
            PrimaryLighten = "#A8EAE6",
            Secondary = "#B07ACC",
            Background = "#0F1E1E",
            Surface = "#162828",
            AppbarBackground = "#0F1E1E",
            AppbarText = "#7DDCD6",
            DrawerBackground = "#162828",
            DrawerText = "#7DDCD6",
            Success = "#4CAF50",
            Warning = "#FF9800",
            Error = "#EF5350",
            Info = "#7DDCD6"
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

    public MudTheme Theme => _theme;
}