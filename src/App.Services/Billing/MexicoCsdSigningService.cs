using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using App.Core.Common;
using App.Core.Interfaces.Billing;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;

namespace App.Services.Billing;

/// <summary>
/// Signs CFDI XML with the CSD certificate.
/// Adds NoCertificado, Certificado, and Sello attributes to the Comprobante element.
/// </summary>
public class MexicoCsdSigningService : IMexicoCsdSigningService
{
    private readonly IMexicoCfdiXmlService _xmlService;
    private readonly ILogger<MexicoCsdSigningService> _logger;

    public MexicoCsdSigningService(
        IMexicoCfdiXmlService xmlService,
        ILogger<MexicoCsdSigningService> logger)
    {
        _xmlService = xmlService;
        _logger = logger;
    }

    public async Task<Result<string>> SignXmlAsync(
        string unsignedXml,
        byte[] certificateBytes,
        byte[] privateKeyBytes,
        string privateKeyPassword)
    {
        try
        {
            _logger.LogInformation("Starting CFDI XML signing process");

            // Load certificate
            var cert = new X509Certificate2(certificateBytes);

            // Validate certificate
            var validResult = ValidateCertificate(certificateBytes);
            if (!validResult.IsSuccess) return Result<string>.Failure(validResult.Error!);
            if (!validResult.Value) return Result<string>.Failure("El certificado CSD está vencido o no es válido aún");

            // Get NoCertificado (SAT serial)
            var serialResult = GetCertificateSerialNumber(cert);
            if (!serialResult.IsSuccess) return Result<string>.Failure(serialResult.Error!);

            // Get Certificado (Base64)
            var certBase64 = Convert.ToBase64String(certificateBytes);

            // Load private key
            var keyResult = LoadPrivateKey(privateKeyBytes, privateKeyPassword);
            if (!keyResult.IsSuccess) return Result<string>.Failure(keyResult.Error!);
            var privateKeyData = keyResult.Value!;

            // Add NoCertificado and Certificado to XML
            var xmlWithCert = AddCertificateAttributes(unsignedXml, serialResult.Value!, certBase64);
            if (!xmlWithCert.IsSuccess) return Result<string>.Failure(xmlWithCert.Error!);

            // Generate cadena original via XSLT
            var chainResult = await _xmlService.GenerateOriginalChainAsync(xmlWithCert.Value!);
            if (!chainResult.IsSuccess) return Result<string>.Failure(chainResult.Error!);

            // Sign cadena original with RSA-SHA256
            var signResult = SignData(chainResult.Value!, privateKeyData);
            if (!signResult.IsSuccess) return Result<string>.Failure(signResult.Error!);

            // Add Sello to XML
            var signedXml = AddSignatureToXml(xmlWithCert.Value!, signResult.Value!);
            if (!signedXml.IsSuccess) return Result<string>.Failure(signedXml.Error!);

            _logger.LogInformation("CFDI XML signed successfully");
            return signedXml;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error signing CFDI XML");
            return Result<string>.Failure($"Error al firmar el CFDI: {ex.Message}");
        }
    }

    public Result<bool> ValidateCertificate(byte[] certificateBytes)
    {
        try
        {
            var cert = new X509Certificate2(certificateBytes);
            var now = DateTime.UtcNow;
            if (cert.NotBefore > now) return Result<bool>.Success(false);
            if (cert.NotAfter < now) return Result<bool>.Success(false);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating certificate");
            return Result<bool>.Failure($"Error al validar certificado: {ex.Message}");
        }
    }

    #region Private helpers

