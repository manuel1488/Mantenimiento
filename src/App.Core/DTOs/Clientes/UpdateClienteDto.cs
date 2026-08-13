using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Clientes;

public class UpdateClienteDto
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

    [Required]
    [StringLength(30)]
    public string Telefono { get; set; } = null!;

    [Required]
    [StringLength(150)]
    public string NombreContacto { get; set; } = null!;

    [Required]
    [StringLength(150)]
    [EmailAddress]
    public string Correo { get; set; } = null!;

    [Required]
    [StringLength(150)]
    public string Calle { get; set; } = null!;

    [Required]
    [StringLength(20)]
    public string NumeroExterior { get; set; } = null!;

    [StringLength(20)]
    public string? NumeroInterior { get; set; }

    [Required]
    [StringLength(100)]
    public string Colonia { get; set; } = null!;

    [Required]
    [StringLength(100)]
    public string Ciudad { get; set; } = null!;

    [Required]
    [StringLength(100)]
    public string Estado { get; set; } = null!;

    [Required]
    [StringLength(10)]
    public string CodigoPostal { get; set; } = null!;

    // Datos fiscales
    [Required]
    [StringLength(13)]
    public string Rfc { get; set; } = null!;

    [Required]
    [StringLength(150)]
    public string RazonSocial { get; set; } = null!;

    [Required]
    [StringLength(10)]
    public string RegimenFiscal { get; set; } = null!;

    [Required]
    [StringLength(10)]
    public string CodigoPostalFiscal { get; set; } = null!;

    [Required]
    [StringLength(10)]
    public string UsoCfdi { get; set; } = null!;
}
