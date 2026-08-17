using App.Core.DTOs.Obras;

namespace App.Web.Components.Admin.Obras.Models;

public static class ObraDialogMapper
{
    public static ObraDialogModel ToDialogModel(ObraDto obra) => new()
    {
        Id = obra.Id,
        ClienteId = obra.ClienteId,
        Direccion = obra.Direccion,
        Urgente = obra.Urgente
    };

    public static CreateObraDto ToCreateDto(ObraDialogModel model) => new()
    {
        ClienteId = model.ClienteId,
        Direccion = model.Direccion,
        Urgente = model.Urgente
    };

    public static UpdateObraDto ToUpdateDto(ObraDialogModel model) => new()
    {
        Id = model.Id,
        ClienteId = model.ClienteId,
        Direccion = model.Direccion,
        Urgente = model.Urgente
    };
}
