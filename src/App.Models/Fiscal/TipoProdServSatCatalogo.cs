using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Base;

namespace App.Models.Fiscal;

/// <summary>
/// Nivel "Tipo" (Productos/Servicios) del catálogo SAT c_ClaveProdServ, usado como primer
/// paso del asistente de selección por categoría. Fuente: phpcfdi/resources-sat-pys (Unlicense).
/// </summary>
[Table("cat_tipos_prod_serv_sat")]
public class TipoProdServSatCatalogo : BaseEntity<int>
{
    [Required]
    [StringLength(2)]
    public string Codigo { get; set; } = null!;

    [Required]
    [StringLength(200)]
    public string Descripcion { get; set; } = null!;
}
