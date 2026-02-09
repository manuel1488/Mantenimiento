using CsvHelper.Configuration;

using App.Core.DTOs.Billing;

namespace App.Services.Mappings;

public sealed class MexicoFiscalRegimeCsvMap : ClassMap<CreateMexicoFiscalRegimeDto>
{
    public MexicoFiscalRegimeCsvMap()
    {
        Map(m => m.Code).Name("code");
        Map(m => m.Description).Name("description");
    }
}