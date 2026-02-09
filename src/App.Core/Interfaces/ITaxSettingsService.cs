using App.Core.DTOs.Settings;

namespace App.Core.Interfaces;

public interface ITaxSettingsService
{
    /// <summary>
    /// Retrieves the current tax configuration settings from the database.
    /// Returns only the most recent settings record, ordered by Id.
    /// </summary>
    /// <returns>
    /// A <see cref="TaxSettingsDto"/> containing the mapped tax settings,
    /// or null if no settings exist in the database.
    /// </returns>
    /// <exception cref="Exception">
    /// Propagates any database or mapping exceptions that occur during retrieval.
    /// These exceptions are logged before being rethrown.
    /// </exception>
    Task<TaxSettingsDto?> GetSettingsAsync();

    /// <summary>
    /// Updates the existing tax settings or creates new ones if none exist.
    /// Performs validation before applying any changes.
    /// </summary>
    /// <param name="updateDto">The DTO containing the new tax settings to be applied.</param>
    /// <returns>
    /// A <see cref="TaxSettingsDto"/> containing the mapped updated or newly created settings.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the provided settings fail validation.
    /// The exception message contains all validation errors joined by newlines.
    /// </exception>
    /// <exception cref="Exception">
    /// Propagates any database or mapping exceptions that occur during the update.
    /// These exceptions are logged before being rethrown.
    /// </exception>
    Task<TaxSettingsDto> UpdateSettingsAsync(UpdateTaxSettingsDto updateDto);

    /// <summary>
    /// Validates tax settings against country-specific rules and requirements.
    /// Performs multiple validations including:
    /// - Verifies the country exists and is active
    /// - Validates the tax ID format for the specified country
    /// - Checks required fields based on country-specific requirements
    /// </summary>
    /// <param name="countryCode">The country code to validate against (e.g., "MX" for Mexico, "CA" for Canada)</param>
    /// <param name="settings">The tax settings to validate</param>
    /// <returns>
    /// A list of localized error messages. An empty list indicates the settings are valid.
    /// For Mexico (MX), validates CFDI use, payment method, and payment type.
    /// For Canada (CA), validates GST number.
    /// </returns>
    Task<IList<string>> ValidateSettingsAsync(string countryCode, UpdateTaxSettingsDto settings);
}