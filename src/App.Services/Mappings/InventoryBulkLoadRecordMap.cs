using CsvHelper.Configuration;

using App.Core.DTOs.Inventory;
using App.Core.Interfaces;
using App.Core.Utils.CsvHelpers.Converters;

namespace App.Services.Mappings;

public sealed class InventoryBulkLoadRecordMap : ClassMap<InventoryBulkLoadRecord>
{
    public InventoryBulkLoadRecordMap(IInventoryColumnMappingService mappingService)
    {
        var reverseMapping = mappingService.GetReverseMapping();
        
        // Map each property based on the translated header names
        foreach (var mapping in reverseMapping)
        {
            var translatedHeader = mapping.Key;   // example: "Product Code"
            var propertyName = mapping.Value;     // example: "ProductCode"

            switch (propertyName)
            {
                case "ProductCode":
                    Map(m => m.ProductCode).Name(translatedHeader);
                    break;
                case "Quantity":
                    Map(m => m.Quantity).Name(translatedHeader)
                        .TypeConverter<DecimalConverter>();
                    break;
                case "MinStock":
                    Map(m => m.MinStock).Name(translatedHeader)
                        .Optional()
                        .TypeConverter<NullableDecimalConverter>();
                    break;
                case "MaxStock":
                    Map(m => m.MaxStock).Name(translatedHeader)
                        .Optional()
                        .TypeConverter<NullableDecimalConverter>();
                    break;
            }
        }
    }
}