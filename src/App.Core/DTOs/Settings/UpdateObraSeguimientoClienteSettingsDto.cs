using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Settings;

public class UpdateObraSeguimientoClienteSettingsDto
{
    [Required]
    [Range(1, 3650)]
    public int DiasVigenciaPostFinalizacion { get; set; } = 90;
}
