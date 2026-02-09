using System.Globalization;

namespace App.Core.Options;

/// <summary>
/// Provides access to supported cultures based on application settings
/// </summary>
public class CultureOptions
{
    private readonly ApplicationOptions _options;
    private readonly Lazy<CultureInfo[]> _supportedCultures;

    public CultureOptions(ApplicationOptions options)
    {
        _options = options;
        _supportedCultures = new Lazy<CultureInfo[]>(() =>
            _options.SupportedLanguages.Select(x => new CultureInfo(x)).ToArray());
    }

    /// <summary>
    /// Gets the array of supported cultures
    /// </summary>
    public CultureInfo[] SupportedCultures => _supportedCultures.Value;

    /// <summary>
    /// Gets the default culture
    /// </summary>
    public CultureInfo DefaultCulture => new(_options.DefaultLanguage);
}