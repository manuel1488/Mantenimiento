using System.Xml.Serialization;

namespace App.Core.Models.Cfdi.V40;

[XmlRoot("Receptor", Namespace = "http://www.sat.gob.mx/cfd/4")]
public class Receptor
{
    [XmlAttribute("Rfc")]
    public string Rfc { get; set; } = string.Empty;

    [XmlAttribute("Nombre")]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// DomicilioFiscalReceptor - Fiscal postal code (required in CFDI 4.0)
    /// </summary>
    [XmlAttribute("DomicilioFiscalReceptor")]
    public string DomicilioFiscalReceptor { get; set; } = string.Empty;

    [XmlAttribute("RegimenFiscalReceptor")]
    public string RegimenFiscalReceptor { get; set; } = string.Empty;

    [XmlAttribute("UsoCFDI")]
    public string UsoCFDI { get; set; } = string.Empty;
}
