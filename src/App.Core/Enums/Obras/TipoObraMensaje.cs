namespace App.Core.Enums.Obras;

public enum TipoObraMensaje
{
    Mensaje = 1,
    Alerta = 2,

    /// <summary>System-generated notification of a progress update on an Actividad — never chosen
    /// manually in <c>ObraEnviarMensajeDialog</c>, only set by <c>ActividadService</c>.</summary>
    Avance = 3
}
