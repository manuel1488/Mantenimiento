using App.Core.DTOs.Fiscal;

namespace App.Core.Interfaces;

/// <summary>
/// Reads the SAT fiscal catalogs (Régimen Fiscal, Uso de CFDI) from their data source.
/// </summary>
public interface IFiscalCatalogDataReader
{
    Task<IEnumerable<CreateRegimenFiscalCatalogoDto>> GetRegimenesFiscalesAsync();
    Task<IEnumerable<CreateUsoCfdiCatalogoDto>> GetUsosCfdiAsync();
}
