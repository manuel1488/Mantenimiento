using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using App.Core.Common;
using App.Core.DTOs.Billing.Mexico;
using App.Core.Interfaces.Billing;
using App.Models.Data.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.Services.Billing;

/// <summary>
/// SW Sapien PAC integration for CFDI stamping.
/// Supports both infinite-token and username/password authentication.
/// </summary>
public class SwSapienService : ISwSapienService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<SwSapienService> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public SwSapienService(
        IHttpClientFactory httpClientFactory,
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<SwSapienService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<Result<SwSapienStampData>> StampAsync(string signedXml)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var settings = await context.MexicoPacSettings.FirstOrDefaultAsync();

            if (settings == null)
                return Result<SwSapienStampData>.Failure("No hay configuración PAC disponible");

            var apiUrl = settings.IsProduction
                ? settings.ProductionUrl
                : (settings.TestUrl ?? settings.ProductionUrl);

            // Get auth token
            var tokenResult = await GetTokenAsync(settings.Token, settings.User, settings.Password, apiUrl);
            if (!tokenResult.IsSuccess)
                return Result<SwSapienStampData>.Failure(tokenResult.Error!);

            // Stamp XML
            return await StampXmlAsync(apiUrl, tokenResult.Value!, signedXml);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stamping CFDI with SW Sapien");
            return Result<SwSapienStampData>.Failure($"Error al timbrar: {ex.Message}");
        }
    }

    public async Task<Result> TestConnectionAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var settings = await context.MexicoPacSettings.FirstOrDefaultAsync();

            if (settings == null)
                return Result.Failure("No hay configuración PAC disponible");

            var apiUrl = settings.IsProduction
                ? settings.ProductionUrl
                : (settings.TestUrl ?? settings.ProductionUrl);

            var tokenResult = await GetTokenAsync(settings.Token, settings.User, settings.Password, apiUrl);
            if (!tokenResult.IsSuccess)
                return Result.Failure(tokenResult.Error!);

            _logger.LogInformation("Prueba de conexión PAC exitosa");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing PAC connection");
            return Result.Failure($"Error en prueba de conexión: {ex.Message}");
        }
    }

    #region Private helpers

    private async Task<Result<string>> GetTokenAsync(
        string? infiniteToken, string? user, string? password, string apiUrl)
    {
        // If an infinite token is configured, use it directly
        if (!string.IsNullOrWhiteSpace(infiniteToken))
            return Result<string>.Success(infiniteToken);

        // Authenticate with user/password
        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
            return Result<string>.Failure("Se requiere token o usuario/contraseña para autenticarse con el PAC");

        try
        {
            var http = _httpClientFactory.CreateClient();
            var body = new SwSapienAuthRequest { User = user, Password = password };
            var content = new StringContent(
                JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            var response = await http.PostAsync($"{apiUrl}/v2/security/authenticate", content);
            var json = await response.Content.ReadAsStringAsync();

            var auth = JsonSerializer.Deserialize<SwSapienAuthResponse>(json, _jsonOptions);
            if (auth?.Status != "success" || auth.Data == null)
                return Result<string>.Failure($"Autenticación fallida: {auth?.Message}");

            return Result<string>.Success(auth.Data.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error authenticating with SW Sapien");
            return Result<string>.Failure($"Error de autenticación: {ex.Message}");
        }
    }

    private async Task<Result<SwSapienStampData>> StampXmlAsync(
        string apiUrl, string token, string signedXml)
    {
        try
        {
            var http = _httpClientFactory.CreateClient();
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var form = new MultipartFormDataContent();
            var xmlBytes = Encoding.UTF8.GetBytes(signedXml);
            var xmlContent = new ByteArrayContent(xmlBytes);
            xmlContent.Headers.ContentType = new MediaTypeHeaderValue("application/xml");
            form.Add(xmlContent, "xml", "cfdi.xml");

            var stampUrl = $"{apiUrl}/cfdi33/stamp/v4";
            _logger.LogInformation("Llamando al endpoint de timbrado PAC: {Url}", stampUrl);

            var response = await http.PostAsync(stampUrl, form);
            var json = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("Respuesta del PAC: {Response}", json);

            var stamp = JsonSerializer.Deserialize<SwSapienStampResponse>(json, _jsonOptions);
            if (stamp?.Status != "success" || stamp.Data == null)
            {
                var error = $"{stamp?.Message} {stamp?.MessageDetail}".Trim();
                _logger.LogError("Error de timbrado PAC: {Error}", error);
                return Result<SwSapienStampData>.Failure(error.Length > 0
                    ? error : "Error desconocido del PAC");
            }

            _logger.LogInformation("CFDI timbrado exitosamente. UUID: {Uuid}", stamp.Data.Uuid);
            return Result<SwSapienStampData>.Success(stamp.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling PAC stamping endpoint");
            return Result<SwSapienStampData>.Failure($"Error en timbrado: {ex.Message}");
        }
    }

    #endregion
}
