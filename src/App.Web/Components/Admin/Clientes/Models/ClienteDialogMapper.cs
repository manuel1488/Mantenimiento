using App.Core.DTOs.Clientes;

namespace App.Web.Components.Admin.Clientes.Models;

public static class ClienteDialogMapper
{
    public static ClienteDialogModel ToDialogModel(ClienteDto cliente) => new()
    {
        Id = cliente.Id,
        Nombre = cliente.Nombre,
        NombreComercial = cliente.NombreComercial,
        Pais = cliente.Pais,
        Telefono = cliente.Telefono,
        NombreContacto = cliente.NombreContacto,
        Correo = cliente.Correo,
        Calle = cliente.Calle,
        NumeroExterior = cliente.NumeroExterior,
        NumeroInterior = cliente.NumeroInterior,
        Colonia = cliente.Colonia,
        Ciudad = cliente.Ciudad,
        Estado = cliente.Estado,
        CodigoPostal = cliente.CodigoPostal,
        TieneDatosFiscales = cliente.TieneDatosFiscales,
        Rfc = cliente.Rfc,
        RazonSocial = cliente.RazonSocial,
        CorreoFiscal = cliente.CorreoFiscal,
        CalleFiscal = cliente.CalleFiscal,
        NumeroExteriorFiscal = cliente.NumeroExteriorFiscal,
        NumeroInteriorFiscal = cliente.NumeroInteriorFiscal,
        ColoniaFiscal = cliente.ColoniaFiscal,
        CiudadFiscal = cliente.CiudadFiscal,
        EstadoFiscal = cliente.EstadoFiscal,
        RegimenFiscal = cliente.RegimenFiscal,
        CodigoPostalFiscal = cliente.CodigoPostalFiscal,
        UsoCfdi = cliente.UsoCfdi,
        FacturacionAutomatica = cliente.FacturacionAutomatica,
        EnviarCorreoFactura = cliente.EnviarCorreoFactura
    };

    public static CreateClienteDto ToCreateDto(ClienteDialogModel model) => new()
    {
        Nombre = model.Nombre,
        NombreComercial = model.NombreComercial,
        Pais = model.Pais,
        Telefono = model.Telefono,
        NombreContacto = model.NombreContacto,
        Correo = model.Correo,
        Calle = model.Calle,
        NumeroExterior = model.NumeroExterior,
        NumeroInterior = model.NumeroInterior,
        Colonia = model.Colonia,
        Ciudad = model.Ciudad,
        Estado = model.Estado,
        CodigoPostal = model.CodigoPostal,
        TieneDatosFiscales = model.TieneDatosFiscales,
        Rfc = model.Rfc,
        RazonSocial = model.RazonSocial,
        CorreoFiscal = model.CorreoFiscal,
        CalleFiscal = model.CalleFiscal,
        NumeroExteriorFiscal = model.NumeroExteriorFiscal,
        NumeroInteriorFiscal = model.NumeroInteriorFiscal,
        ColoniaFiscal = model.ColoniaFiscal,
        CiudadFiscal = model.CiudadFiscal,
        EstadoFiscal = model.EstadoFiscal,
        RegimenFiscal = model.RegimenFiscal,
        CodigoPostalFiscal = model.CodigoPostalFiscal,
        UsoCfdi = model.UsoCfdi,
        FacturacionAutomatica = model.FacturacionAutomatica,
        EnviarCorreoFactura = model.EnviarCorreoFactura
    };

    public static UpdateClienteDto ToUpdateDto(ClienteDialogModel model) => new()
    {
        Nombre = model.Nombre,
        NombreComercial = model.NombreComercial,
        Pais = model.Pais,
        Telefono = model.Telefono,
        NombreContacto = model.NombreContacto,
        Correo = model.Correo,
        Calle = model.Calle,
        NumeroExterior = model.NumeroExterior,
        NumeroInterior = model.NumeroInterior,
        Colonia = model.Colonia,
        Ciudad = model.Ciudad,
        Estado = model.Estado,
        CodigoPostal = model.CodigoPostal,
        TieneDatosFiscales = model.TieneDatosFiscales,
        Rfc = model.Rfc,
        RazonSocial = model.RazonSocial,
        CorreoFiscal = model.CorreoFiscal,
        CalleFiscal = model.CalleFiscal,
        NumeroExteriorFiscal = model.NumeroExteriorFiscal,
        NumeroInteriorFiscal = model.NumeroInteriorFiscal,
        ColoniaFiscal = model.ColoniaFiscal,
        CiudadFiscal = model.CiudadFiscal,
        EstadoFiscal = model.EstadoFiscal,
        RegimenFiscal = model.RegimenFiscal,
        CodigoPostalFiscal = model.CodigoPostalFiscal,
        UsoCfdi = model.UsoCfdi,
        FacturacionAutomatica = model.FacturacionAutomatica,
        EnviarCorreoFactura = model.EnviarCorreoFactura
    };
}
