namespace App.Core.DTOs.Product;

/// <summary>
/// Defines the product template column configuration
/// </summary>
public class ProductColumnConfig
{
    public string PropertyName { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public Type DataType { get; set; } = typeof(string);
    public Func<object, bool>? Validator { get; set; }
    public object? DefaultValue { get; set; }
    public string? ExampleValue { get; set; }

    // Lazy loading for localized values to avoid issues during initialization
    public Func<string> GetLocalizedName { get; set; } = () => string.Empty;
    public Func<string>? GetValidationError { get; set; }
}