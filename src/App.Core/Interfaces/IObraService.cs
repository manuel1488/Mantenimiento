using App.Core.Common;
using App.Core.DTOs.Obras;

namespace App.Core.Interfaces;

public interface IObraService
{
    Task<Result<List<ObraDto>>> GetAllAsync();
    Task<Result<ObraDto>> GetByIdAsync(int id);
    Task<Result<ObraDto>> CreateAsync(CreateObraDto dto);
    Task<Result<ObraDto>> UpdateAsync(UpdateObraDto dto);
    Task<Result> DeleteAsync(int id);
    Task<Result<ObraDto>> FinalizarAsync(int id);
}
