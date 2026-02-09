using App.Core.DTOs.Settings;

namespace App.Core.Interfaces;

public interface ILookupService
{
    /// <summary>
    /// Gets all active countries
    /// </summary>
    Task<IList<CountryDto>> GetCountriesAsync();

    /// <summary>
    /// Gets all active currencies
    /// </summary>
    Task<IList<CurrencyDto>> GetCurrenciesAsync();

    /// <summary>
    /// Gets the currency for a country
    /// </summary>
    Task<CurrencyDto?> GetCountryCurrencyAsync(string countryCode);
}