using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Base;

namespace App.Models.Fiscal;

/// <summary>
/// Catálogo oficial SAT c_ClaveProdServ, usado para CFDI y como referencia opcional del catálogo propio de Servicio.
/// </summary>
[Table("cat_claves_prod_serv_sat")]
public class ClaveProdServSatCatalogo : BaseEntity<int>
{
    [Required]
    [StringLength(10)]
    public string Codigo { get; set; } = null!;

    [Required]
    [StringLength(250)]
    public string Descripcion { get; set; } = null!;
}