    private Result<string> GetCertificateSerialNumber(X509Certificate2 cert)
    {
        try
        {
            var parser = new X509CertificateParser();
            var bcCert = parser.ReadCertificate(cert.RawData);
            var serialBytes = bcCert.SerialNumber.ToByteArray();

            // SAT encodes serial as ASCII digits in hex bytes
            try
            {
                var ascii = Encoding.ASCII.GetString(serialBytes);
                if (ascii.Length == 20 && ascii.All(char.IsDigit))
                    return Result<string>.Success(ascii);
            }
            catch { /* try numeric fallback */ }

            var serial = bcCert.SerialNumber.ToString();
            if (serial.Length < 20) serial = serial.PadLeft(20, '0');
            else if (serial.Length > 20) serial = serial[^20..];

            return Result<string>.Success(serial);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting certificate serial number");
            return Result<string>.Failure($"Error al obtener número de certificado: {ex.Message}");
        }
    }

    private Result<byte[]> LoadPrivateKey(byte[] privateKeyBytes, string password)
    {
        try
        {
            AsymmetricKeyParameter privateKey;

            try
            {
                // DER PKCS#8 encrypted (SAT standard)
                var asn1 = Asn1Object.FromByteArray(privateKeyBytes);
                var encryptedKeyInfo = EncryptedPrivateKeyInfo.GetInstance(asn1);
                var decryptedKeyInfo = PrivateKeyInfoFactory.CreatePrivateKeyInfo(
                    password.ToCharArray(), encryptedKeyInfo);
                privateKey = PrivateKeyFactory.CreateKey(decryptedKeyInfo);
            }
            catch
            {
                // PEM fallback
                using var reader = new StreamReader(new MemoryStream(privateKeyBytes));
                var pemReader = new Org.BouncyCastle.OpenSsl.PemReader(
                    reader, new PasswordFinder(password));
                var obj = pemReader.ReadObject();
                privateKey = obj is AsymmetricCipherKeyPair pair ? pair.Private : (AsymmetricKeyParameter)obj;
            }

            if (privateKey is not RsaPrivateCrtKeyParameters rsaKey)
                return Result<byte[]>.Failure("El tipo de llave no es RSA");

            var rsaParams = DotNetUtilities.ToRSAParameters(rsaKey);
            using var rsa = RSA.Create();
            rsa.ImportParameters(rsaParams);
            return Result<byte[]>.Success(rsa.ExportRSAPrivateKey());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading private key");
            return Result<byte[]>.Failure($"Error al cargar la llave privada: {ex.Message}");
        }
    }

    private Result<string> SignData(string data, byte[] privateKey)
    {
        try
        {
            using var rsa = RSA.Create();
            rsa.ImportRSAPrivateKey(privateKey, out _);
            var bytes = Encoding.UTF8.GetBytes(data);
            var sig = rsa.SignData(bytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return Result<string>.Success(Convert.ToBase64String(sig));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error signing data");
            return Result<string>.Failure($"Error al firmar: {ex.Message}");
        }
    }

    private Result<string> AddCertificateAttributes(string xml, string noCertificado, string certificado)
    {
        try
        {
            var doc = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
            doc.Root!.SetAttributeValue("NoCertificado", noCertificado);
            doc.Root!.SetAttributeValue("Certificado", certificado);
            return Result<string>.Success(XmlToString(doc));
        }
        catch (Exception ex)
        {
            return Result<string>.Failure($"Error al agregar atributos de certificado: {ex.Message}");
        }
    }

    private Result<string> AddSignatureToXml(string xml, string sello)
    {
        try
        {
            var doc = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
            doc.Root!.SetAttributeValue("Sello", sello);
            return Result<string>.Success(XmlToString(doc));
        }
        catch (Exception ex)
        {
            return Result<string>.Failure($"Error al agregar firma: {ex.Message}");
        }
    }

    private static string XmlToString(XDocument doc)
    {
        var settings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false),
            Indent = false,
            OmitXmlDeclaration = false
        };
        using var ms = new MemoryStream();
        using (var w = XmlWriter.Create(ms, settings)) doc.Save(w);
        return Encoding.UTF8.GetString(ms.ToArray());
    }

    private sealed class PasswordFinder : Org.BouncyCastle.OpenSsl.IPasswordFinder
    {
        private readonly string _pw;
        public PasswordFinder(string pw) => _pw = pw;
        public char[] GetPassword() => _pw.ToCharArray();
    }

    #endregion
}
