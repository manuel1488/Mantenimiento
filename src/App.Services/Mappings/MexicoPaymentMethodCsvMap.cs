using CsvHelper.Configuration;

using App.Core.DTOs.Billing;

namespace App.Services.Mappings;

public sealed class MexicoPaymentMethodCsvMap : ClassMap<CreateMexicoPaymentMethodDto>
{
    public MexicoPaymentMethodCsvMap()
    {
        Map(m => m.Code).Name("code");
        Map(m => m.Description).Name("description");
    }
}