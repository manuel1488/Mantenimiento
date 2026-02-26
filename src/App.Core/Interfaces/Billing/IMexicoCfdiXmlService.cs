using App.Core.Common;
using App.Core.Models.Cfdi.V40;

namespace App.Core.Interfaces.Billing;

public interface IMexicoCfdiXmlService
{
    /// <summary>Generates the CFDI 4.0 XML from a Comprobante object.</summary>
    Task<Result<string>> GenerateXmlAsync(Comprobante comprobante);

    /// <summary>Generates the cadena original string using SAT XSLT.</summary>
    Task<Result<string>> GenerateOriginalChainAsync(string cfdiXml);

    /// <summary>Validates the XML against SAT XSD schemas. Returns list of errors (empty = valid).</summary>
    Task<Result<List<string>>> ValidateXmlAsync(string cfdiXml);
}
