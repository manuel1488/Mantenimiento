using App.Core.DTOs.Billing;

namespace App.Core.Interfaces;

/// <summary>
/// Provides methods to read fiscal catalog data from a data source
/// </summary>
public interface IFiscalCatalogDataReader
{
    Task<IEnumerable<CreateMexicoFiscalRegimeDto>> GetFiscalRegimesAsync();
    Task<IEnumerable<CreateMexicoPaymentFormDto>> GetPaymentFormsAsync();
    Task<IEnumerable<CreateMexicoPaymentMethodDto>> GetPaymentMethodsAsync();
    Task<IEnumerable<CreateMexicoCfdiUseDto>> GetCfdiUsesAsync();
    Task<IEnumerable<CreateMexicoProductServiceDto>> GetProductServicesAsync();
    Task<IEnumerable<CreateMexicoSatUnitDto>> GetSatUnitsAsync();
}