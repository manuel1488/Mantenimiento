using CsvHelper.Configuration;
using App.Core.DTOs.Billing;

namespace App.Services.Mappings;

public sealed class MexicoSatUnitCsvMap : ClassMap<CreateMexicoSatUnitDto>
{
    public MexicoSatUnitCsvMap()
    {
        Map(m => m.Code).Name("Code");
        Map(m => m.Name).Name("Name");
        Map(m => m.Symbol).Name("Symbol").Optional();
    }
}
