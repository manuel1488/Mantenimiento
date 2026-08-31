using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Base;
using App.Core.Enums.Obras;
using App.Core.Interfaces;

namespace App.Models.Obras;

/// <summary>Mensaje/alerta enviado al Cliente de una Obra, con foto opcional. El envío efectivo pasa
/// por <see cref="Core.Interfaces.Notifications.INotificationService"/>, que se encarga de repartirlo
/// entre los canales configurados (correo, y a futuro otros); esta entidad solo deja el historial
/// visible en la UI de la Obra.</summary>
[Table("obr_obra_mensajes")]
public class ObraMensaje : BaseEntity<int>, IAuditTracked
{
    [Required]
    public int ObraId { get; set; }
    public Obra Obra { get; set; } = null!;

    [Required]
    public TipoObraMensaje Tipo { get; set; } = TipoObraMensaje.Mensaje;

    /// <summary>
    /// For <see cref="TipoObraMensaje.Mensaje"/>/<see cref="TipoObraMensaje.Alerta"/>, the free-text
    /// subject the sender typed — stored and shown as-is, in whatever language they wrote it in.
    /// For <see cref="TipoObraMensaje.Avance"/> (system-generated), this holds the Servicio name
    /// instead — plain data, not translatable prose — so the display sentence can be rebuilt in the
    /// viewer's current UI language rather than frozen in whatever language was active when it was
    /// created (see <see cref="PorcentajeAvance"/>).
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Asunto { get; set; } = null!;

    /// <summary>
    /// For Mensaje/Alerta, the free-text body. For Avance, the raw Observaciones note (or empty) —
    /// same reasoning as <see cref="Asunto"/>: kept as data so the sentence around it can be
    /// re-localized at display time.
    /// </summary>
    [Required]
    [StringLength(3000)]
    public string Cuerpo { get; set; } = null!;

    /// <summary>Only set for <see cref="TipoObraMensaje.Avance"/> — the % progress at the time of this
    /// registro, used together with <see cref="Asunto"/>/<see cref="Cuerpo"/> to rebuild the localized
    /// display sentence.</summary>
    public int? PorcentajeAvance { get; set; }

    [StringLength(500)]
    public string? FotoRutaArchivo { get; set; }

    /// <summary>
    /// Clave de la miniatura en el almacenamiento, generada junto con la imagen completa.
    /// Nula si la miniatura falló al subirse — la UI hace fallback a <see cref="FotoRutaArchivo"/>.
    /// </summary>
    [StringLength(500)]
    public string? FotoRutaArchivoThumbnail { get; set; }

    /// <summary>
    /// Resumen legible de a qué canal(es) y dirección(es) se envió (p. ej. "Email: cliente@correo.com"),
    /// fijado al momento del envío — no depende de los datos de contacto actuales del Cliente. El envío
    /// real es channel-agnostic (<see cref="Core.Models.Notifications.NotificationMessage"/>); este campo
    /// es solo para mostrar el historial.
    /// </summary>
    [Required]
    [StringLength(500)]
    public string Destinatarios { get; set; } = null!;

    [Required]
    public DateTime FechaEnvio { get; set; }
}
