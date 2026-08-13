namespace App.Core.DTOs.Clientes;

public class ClienteDto
{
    public int Id { get; set; }

    // Datos comerciales
    public string Nombre { get; set; } = null!;
    public string? NombreComercial { get; set; }
    public string Pais { get; set; } = null!;
    public string Telefono { get; set; } = null!;
    public string NombreContacto { get; set; } = null!;
    public string Correo { get; set; } = null!;
    public string Calle { get; set; } = null!;
    public string NumeroExterior { get; set; } = null!;
    public string? NumeroInterior { get; set; }
    public string Colonia { get; set; } = null!;
    public string Ciudad { get; set; } = null!;
    public string Estado { get; set; } = null!;
    public string CodigoPostal { get; set; } = null!;

    // Datos fiscales
    public string Rfc { get; set; } = null!;
    public string RazonSocial { get; set; } = null!;
    public string RegimenFiscal { get; set; } = null!;
    public string CodigoPostalFiscal { get; set; } = null!;
    public string UsoCfdi { get; set; } = null!;
}
