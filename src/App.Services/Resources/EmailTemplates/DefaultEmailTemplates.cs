using System.Reflection;

namespace App.Services.Resources.EmailTemplates;

/// <summary>
/// Provides lazy-loaded access to the default email template HTML and CSS embedded resources.
/// </summary>
public static class DefaultEmailTemplates
{
    private static readonly Assembly Assembly = typeof(DefaultEmailTemplates).Assembly;
    private const string ResourcePrefix = "App.Services.Resources.EmailTemplates";

    private static string? _classicHtml;
    private static string? _classicCss;
    private static string? _compactHtml;
    private static string? _compactCss;
    private static string? _modernHtml;
    private static string? _modernCss;

    public static string ClassicHtml => _classicHtml ??= Load("Classic.template.html");
    public static string ClassicCss  => _classicCss  ??= Load("Classic.styles.css");
    public static string CompactHtml => _compactHtml ??= Load("Compact.template.html");
    public static string CompactCss  => _compactCss  ??= Load("Compact.styles.css");
    public static string ModernHtml  => _modernHtml  ??= Load("Modern.template.html");
    public static string ModernCss   => _modernCss   ??= Load("Modern.styles.css");

    private static string Load(string fileName)
    {
        var resourceName = $"{ResourcePrefix}.{fileName}";
        using var stream = Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
