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

public sealed class TipoProdServSatCatalogoCsvMap : ClassMap<CreateTipoProdServSatCatalogoDto>
{
    public TipoProdServSatCatalogoCsvMap()
    {
        Map(m => m.Codigo).Name("code");
        Map(m => m.Descripcion).Name("description");
    }
}

public sealed class SegmentoProdServSatCatalogoCsvMap : ClassMap<CreateSegmentoProdServSatCatalogoDto>
{
    public SegmentoProdServSatCatalogoCsvMap()
    {
        Map(m => m.Codigo).Name("code");
        Map(m => m.Descripcion).Name("description");
        Map(m => m.TipoCodigo).Name("tipo_code");
    }
}

public sealed class FamiliaProdServSatCatalogoCsvMap : ClassMap<CreateFamiliaProdServSatCatalogoDto>
{
    public FamiliaProdServSatCatalogoCsvMap()
    {
        Map(m => m.Codigo).Name("code");
        Map(m => m.Descripcion).Name("description");
        Map(m => m.SegmentoCodigo).Name("segmento_code");
    }
}

public sealed class ClaveProdServSatCatalogoCsvMap : ClassMap<CreateClaveProdServSatCatalogoDto>
{
    public ClaveProdServSatCatalogoCsvMap()
    {
        Map(m => m.Codigo).Name("code");
        Map(m => m.Descripcion).Name("description");
    }
}
