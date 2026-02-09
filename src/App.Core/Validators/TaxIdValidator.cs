using System.Text.RegularExpressions;
using App.Core.Common;
using Microsoft.Extensions.Localization;

namespace App.Core.Validators;

/// <summary>
/// Validates tax identification numbers for different countries.
/// </summary>
public class TaxIdValidator
{
    private readonly IStringLocalizer<TaxIdValidator> _localizer;

    public TaxIdValidator(IStringLocalizer<TaxIdValidator> localizer)
    {
        _localizer = localizer;
    }

    public record TaxIdFormat(string Pattern, int MaxLength);

    private static readonly Dictionary<string, TaxIdFormat> TaxIdFormats = new()
    {
        ["MX"] = new TaxIdFormat(
            Pattern: @"^[A-Z&Ñ]{3,4}[0-9]{6}[A-Z0-9]{3}$",
            MaxLength: 13
        ),
        ["CA"] = new TaxIdFormat(
            Pattern: @"^[0-9]{9}$",
            MaxLength: 9
        )
    };

    public TaxIdFormat? GetFormatForCountry(string countryCode)
    {
        return TaxIdFormats.GetValueOrDefault(countryCode.ToUpper());
    }

    public Result ValidateTaxId(string countryCode, string taxId)
    {
        return countryCode.ToUpper() switch
        {
            "MX" => ValidateMexicanRFC(taxId),
            "CA" => ValidateCanadianBN(taxId),
            _ => Result.Failure(_localizer["Country_Unsupported"])
        };
    }

    public Result ValidateMexicanRFC(string rfc)
    {
        if (string.IsNullOrEmpty(rfc))
            return Result.Failure(_localizer["TaxId_Empty"]);

        // Convert to uppercase and trim
        rfc = rfc.ToUpper().Trim();

        // Validate min length
        if (rfc.Length < 12)
            return Result.Failure(_localizer["RFC_TooShort"]);

        if(rfc.Length > 13)
            return Result.Failure(_localizer["RFC_TooLong"]);

        // Validate length and determine person type
        bool isMoralPerson = rfc.Length == 12;
        bool isPhysicalPerson = rfc.Length == 13;

        if (!isMoralPerson && !isPhysicalPerson)
            return Result.Failure(_localizer["RFC_InvalidLength"]);

        // Validate initial letters based on person type
        var initialLetters = isMoralPerson ? rfc[..3] : rfc[..4];
        if (!Regex.IsMatch(initialLetters, @"^[A-Z&Ñ]{3,4}$"))
            return Result.Failure(_localizer["RFC_InvalidInitials", isMoralPerson ? "3" : "4"]);

        // Validate numeric part (6 digits)
        var startPos = isMoralPerson ? 3 : 4;
        var numericPart = rfc.Substring(startPos, 6);
        if (!Regex.IsMatch(numericPart, @"^[0-9]{6}$"))
            return Result.Failure(_localizer["RFC_InvalidNumericPart"]);

        // Validate homoclave (last 3 characters)
        var homoclavePart = rfc[^3..];
        if (!Regex.IsMatch(homoclavePart, @"^[A-Z0-9]{3}$"))
            return Result.Failure(_localizer["RFC_InvalidHomoclave"]);

        return Result.Success();
    }

    public Result ValidateCanadianBN(string bn)
    {
        if (string.IsNullOrEmpty(bn))
            return Result.Failure(_localizer["BN_Empty"]);

        var format = TaxIdFormats["CA"];
        if (!Regex.IsMatch(bn, format.Pattern))
            return Result.Failure(_localizer["BN_InvalidFormat"]);

        return Result.Success();
    }
}