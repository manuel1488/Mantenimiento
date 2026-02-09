using System.Globalization;

using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace App.Core.Utils.CsvHelpers.Converters;

public class NullableDecimalConverter : DefaultTypeConverter
{
    public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        
        if (decimal.TryParse(text, 
            NumberStyles.Number | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture, 
            out decimal result))
        {
            return result;
        }
        
        throw new CsvHelperException(row.Context, 
            "Invalid decimal value");
    }
}