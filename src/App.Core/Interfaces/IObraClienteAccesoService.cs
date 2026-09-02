using App.Core.Common;
using App.Core.DTOs.Obras;

namespace App.Core.Interfaces;

/// <summary>
/// Administra el enlace público de seguimiento del Cliente de una Obra (ver
/// <see cref="Models.Obras.ObraClienteAcceso"/> en App.Models). Los métodos de administración
/// requieren un Coordinador autenticado; <see cref="ResolveTokenAsync"/> es la única entrada
/// pensada para correr sin usuario ASP.NET, desde la página pública.
/// </summary>
public interface IObraClienteAccesoService
{
    Task<Result<ObraClienteAccesoDto>> GetByObraIdAsync(int obraId, CancellationToken cancellationToken = default);

    /// <summary>Genera un nuevo token aleatorio para la Obra, invalidando el anterior.</summary>
    Task<Result<ObraClienteAccesoDto>> RegenerarTokenAsync(int obraId, CancellationToken cancellationToken = default);

    Task<Result<ObraClienteAccesoDto>> SetHabilitadoAsync(int obraId, bool habilitado, CancellationToken cancellationToken = default);

    /// <summary>Emails the given (already-built) tracking URL to the given address, in the Obra's
    /// context (subject/body mention the Obra's address).</summary>
    Task<Result> SendLinkByEmailAsync(int obraId, string url, string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resuelve un token público a su ObraId si es válido: existe, está habilitado y (la Obra no ha
    /// finalizado o sigue dentro de la vigencia configurada). Devuelve el mismo error genérico en
    /// cualquier caso de invalidez, para no revelar por qué falló.
    /// </summary>
    Task<Result<int>> ResolveTokenAsync(string token, CancellationToken cancellationToken = default);
}
