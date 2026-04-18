using System.Reflection;

namespace App.Services.Resources.PdfTemplates;

/// <summary>
/// Provides lazy-loaded access to the default quotation PDF CSS embedded resource.
/// Pattern mirrors <see cref="App.Services.Resources.EmailTemplates.DefaultEmailTemplates"/>.
/// </summary>
public static class DefaultQuotationTemplate
{
    private static readonly Assembly Assembly = typeof(DefaultQuotationTemplate).Assembly;
    private const string ResourcePrefix = "App.Services.Resources.PdfTemplates.Quotation";

    private static string? _html;
    private static string? _css;

    /// <summary>Default HTML body template (Scriban) for the quotation PDF document.</summary>
    public static string Html => _html ??= Load("template.html");

    /// <summary>Default CSS styles for the quotation PDF document.</summary>
    public static string Css => _css ??= Load("styles.css");

    private static string Load(string fileName)
    {
        var resourceName = $"{ResourcePrefix}.{fileName}";
        using var stream = Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
