using App.Core.DTOs.Fiscal;

namespace App.Core.Interfaces;

public interface IFiscalCatalogService
{
    Task<IList<RegimenFiscalCatalogoDto>> GetRegimenesFiscalesAsync();
    Task<IList<UsoCfdiCatalogoDto>> GetUsosCfdiAsync();
    Task<IList<UsoCfdiCatalogoDto>> GetUsosCfdiPorRegimenAsync(string codigoRegimenFiscal);
    Task<(int TotalCount, IList<ClaveUnidadSatCatalogoDto> Items)> SearchClavesUnidadSatAsync(
        string? searchText = null,
        int page = 1,
        int pageSize = 50);
    Task<(int TotalCount, IList<ClaveProdServSatCatalogoDto> Items)> SearchClavesProdServSatAsync(
        string? searchText = null,
        int page = 1,
        int pageSize = 50);

    /// <summary>
    /// Busca entradas de nivel "Clase" del catálogo SAT (código de 6 dígitos + "00"),
    /// usado como paso intermedio del asistente de selección por categoría.
    /// </summary>
    Task<(int TotalCount, IList<ClaveProdServSatCatalogoDto> Items)> SearchClasesProdServSatAsync(
        string? searchText = null,
        int page = 1,
        int pageSize = 50);

    /// <summary>
    /// Lista los códigos de Producto/Servicio (hoja) que pertenecen a una Clase dada
    /// (los primeros 6 dígitos de <paramref name="claseCodigo"/>), excluyendo la fila de la Clase misma.
    /// </summary>
    Task<(int TotalCount, IList<ClaveProdServSatCatalogoDto> Items)> GetProductosPorClaseAsync(
        string claseCodigo,
        string? searchText = null,
        int page = 1,
        int pageSize = 50);

    /// <summary>
    /// Niveles superiores de la jerarquía del catálogo SAT c_ClaveProdServ (Tipo/Segmento/Familia),
    /// usados por el asistente de selección en cascada. Fuente: phpcfdi/resources-sat-pys.
    /// </summary>
    Task<IList<TipoProdServSatCatalogoDto>> GetTiposProdServSatAsync();

    Task<IList<SegmentoProdServSatCatalogoDto>> GetSegmentosProdServSatAsync(string tipoCodigo);

    Task<IList<FamiliaProdServSatCatalogoDto>> GetFamiliasProdServSatAsync(string segmentoCodigo);

    /// <summary>
    /// Lista las entradas de nivel "Clase" (código de 6 dígitos + "00") cuyo prefijo de Familia
    /// (primeros 4 dígitos) coincide con <paramref name="familiaCodigo"/>.
    /// </summary>
    Task<IList<ClaveProdServSatCatalogoDto>> GetClasesPorFamiliaAsync(string familiaCodigo);
}
