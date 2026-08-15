using App.Core.Common;
using App.Core.DTOs.Servicios;

namespace App.Core.Interfaces;

public interface IUnidadMedidaService
{
    Task<Result<List<UnidadMedidaDto>>> GetAllAsync();
    Task<Result<UnidadMedidaDto>> GetByIdAsync(int id);
    Task<Result<UnidadMedidaDto>> CreateAsync(CreateUnidadMedidaDto dto);
    Task<Result<UnidadMedidaDto>> UpdateAsync(int id, UpdateUnidadMedidaDto dto);
    Task<Result> DeleteAsync(int id);
}
