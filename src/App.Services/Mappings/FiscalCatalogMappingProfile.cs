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
    }
}
