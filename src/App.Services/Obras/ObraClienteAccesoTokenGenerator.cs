using System.Security.Cryptography;

namespace App.Services.Obras;

/// <summary>
/// Genera el token no adivinable del enlace público de seguimiento del Cliente (ver
/// <see cref="App.Models.Obras.ObraClienteAcceso"/>). Usado tanto al crear una Obra
/// (<see cref="ObraService.CreateAsync"/>) como al regenerar un enlace existente
/// (<see cref="ObraClienteAccesoService.RegenerarTokenAsync"/>).
/// </summary>
public static class ObraClienteAccesoTokenGenerator
{
    public static string Generate() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(24))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
