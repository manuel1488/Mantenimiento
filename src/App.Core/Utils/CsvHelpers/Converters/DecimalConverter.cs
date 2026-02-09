using System.Globalization;

using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace App.Core.Utils.CsvHelpers.Converters;

public class DecimalConverter : DefaultTypeConverter
{
    public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0m;
        
        if (decimal.TryParse(text, 
            NumberStyles.Number | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture, 
            out decimal result))
        {
            return result;
        }
        
        throw new TypeConverterException(this, memberMapData, text, row.Context,
            $"Invalid decimal value: '{text}'. Expected format: 123.45");
    }
}