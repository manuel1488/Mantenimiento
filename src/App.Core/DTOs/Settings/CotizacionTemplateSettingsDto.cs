namespace App.Core.DTOs.Settings;

public class CotizacionTemplateSettingsDto
{
    public int Id { get; set; }
    public string HtmlContent { get; set; } = null!;
    public string CssContent { get; set; } = string.Empty;
    public string? PaymentTermsText { get; set; }
    public bool MostrarDatosBancarios { get; set; }
    public string? BancoBeneficiario { get; set; }
    public string? BancoRfc { get; set; }
    public string? BancoNombre { get; set; }
    public string? BancoNumeroCuenta { get; set; }
    public string? BancoClabe { get; set; }
    public string? BancoSwift { get; set; }
    public bool MostrarDireccionEnCotizacion { get; set; }
    public string? Direccion { get; set; }
    public bool MostrarContacto { get; set; }
    public string? SitioWeb { get; set; }
    public string? Telefono { get; set; }
    public string? CorreoElectronico { get; set; }
    public string? WhatsApp { get; set; }
    public string? Facebook { get; set; }
    public string? Instagram { get; set; }
    public string FolioPrefijo { get; set; } = "COT";
    public int FolioDigitos { get; set; } = 4;
}
