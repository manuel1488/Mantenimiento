using System.Reflection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace App.Core.Extensions;

public static class StringLocalizationExtensions
{
    private static readonly Dictionary<string, IStringLocalizer> _localizers = new();
    private static readonly IStringLocalizerFactory _factory;

    static StringLocalizationExtensions()
    {
        var loggerFactory = NullLoggerFactory.Instance;

        _factory = new ResourceManagerStringLocalizerFactory(
            new OptionsWrapper<LocalizationOptions>(
                new LocalizationOptions { ResourcesPath = "Resources" }), 
            loggerFactory);
    }

    public static string GetLocalizedDescription(this string value, Type resourceType)
    {
        var key = resourceType.FullName ?? resourceType.Name;
        
        if (!_localizers.TryGetValue(key, out var localizer))
        {
            localizer = _factory.Create(resourceType);
            _localizers[key] = localizer;
        }

        return localizer[value] ?? value;
    }
}