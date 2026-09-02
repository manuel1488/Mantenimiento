namespace App.Core.DTOs.Obras;

/// <summary>Vista de administración del enlace público de seguimiento del Cliente de una Obra.</summary>
public class ObraClienteAccesoDto
{
    public int ObraId { get; set; }
    public string Token { get; set; } = null!;
    public bool Habilitado { get; set; }
    public DateTime TokenGeneradoEn { get; set; }

    /// <summary>Null mientras la Obra no esté Finalizada — el enlace no expira por fecha hasta entonces.</summary>
    public DateTime? ExpiraEn { get; set; }

    /// <summary>true si <see cref="Habilitado"/> y (no ha expirado o la Obra no ha finalizado).</summary>
    public bool Vigente { get; set; }
}
