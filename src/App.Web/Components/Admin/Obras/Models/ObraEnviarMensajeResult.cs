using App.Core.Enums.Obras;

namespace App.Web.Components.Admin.Obras.Models;

public record ObraEnviarMensajeResult(
    TipoObraMensaje Tipo,
    string Asunto,
    string Cuerpo,
    byte[]? FotoData,
    string? FotoContentType,
    string? FotoFileName);
