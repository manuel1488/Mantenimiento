using App.Core.Common;
using App.Core.DTOs.Clientes;

namespace App.Core.Interfaces;

public interface IClienteService
{
    Task<Result<List<ClienteDto>>> GetAllAsync();
    Task<Result<ClienteDto>> GetByIdAsync(int id);
    Task<Result<ClienteDto>> CreateAsync(CreateClienteDto dto);
    Task<Result<ClienteDto>> UpdateAsync(int id, UpdateClienteDto dto);
    Task<Result> DeleteAsync(int id);
}
