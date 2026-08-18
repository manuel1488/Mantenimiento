using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Base;

namespace App.Models.Fiscal;

/// <summary>
/// Nivel "Segmento" del catálogo SAT c_ClaveProdServ, hijo de Tipo y padre de Familia.
/// Fuente: phpcfdi/resources-sat-pys (Unlicense). <see cref="TipoCodigo"/> es una clave de
/// filtrado plana, no una FK real, siguiendo el patrón de los demás catálogos SAT importados por CSV.
/// </summary>
[Table("cat_segmentos_prod_serv_sat")]
public class SegmentoProdServSatCatalogo : BaseEntity<int>
{
    [Required]
    [StringLength(2)]
    public string Codigo { get; set; } = null!;

    [Required]
    [StringLength(200)]
    public string Descripcion { get; set; } = null!;

    [Required]
    [StringLength(2)]
    public string TipoCodigo { get; set; } = null!;
}
