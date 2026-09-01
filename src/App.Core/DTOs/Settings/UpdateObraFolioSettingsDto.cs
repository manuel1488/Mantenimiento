using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Settings;

public class UpdateObraFolioSettingsDto
{
    [Required]
    [StringLength(20)]
    public string FolioPrefijo { get; set; } = "OBR";

    [Required]
    [Range(1, 10)]
    public int FolioDigitos { get; set; } = 4;
}
