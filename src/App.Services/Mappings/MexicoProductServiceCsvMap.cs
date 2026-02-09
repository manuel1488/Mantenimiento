using CsvHelper.Configuration;

using App.Core.DTOs.Billing;

namespace App.Services.Mappings;

public sealed class MexicoProductServiceCsvMap : ClassMap<CreateMexicoProductServiceDto>
{
    public MexicoProductServiceCsvMap()
    {
        Map(m => m.Code).Name("code");
        Map(m => m.Description).Name("description");
        Map(m => m.EffectiveFrom).Name("effective_from")
            .Default(DateTime.UtcNow);
        Map(m => m.EffectiveTo).Name("effective_to")
            .Optional();
    }
}