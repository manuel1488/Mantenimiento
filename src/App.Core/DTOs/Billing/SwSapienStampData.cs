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
