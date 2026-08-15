using CsvHelper.Configuration;

using App.Core.DTOs.Fiscal;

namespace App.Services.Mappings;

public sealed class RegimenFiscalCatalogoCsvMap : ClassMap<CreateRegimenFiscalCatalogoDto>
{
    public RegimenFiscalCatalogoCsvMap()
    {
        Map(m => m.Codigo).Name("code");
        Map(m => m.Descripcion).Name("description");
    }
}

public sealed class UsoCfdiCatalogoCsvMap : ClassMap<CreateUsoCfdiCatalogoDto>
{
    public UsoCfdiCatalogoCsvMap()
    {
        Map(m => m.Codigo).Name("code");
        Map(m => m.Descripcion).Name("description");
        Map(m => m.CodigosRegimenFiscal).Name("fiscal_regime_codes").Optional();
    }
}

public sealed class ClaveUnidadSatCatalogoCsvMap : ClassMap<CreateClaveUnidadSatCatalogoDto>
{
    public ClaveUnidadSatCatalogoCsvMap()
    {
        Map(m => m.Codigo).Name("Code");
        Map(m => m.Nombre).Name("Name");
        Map(m => m.Simbolo).Name("Symbol").Optional();
    }
}
