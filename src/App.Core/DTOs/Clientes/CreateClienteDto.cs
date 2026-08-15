using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Clientes;

public class CreateClienteDto
{
    // Datos comerciales
    [Required]
    [StringLength(150)]
    public string Nombre { get; set; } = null!;

    [StringLength(150)]
    public string? NombreComercial { get; set; }

    [Required]
    [StringLength(100)]
    public string Pais { get; set; } = null!;

    [StringLength(30)]
    public string? Telefono { get; set; }

    [StringLength(150)]
    public string? NombreContacto { get; set; }

    [StringLength(150)]
    [EmailAddress]
    public string? Correo { get; set; }

    [StringLength(150)]
    public string? Calle { get; set; }

    [StringLength(20)]
    public string? NumeroExterior { get; set; }

    [StringLength(20)]
    public string? NumeroInterior { get; set; }

    [StringLength(100)]
    public string? Colonia { get; set; }

    [StringLength(100)]
    public string? Ciudad { get; set; }

    [StringLength(100)]
    public string? Estado { get; set; }

    [StringLength(10)]
    public string? CodigoPostal { get; set; }

    // Datos fiscales
    public bool TieneDatosFiscales { get; set; }

    [StringLength(13)]
    public string? Rfc { get; set; }

    [StringLength(150)]
    public string? RazonSocial { get; set; }

    [StringLength(150)]
    [EmailAddress]
    public string? CorreoFiscal { get; set; }

    [StringLength(150)]
    public string? CalleFiscal { get; set; }

    [StringLength(20)]
    public string? NumeroExteriorFiscal { get; set; }

    [StringLength(20)]
    public string? NumeroInteriorFiscal { get; set; }

    [StringLength(100)]
    public string? ColoniaFiscal { get; set; }

    [StringLength(100)]
    public string? CiudadFiscal { get; set; }

    [StringLength(100)]
    public string? EstadoFiscal { get; set; }

    [StringLength(10)]
    public string? RegimenFiscal { get; set; }

    [StringLength(10)]
    public string? CodigoPostalFiscal { get; set; }

    [StringLength(10)]
    public string? UsoCfdi { get; set; }

    // Preferencias de facturación México
    public bool FacturacionAutomatica { get; set; }
    public bool EnviarCorreoFactura { get; set; }
}
