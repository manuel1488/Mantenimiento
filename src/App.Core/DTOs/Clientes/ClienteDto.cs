namespace App.Core.DTOs.Clientes;

public class ClienteDto
{
    public int Id { get; set; }

    // Datos comerciales
    public string Nombre { get; set; } = null!;
    public string? NombreComercial { get; set; }
    public string Pais { get; set; } = null!;
    public string? Telefono { get; set; }
    public string? NombreContacto { get; set; }
    public string? Correo { get; set; }
    public string? Calle { get; set; }
    public string? NumeroExterior { get; set; }
    public string? NumeroInterior { get; set; }
    public string? Colonia { get; set; }
    public string? Ciudad { get; set; }
    public string? Estado { get; set; }
    public string? CodigoPostal { get; set; }

    // Datos fiscales
    public bool TieneDatosFiscales { get; set; }
    public string? Rfc { get; set; }
    public string? RazonSocial { get; set; }
    public string? CorreoFiscal { get; set; }
    public string? CalleFiscal { get; set; }
    public string? NumeroExteriorFiscal { get; set; }
    public string? NumeroInteriorFiscal { get; set; }
    public string? ColoniaFiscal { get; set; }
    public string? CiudadFiscal { get; set; }
    public string? EstadoFiscal { get; set; }
    public string? RegimenFiscal { get; set; }
    public string? CodigoPostalFiscal { get; set; }
    public string? UsoCfdi { get; set; }

    // Preferencias de facturación México
    public bool FacturacionAutomatica { get; set; }
    public bool EnviarCorreoFactura { get; set; }
}
