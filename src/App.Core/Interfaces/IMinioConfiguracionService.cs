using App.Core.DTOs.Settings;

namespace App.Core.Interfaces;

public interface IMinioConfiguracionService
{
    /// <summary>
    /// Gets the current MinIO configuration, or null if it hasn't been configured yet.
    /// </summary>
    Task<MinioConfiguracionDto?> GetConfigAsync();

    /// <summary>
    /// Creates or updates the singleton MinIO configuration row.
    /// </summary>
    Task<MinioConfiguracionDto> UpdateConfigAsync(UpdateMinioConfiguracionDto updateDto);
}
