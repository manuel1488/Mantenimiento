using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Base;
using App.Core.Enums.Obras;
using App.Core.Interfaces;
using App.Models.Clientes;
using App.Models.Cotizaciones;
using App.Models.Facturas;

namespace App.Models.Obras;

[Table("obr_obras")]
public class Obra : BaseEntity<int>, IAuditTracked
{
    [Required]
    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;

    /// <summary>
    /// Dirección de la obra, texto libre capturado por Obra, sin catálogo de sitios.
    /// </summary>
    [Required]
    [StringLength(300)]
    public string Direccion { get; set; } = null!;

    public bool Urgente { get; set; }

    [Required]
    public ObraEstado Estado { get; set; } = ObraEstado.Solicitada;

    [Required]
    public DateTime FechaSolicitud { get; set; }

    public ICollection<Actividad> Actividades { get; set; } = new List<Actividad>();
    public ICollection<Cotizacion> Cotizaciones { get; set; } = new List<Cotizacion>();
    public Factura? Factura { get; set; }
}
