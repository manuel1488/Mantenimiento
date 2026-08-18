using AutoMapper;

using App.Core.DTOs.Fiscal;
using App.Models.Fiscal;

namespace App.Services.Mappings;

public class FiscalCatalogMappingProfile : Profile
{
    public FiscalCatalogMappingProfile()
    {
        CreateMap<RegimenFiscalCatalogo, RegimenFiscalCatalogoDto>();
        CreateMap<UsoCfdiCatalogo, UsoCfdiCatalogoDto>();
        CreateMap<ClaveUnidadSatCatalogo, ClaveUnidadSatCatalogoDto>();
        CreateMap<ClaveProdServSatCatalogo, ClaveProdServSatCatalogoDto>();
        CreateMap<TipoProdServSatCatalogo, TipoProdServSatCatalogoDto>();
        CreateMap<SegmentoProdServSatCatalogo, SegmentoProdServSatCatalogoDto>();
        CreateMap<FamiliaProdServSatCatalogo, FamiliaProdServSatCatalogoDto>();
    }
}
