using App.Core.DTOs.Billing;
using App.Core.DTOs.Billing.Mexico;

namespace App.Core.Interfaces;

public interface IMexicoFiscalCatalogService
{
    // Fiscal Regimes
    Task<IList<MexicoFiscalRegimeDto>> GetFiscalRegimesAsync();
    Task<MexicoFiscalRegimeDto?> GetFiscalRegimeByIdAsync(int id);
    Task<MexicoFiscalRegimeDto?> GetFiscalRegimeByCodeAsync(string code);
    Task<MexicoFiscalRegimeDto> CreateFiscalRegimeAsync(CreateMexicoFiscalRegimeDto createDto);
    Task<MexicoFiscalRegimeDto> UpdateFiscalRegimeAsync(int id, UpdateMexicoFiscalCatalogDto updateDto);

    // Payment Forms
    Task<IList<MexicoPaymentFormDto>> GetPaymentFormsAsync();
    Task<MexicoPaymentFormDto?> GetPaymentFormByIdAsync(int id);
    Task<MexicoPaymentFormDto?> GetPaymentFormByCodeAsync(string code);
    Task<MexicoPaymentFormDto> CreatePaymentFormAsync(CreateMexicoPaymentFormDto createDto);
    Task<MexicoPaymentFormDto> UpdatePaymentFormAsync(int id, UpdateMexicoFiscalCatalogDto updateDto);

    // Payment Methods
    Task<IList<MexicoPaymentMethodDto>> GetPaymentMethodsAsync();
    Task<MexicoPaymentMethodDto?> GetPaymentMethodByIdAsync(int id);
    Task<MexicoPaymentMethodDto?> GetPaymentMethodByCodeAsync(string code);
    Task<MexicoPaymentMethodDto> CreatePaymentMethodAsync(CreateMexicoPaymentMethodDto createDto);
    Task<MexicoPaymentMethodDto> UpdatePaymentMethodAsync(int id, UpdateMexicoFiscalCatalogDto updateDto);

    // CFDI Uses
    Task<IList<MexicoCfdiUseDto>> GetCfdiUsesAsync();
    Task<MexicoCfdiUseDto?> GetCfdiUseByIdAsync(int id);
    Task<MexicoCfdiUseDto?> GetCfdiUseByCodeAsync(string code);
    Task<MexicoCfdiUseDto> CreateCfdiUseAsync(CreateMexicoCfdiUseDto createDto);
    Task<MexicoCfdiUseDto> UpdateCfdiUseAsync(int id, UpdateMexicoFiscalCatalogDto updateDto);

    // Product Services
    Task<IList<MexicoProductServiceDto>> GetProductServicesAsync();
    Task<MexicoProductServiceDto?> GetProductServiceByIdAsync(int id);
    Task<MexicoProductServiceDto?> GetProductServiceByCodeAsync(string code);
    Task<MexicoProductServiceDto> CreateProductServiceAsync(CreateMexicoProductServiceDto createDto);
    Task<MexicoProductServiceDto> UpdateProductServiceAsync(int id, UpdateMexicoFiscalCatalogDto updateDto);
    Task<(int TotalCount, IList<MexicoProductServiceDto> Items)> SearchProductServicesAsync(
        string? searchText = null,
        int page = 1,
        int pageSize = 50);

    // Validation Methods
    Task<bool> ValidateUniqueCodeAsync<T>(string code, int? excludeId = null) where T : class;
}