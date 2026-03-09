namespace App.Core.DTOs.Billing.Mexico;

/// <summary>Response from SW Sapien PAC stamping.</summary>
public class SwSapienStampResponse
{
    public string? Status { get; set; }
    public SwSapienStampData? Data { get; set; }
    public string? Message { get; set; }
    public string? MessageDetail { get; set; }
}

public class SwSapienStampData
{
    public string? Cfdi { get; set; }
    public string? CadenaOriginalSat { get; set; }
    public string? NoCertificadoSat { get; set; }
    public string? NoCertificadoCfdi { get; set; }
    public string? Uuid { get; set; }
    public string? FechaTimbrado { get; set; }
    public string? QrCode { get; set; }
    public string? SelloSat { get; set; }
    public string? SelloCfdi { get; set; }
}

public class SwSapienAuthRequest
{
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class SwSapienAuthResponse
{
    public string? Status { get; set; }
    public SwSapienAuthData? Data { get; set; }
    public string? Message { get; set; }
    public string? MessageDetail { get; set; }
}

public class SwSapienAuthData
{
    public string Token { get; set; } = string.Empty;
}

/// <summary>Wrapper for GET /management/v2/api/users/balance response.</summary>
public class SwSapienBalanceApiResponse
{
    public string? Status { get; set; }
    public string? Message { get; set; }
    public SwSapienStampBalanceData? Data { get; set; }
}

// ── Cancellation ─────────────────────────────────────────────────────────────

/// <summary>Request body for POST /cfdi33/cancel/csd/status</summary>
public class SwSapienCancelRequest
{
    public string Rfc { get; set; } = string.Empty;
    public string RfcReceptor { get; set; } = string.Empty;
    public string Total { get; set; } = string.Empty;
    public string Uuid { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
    public string? FolioSustitucion { get; set; }
    public string B64Cer { get; set; } = string.Empty;
    public string B64Key { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>Top-level response from SW Sapien cancellation endpoint.</summary>
public class SwSapienCancelResponse
{
    public string? Status { get; set; }
    public SwSapienCancelData? Data { get; set; }
    public string? Message { get; set; }
    public string? MessageDetail { get; set; }
}

public class SwSapienCancelData
{
    /// <summary>SAT-signed cancellation acknowledgment XML.</summary>
    public string? Acuse { get; set; }

    /// <summary>Dictionary of UUID → status code (e.g. "201", "202", "204").</summary>
    public Dictionary<string, string>? Uuid { get; set; }

    /// <summary>SAT status: "Cancelado", "Vigente".</summary>
    public string? StatusSat { get; set; }

    /// <summary>SAT status code with description.</summary>
    public string? StatusCodeSat { get; set; }

    /// <summary>"Cancelable sin aceptación", "Cancelable con aceptación", "No cancelable".</summary>
    public string? IsCancelable { get; set; }

    /// <summary>SAT/PAC cancellation workflow status.</summary>
    public string? StatusCancelation { get; set; }
}
