using CsvHelper.Configuration;

using App.Core.DTOs.Billing;

namespace App.Services.Mappings;

public sealed class MexicoPaymentFormCsvMap : ClassMap<CreateMexicoPaymentFormDto>
{
    public MexicoPaymentFormCsvMap()
    {
        Map(m => m.Code).Name("code");
        Map(m => m.Description).Name("description");
    }
}