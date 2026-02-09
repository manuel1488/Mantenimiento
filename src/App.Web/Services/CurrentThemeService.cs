namespace App.Web.Services;

using MudBlazor;

public class CurrentThemeService
{
    private readonly MudTheme _theme = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#E53935",
            Secondary = "#757575",
            Background = "#F5F5F5",
            Surface = "#FFFFFF",
            AppbarBackground = "#FFFFFF",
            DrawerBackground = "#FFFFFF",
            Success = "#4CAF50",
            Warning = "#FF9800",
            Error = "#E53935",
            Info = "#2196F3"
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#E53935",
            Secondary = "#757575",
            Background = "#121212",
            Surface = "#1E1E1E",
            AppbarBackground = "#1E1E1E",
            DrawerBackground = "#1E1E1E",
            Success = "#4CAF50",
            Warning = "#FF9800",
            Error = "#E53935",
            Info = "#2196F3"
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
                FontSize = "24px",
                FontWeight = "400",
                LineHeight = "1.167",
                LetterSpacing = "-.01562em"
            },
            H2 = new H2Typography
            {
                FontSize = "20px",
                FontWeight = "300",
                LineHeight = "1.2",
                LetterSpacing = "-.00833em"
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