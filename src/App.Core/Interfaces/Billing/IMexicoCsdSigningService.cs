using App.Core.Common;

namespace App.Core.Interfaces.Billing;

public interface IMexicoCsdSigningService
{
    /// <summary>
    /// Signs a CFDI XML string with the CSD certificate and private key.
    /// Adds NoCertificado, Certificado, and Sello attributes to the Comprobante element.
    /// </summary>
    Task<Result<string>> SignXmlAsync(
        string unsignedXml,
        byte[] certificateBytes,
        byte[] privateKeyBytes,
        string privateKeyPassword);

    /// <summary>Validates that the certificate bytes represent a valid CSD (not expired).</summary>
    Result<bool> ValidateCertificate(byte[] certificateBytes);
}
