namespace App.Web.Components.Admin.Clientes.Models;

public class ClienteDialogModel
{
    public int Id { get; set; }

    // Datos comerciales
    public string Nombre { get; set; } = string.Empty;
    public string? NombreComercial { get; set; }
    public string Pais { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string NombreContacto { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string Calle { get; set; } = string.Empty;
    public string NumeroExterior { get; set; } = string.Empty;
    public string? NumeroInterior { get; set; }
    public string Colonia { get; set; } = string.Empty;
    public string Ciudad { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string CodigoPostal { get; set; } = string.Empty;

    // Datos fiscales
    public string Rfc { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;
    public string RegimenFiscal { get; set; } = string.Empty;
    public string CodigoPostalFiscal { get; set; } = string.Empty;
    public string UsoCfdi { get; set; } = string.Empty;
}
