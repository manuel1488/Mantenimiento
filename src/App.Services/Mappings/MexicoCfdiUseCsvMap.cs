using CsvHelper.Configuration;

using App.Core.DTOs.Billing;

namespace App.Services.Mappings;

public sealed class MexicoCfdiUseCsvMap : ClassMap<CreateMexicoCfdiUseDto>
{
    public MexicoCfdiUseCsvMap()
    {
        Map(m => m.Code).Name("code");
        Map(m => m.Description).Name("description");
    }
}