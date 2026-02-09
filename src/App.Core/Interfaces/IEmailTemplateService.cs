namespace App.Core.Interfaces;

/// <summary>
/// Service for managing email templates
/// </summary>
public interface IEmailTemplateService
{
    /// <summary>
    /// Gets an email template by name and replaces its placeholders with the provided data
    /// </summary>
    Task<string> GetTemplateAsync(string templateName, object data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available template names
    /// </summary>
    Task<IEnumerable<string>> GetAvailableTemplatesAsync(CancellationToken cancellationToken = default);
}