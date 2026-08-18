using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Base;

namespace App.Models.Fiscal;

/// <summary>
/// Nivel "Familia" del catálogo SAT c_ClaveProdServ, hijo de Segmento y padre de Clase.
/// Fuente: phpcfdi/resources-sat-pys (Unlicense). <see cref="SegmentoCodigo"/> es una clave de
/// filtrado plana, no una FK real, siguiendo el patrón de los demás catálogos SAT importados por CSV.
/// </summary>
[Table("cat_familias_prod_serv_sat")]
public class FamiliaProdServSatCatalogo : BaseEntity<int>
{
    [Required]
    [StringLength(4)]
    public string Codigo { get; set; } = null!;

    [Required]
    [StringLength(200)]
    public string Descripcion { get; set; } = null!;

    [Required]
    [StringLength(2)]
    public string SegmentoCodigo { get; set; } = null!;
}
