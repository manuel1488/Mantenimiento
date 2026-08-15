using App.Core.DTOs.Fiscal;

namespace App.Core.Interfaces;

public interface IFiscalCatalogService
{
    Task<IList<RegimenFiscalCatalogoDto>> GetRegimenesFiscalesAsync();
    Task<IList<UsoCfdiCatalogoDto>> GetUsosCfdiAsync();
    Task<IList<UsoCfdiCatalogoDto>> GetUsosCfdiPorRegimenAsync(string codigoRegimenFiscal);
    Task<(int TotalCount, IList<ClaveUnidadSatCatalogoDto> Items)> SearchClavesUnidadSatAsync(
        string? searchText = null,
        int page = 1,
        int pageSize = 50);
}
