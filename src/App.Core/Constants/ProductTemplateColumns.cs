using App.Core.DTOs.Product;

using Microsoft.Extensions.Localization;

namespace App.Core.Constants;

public static class ProductTemplateColumns
{
    /// <summary>
    /// Gets the product column configurations - Single Source of Truth
    /// </summary>
    public static List<ProductColumnConfig> GetProductColumnConfigurations(IStringLocalizer _localizer)
    {
        return new List<ProductColumnConfig>
        {
            new() { 
                PropertyName = "Code",
                GetLocalizedName = () => _localizer["Code"],
                IsRequired = false,
                DataType = typeof(string),
                ExampleValue = "" // Empty means auto-generate
            },
            new() { 
                PropertyName = "Name",
                GetLocalizedName = () => _localizer["Name"],
                IsRequired = true,
                DataType = typeof(string),
                ExampleValue = _localizer["Sample Product"]
            },
            new() { 
                PropertyName = "Brand",
                GetLocalizedName = () => _localizer["Brand"],
                IsRequired = true,
                DataType = typeof(string),
                ExampleValue = _localizer["Sample Brand"]
            },
            new() { 
                PropertyName = "Description",
                GetLocalizedName = () => _localizer["Description"],
                IsRequired = true,
                DataType = typeof(string),
                ExampleValue = _localizer["Product description"]
            },
            new() { 
                PropertyName = "Barcode",
                GetLocalizedName = () => _localizer["Barcode"],
                IsRequired = false,
                DataType = typeof(string),
                ExampleValue = "123456789012"
            },
            new() { 
                PropertyName = "Content",
                GetLocalizedName = () => _localizer["Content"],
                IsRequired = true,
                DataType = typeof(decimal),
                Validator = (value) => value is decimal d && d > 0,
                GetValidationError = () => _localizer["Content must be greater than 0"],
                DefaultValue = 1.0m,
                ExampleValue = "1.0"
            },
            new() { 
                PropertyName = "UnitMeasureCode",
                GetLocalizedName = () => _localizer["UnitMeasureCode"],
                IsRequired = true,
                DataType = typeof(string),
                ExampleValue = "PZA" // Will be replaced with actual unit code
            },
            new() { 
                PropertyName = "Price",
                GetLocalizedName = () => _localizer["Price"],
                IsRequired = true,
                DataType = typeof(decimal),
                Validator = (value) => value is decimal d && d > 0,
                GetValidationError = () => _localizer["Price must be greater than 0"],
                ExampleValue = "100.00"
            },
            new() { 
                PropertyName = "IsTaxable",
                GetLocalizedName = () => _localizer["IsTaxable"],
                IsRequired = true,
                DataType = typeof(bool),
                DefaultValue = false,
                ExampleValue = "true"
            },
            new() { 
                PropertyName = "IsActive",
                GetLocalizedName = () => _localizer["IsActive"],
                IsRequired = true,
                DataType = typeof(bool),
                DefaultValue = true,
                ExampleValue = "true"
            },
            new() { 
                PropertyName = "MexicoProductServiceCode",
                GetLocalizedName = () => _localizer["MexicoProductServiceCode"],
                IsRequired = false,
                DataType = typeof(string),
                ExampleValue = "01010101"
            },
            new() {
                PropertyName = "AllowPartialSale",
                GetLocalizedName = () => _localizer["AllowPartialSale"],
                IsRequired = true,
                DataType = typeof(bool),
                DefaultValue = false,
                ExampleValue = "false"
            },
            new() {
                PropertyName = "AllowCustomPricing",
                GetLocalizedName = () => _localizer["AllowCustomPricing"],
                IsRequired = true,
                DataType = typeof(bool),
                DefaultValue = false,
                ExampleValue = "false"
            }
        };
    }
}