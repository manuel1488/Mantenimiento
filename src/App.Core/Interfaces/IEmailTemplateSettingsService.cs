using App.Core.Common;
using App.Core.DTOs.Settings;

namespace App.Core.Interfaces;

public interface IEmailTemplateSettingsService
{
    Task<Result<EmailTemplateSettingsDto>> GetAsync(string name);
    Task<Result<EmailTemplateSettingsDto>> SaveAsync(SaveEmailTemplateSettingsDto dto);
    Task<Result> DeleteAsync(string name);
}
