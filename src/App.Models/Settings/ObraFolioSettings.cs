using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Core.Base;
using App.Core.Interfaces;

namespace App.Models.Settings;

[Table("stg_obra_folio_settings")]
public class ObraFolioSettings : BaseEntity<int>, IAuditTracked
{
    [Required]
    [StringLength(20)]
    public string FolioPrefijo { get; set; } = "OBR";

    [Required]
    public int FolioDigitos { get; set; } = 4;
}
