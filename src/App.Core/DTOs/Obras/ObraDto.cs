using App.Core.Enums.Obras;

namespace App.Core.DTOs.Obras;

public class ObraDto
{
    public int Id { get; set; }
    public int? FolioAnio { get; set; }
    public int? FolioNumero { get; set; }
    public int ClienteId { get; set; }
    public string ClienteNombre { get; set; } = null!;
    public string Direccion { get; set; } = null!;
    public bool Urgente { get; set; }
    public ObraEstado Estado { get; set; }
    public DateTime FechaSolicitud { get; set; }
    public int? CotizacionOrigenId { get; set; }
    public int PorcentajeAvance { get; set; }
}
