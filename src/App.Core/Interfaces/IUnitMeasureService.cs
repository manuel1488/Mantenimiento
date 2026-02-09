using App.Core.DTOs.UnitMeasure;

namespace App.Core.Interfaces;

public interface IUnitMeasureService
{
    Task<IList<UnitMeasureDto>> GetActiveUnitMeasuresAsync(string countryCode);
    
    Task<UnitMeasureDto?> GetUnitMeasureByIdAsync(int id);

    Task<(int TotalCount, IList<UnitMeasureDto> Items)> GetUnitMeasuresAsync(
        int page = 1,
        int pageSize = 10,
        string? searchString = null,
        string? countryCode = null);
    
    Task<UnitMeasureDto> CreateUnitMeasureAsync(CreateUnitMeasureDto createDto);
    
    Task<UnitMeasureDto> UpdateUnitMeasureAsync(int id, UpdateUnitMeasureDto updateDto);
    
    Task<bool> DeleteUnitMeasureAsync(int id);
    
    Task<bool> ValidateUniqueCodeAsync(string code, string countryCode, int? excludeId = null);
}