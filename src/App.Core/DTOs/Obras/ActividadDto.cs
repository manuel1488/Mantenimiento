using App.Core.Enums.Obras;

namespace App.Core.DTOs.Obras;

public class ActividadDto
{
    public int Id { get; set; }
    public int ObraId { get; set; }
    public int ServicioId { get; set; }
    public string ServicioNombre { get; set; } = null!;
    public string UnidadMedidaCodigo { get; set; } = null!;
    public string UnidadMedidaNombre { get; set; } = null!;
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Costo { get; set; }
    public decimal RendimientoDiasPorUnidad { get; set; }
    public decimal TiempoEstimadoDias { get; set; }
    public ActividadEstado Estado { get; set; }
    public int? TecnicoId { get; set; }
    public int? SubcontratistaId { get; set; }
    public int PorcentajeAvance { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
}
