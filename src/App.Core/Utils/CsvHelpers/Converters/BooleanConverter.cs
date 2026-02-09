using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace App.Core.Utils.CsvHelpers.Converters;

public class BooleanConverter : DefaultTypeConverter
{
    public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        text = text.Trim().ToLowerInvariant();

        return text switch
        {
            "true" or "1" or "yes" or "y" or "si" or "s" => true,
            "false" or "0" or "no" or "n" => false,
            _ => throw new TypeConverterException(this, memberMapData, text, row.Context,
                $"Invalid boolean value: '{text}'. Expected: true/false, 1/0, yes/no, y/n, si/s")
        };
    }

    public override string ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
    {
        return value is bool boolValue ? (boolValue ? "true" : "false") : "false";
    }
}