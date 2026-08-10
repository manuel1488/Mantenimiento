using App.Core.Common;
using App.Core.DTOs.Servicios;

namespace App.Core.Interfaces;

public interface IServicioService
{
    Task<Result<List<ServicioDto>>> GetAllAsync();
    Task<Result<ServicioDto>> GetByIdAsync(int id);
    Task<Result<ServicioDto>> CreateAsync(CreateServicioDto dto);
    Task<Result<ServicioDto>> UpdateAsync(int id, UpdateServicioDto dto);
    Task<Result> DeleteAsync(int id);
}
