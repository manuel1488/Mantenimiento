using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Settings;

public class UpdateCotizacionTemplateSettingsDto
{
    [Required]
    public string HtmlContent { get; set; } = null!;

    public string CssContent { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? PaymentTermsText { get; set; }

    public bool MostrarDatosBancarios { get; set; }

    [StringLength(150)]
    public string? BancoBeneficiario { get; set; }

    [StringLength(13)]
    public string? BancoRfc { get; set; }

    [StringLength(100)]
    public string? BancoNombre { get; set; }

    [StringLength(50)]
    public string? BancoNumeroCuenta { get; set; }

    [StringLength(18)]
    public string? BancoClabe { get; set; }

    [StringLength(20)]
    public string? BancoSwift { get; set; }

    public bool MostrarDireccionEnCotizacion { get; set; }

    [StringLength(300)]
    public string? Direccion { get; set; }

    public bool MostrarContacto { get; set; }

    [StringLength(200)]
    public string? SitioWeb { get; set; }

    [StringLength(30)]
    public string? Telefono { get; set; }

    [StringLength(150)]
    public string? CorreoElectronico { get; set; }

    [StringLength(30)]
    public string? WhatsApp { get; set; }

    [StringLength(150)]
    public string? Facebook { get; set; }

    [StringLength(150)]
    public string? Instagram { get; set; }

    [Required]
    [StringLength(20)]
    public string FolioPrefijo { get; set; } = "COT";

    [Range(1, 10)]
    public int FolioDigitos { get; set; } = 4;
}
