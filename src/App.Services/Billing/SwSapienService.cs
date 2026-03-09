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

    // ── Balance API ──────────────────────────────────────────────────────────

    public async Task<Result<SwSapienStampBalanceData>> GetStampBalanceAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var settings = await context.MexicoPacSettings.FirstOrDefaultAsync();

            if (settings == null)
                return Result<SwSapienStampBalanceData>.Failure("No hay configuración PAC disponible");

            var stampingUrl = settings.IsProduction
                ? settings.ProductionUrl
                : (settings.TestUrl ?? settings.ProductionUrl);

            var tokenResult = await GetTokenAsync(settings.Token, settings.User, settings.Password, stampingUrl);
            if (!tokenResult.IsSuccess)
                return Result<SwSapienStampBalanceData>.Failure(tokenResult.Error!);

            // SW Sapien management API uses a different base URL than the stamping API
            var managementUrl = GetManagementApiUrl(stampingUrl);

            var http = _httpClientFactory.CreateClient();
            http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", tokenResult.Value!);

            var response = await http.GetAsync($"{managementUrl}/management/v2/api/users/balance");
            var json = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("SW Sapien balance response: {Json}", json);

            var result = JsonSerializer.Deserialize<SwSapienBalanceApiResponse>(json, _jsonOptions);
            if (result?.Status != "success" || result.Data == null)
                return Result<SwSapienStampBalanceData>.Failure(
                    $"Error al consultar saldo de timbres: {result?.Message ?? "respuesta inválida del PAC"}");

            return Result<SwSapienStampBalanceData>.Success(result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stamp balance from SW Sapien");
            return Result<SwSapienStampBalanceData>.Failure($"Error al consultar saldo: {ex.Message}");
        }
    }

    /// <summary>
    /// Derives the SW Sapien Management API base URL from the stamping URL.
    /// services.sw.com.mx     → api.sw.com.mx
    /// services.test.sw.com.mx → api.test.sw.com.mx
    /// </summary>
    private static string GetManagementApiUrl(string stampingUrl) =>
        stampingUrl
            .Replace("services.test.sw.com.mx", "api.test.sw.com.mx")
            .Replace("services.sw.com.mx", "api.sw.com.mx")
            .TrimEnd('/');

    // ── Cancellation ─────────────────────────────────────────────────────────

    public async Task<Result<SwSapienCancelData>> CancelCfdiAsync(
        string uuid,
        string issuerRfc,
        string receiverRfc,
        decimal total,
        string cancellationReason,
        string? replacementUuid = null)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var settings = await context.MexicoPacSettings.FirstOrDefaultAsync();

            if (settings == null)
                return Result<SwSapienCancelData>.Failure("No hay configuración PAC disponible");

            if (string.IsNullOrEmpty(settings.CsdCertificateBase64))
                return Result<SwSapienCancelData>.Failure("No hay certificado CSD configurado");

            if (string.IsNullOrEmpty(settings.CsdPrivateKeyBase64))
                return Result<SwSapienCancelData>.Failure("No hay llave privada CSD configurada");

            if (string.IsNullOrEmpty(settings.CsdPassword))
                return Result<SwSapienCancelData>.Failure("No hay contraseña del CSD configurada");

            var apiUrl = (settings.IsProduction
                ? settings.ProductionUrl
                : (settings.TestUrl ?? settings.ProductionUrl)).TrimEnd('/');

            var tokenResult = await GetTokenAsync(settings.Token, settings.User, settings.Password, apiUrl);
            if (!tokenResult.IsSuccess)
                return Result<SwSapienCancelData>.Failure(tokenResult.Error!);

            return await CancelCfdiInternalAsync(
                apiUrl, tokenResult.Value!,
                uuid, issuerRfc, receiverRfc, total,
                cancellationReason, replacementUuid,
                settings.CsdCertificateBase64, settings.CsdPrivateKeyBase64, settings.CsdPassword);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling CFDI {Uuid}", uuid);
            return Result<SwSapienCancelData>.Failure($"Error al cancelar: {ex.Message}");
        }
    }

    public async Task<Result<SwSapienCancelData>> CheckCancellationStatusAsync(
        string uuid,
        string issuerRfc,
        string receiverRfc,
        decimal total,
        string cancellationReason,
        string? replacementUuid = null)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var settings = await context.MexicoPacSettings.FirstOrDefaultAsync();

            if (settings == null)
                return Result<SwSapienCancelData>.Failure("No hay configuración PAC disponible");

            if (string.IsNullOrEmpty(settings.CsdCertificateBase64))
                return Result<SwSapienCancelData>.Failure("No hay certificado CSD configurado");

            if (string.IsNullOrEmpty(settings.CsdPrivateKeyBase64))
                return Result<SwSapienCancelData>.Failure("No hay llave privada CSD configurada");

            if (string.IsNullOrEmpty(settings.CsdPassword))
                return Result<SwSapienCancelData>.Failure("No hay contraseña del CSD configurada");

            var apiUrl = (settings.IsProduction
                ? settings.ProductionUrl
                : (settings.TestUrl ?? settings.ProductionUrl)).TrimEnd('/');

            var tokenResult = await GetTokenAsync(settings.Token, settings.User, settings.Password, apiUrl);
            if (!tokenResult.IsSuccess)
                return Result<SwSapienCancelData>.Failure(tokenResult.Error!);

            return await CancelCfdiInternalAsync(
                apiUrl, tokenResult.Value!,
                uuid, issuerRfc, receiverRfc, total,
                cancellationReason, replacementUuid,
                settings.CsdCertificateBase64, settings.CsdPrivateKeyBase64, settings.CsdPassword);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cancellation status for CFDI {Uuid}", uuid);
            return Result<SwSapienCancelData>.Failure($"Error al consultar estado de cancelación: {ex.Message}");
        }
    }

    private async Task<Result<SwSapienCancelData>> CancelCfdiInternalAsync(
        string apiUrl, string token,
        string uuid, string issuerRfc, string receiverRfc, decimal total,
        string motivo, string? folioSustitucion,
        string b64Cer, string b64Key, string password)
    {
        try
        {
            var http = _httpClientFactory.CreateClient();
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var request = new SwSapienCancelRequest
            {
                Rfc = issuerRfc,
                RfcReceptor = receiverRfc,
                Total = total.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                Uuid = uuid,
                Motivo = motivo,
                FolioSustitucion = motivo == "01" ? folioSustitucion : null,
                B64Cer = b64Cer,
                B64Key = b64Key,
                Password = password
            };

            var jsonBody = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var cancelUrl = $"{apiUrl}/cfdi33/cancel/csd/status";
            _logger.LogInformation("Calling PAC cancel endpoint: {Url} for UUID: {Uuid}", cancelUrl, uuid);

            var response = await http.PostAsync(cancelUrl,
                new StringContent(jsonBody, Encoding.UTF8, "application/json"));

            var json = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("PAC cancel response: {Response}", json);

            var cancelResponse = JsonSerializer.Deserialize<SwSapienCancelResponse>(json, _jsonOptions);
            if (cancelResponse?.Status != "success" || cancelResponse.Data == null)
            {
                var error = $"{cancelResponse?.Message} {cancelResponse?.MessageDetail}".Trim();
                _logger.LogError("PAC cancellation error: {Error}", error);
                return Result<SwSapienCancelData>.Failure(
                    error.Length > 0 ? error : "Error desconocido del PAC al cancelar");
            }

            _logger.LogInformation("CFDI cancellation response received. StatusSat: {StatusSat}",
                cancelResponse.Data.StatusSat);
            return Result<SwSapienCancelData>.Success(cancelResponse.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling PAC cancellation endpoint");
            return Result<SwSapienCancelData>.Failure($"Error en cancelación: {ex.Message}");
        }
    }

}
