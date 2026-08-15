namespace App.Core.Interfaces;

/// <summary>
/// Completa el vínculo opcional a la clave SAT (c_ClaveUnidad) de las unidades de medida sembradas por defecto,
/// una vez que el catálogo SAT ya fue sembrado. No sobreescribe vínculos ya asignados manualmente.
/// </summary>
public interface IUnidadMedidaSatLinker
{
    Task LinkAsync();
}
