using App.Core.DTOs.Fiscal;

namespace App.Core.Interfaces;

public interface IFiscalCatalogService
{
    Task<IList<RegimenFiscalCatalogoDto>> GetRegimenesFiscalesAsync();
    Task<IList<UsoCfdiCatalogoDto>> GetUsosCfdiAsync();
    Task<IList<UsoCfdiCatalogoDto>> GetUsosCfdiPorRegimenAsync(string codigoRegimenFiscal);
}
