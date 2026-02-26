using System.Xml.Serialization;

namespace App.Core.Models.Cfdi.V40;

[XmlRoot("Emisor", Namespace = "http://www.sat.gob.mx/cfd/4")]
public class Emisor
{
    [XmlAttribute("Rfc")]
    public string Rfc { get; set; } = string.Empty;

    [XmlAttribute("Nombre")]
    public string Nombre { get; set; } = string.Empty;

    [XmlAttribute("RegimenFiscal")]
    public string RegimenFiscal { get; set; } = string.Empty;
}
