using System.Xml;
using System.Xml.Serialization;

namespace App.Core.Models.Cfdi.V40;

/// <summary>
/// CFDI 4.0 root element — Ingreso type only, MXN currency.
/// </summary>
[XmlRoot("Comprobante", Namespace = "http://www.sat.gob.mx/cfd/4")]
public class Comprobante
{
    [XmlAttribute("schemaLocation", Namespace = "http://www.w3.org/2001/XMLSchema-instance")]
    public string SchemaLocation { get; set; } =
        "http://www.sat.gob.mx/cfd/4 http://www.sat.gob.mx/sitio_internet/cfd/4/cfdv40.xsd";

    [XmlAttribute("Version")]
    public string Version { get; set; } = "4.0";

    [XmlAttribute("Serie")]
    public string? Serie { get; set; }

    [XmlAttribute("Folio")]
    public string Folio { get; set; } = string.Empty;

    /// <summary>YYYY-MM-DDTHH:MM:SS local time (America/Mexico_City)</summary>
    [XmlAttribute("Fecha")]
    public string Fecha { get; set; } = string.Empty;

    /// <summary>Set by signing process (CSD).</summary>
    [XmlAttribute("Sello")]
    public string Sello { get; set; } = string.Empty;

    [XmlAttribute("FormaPago")]
    public string FormaPago { get; set; } = string.Empty;

    /// <summary>Set by signing process (CSD serial).</summary>
    [XmlAttribute("NoCertificado")]
    public string NoCertificado { get; set; } = string.Empty;

    /// <summary>Set by signing process (Base64 certificate).</summary>
    [XmlAttribute("Certificado")]
    public string Certificado { get; set; } = string.Empty;

    [XmlIgnore]
    public decimal SubTotal { get; set; }

    [XmlAttribute("SubTotal")]
    public string SubTotalString
    {
        get => CfdiFormatHelper.FormatAmount(SubTotal);
        set => SubTotal = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    [XmlIgnore]
    public decimal Descuento { get; set; }

    [XmlAttribute("Descuento")]
    public string DescuentoString
    {
        get => CfdiFormatHelper.FormatAmount(Descuento);
        set => Descuento = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    [XmlIgnore]
    public bool DescuentoStringSpecified => Descuento > 0;

    [XmlAttribute("Moneda")]
    public string Moneda { get; set; } = "MXN";

    [XmlIgnore]
    public decimal Total { get; set; }

    [XmlAttribute("Total")]
    public string TotalString
    {
        get => CfdiFormatHelper.FormatAmount(Total);
        set => Total = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>Always "I" (Ingreso) for this system.</summary>
    [XmlAttribute("TipoDeComprobante")]
    public string TipoDeComprobante { get; set; } = "I";

    /// <summary>"01" = No aplica exportación.</summary>
    [XmlAttribute("Exportacion")]
    public string Exportacion { get; set; } = "01";

    [XmlAttribute("MetodoPago")]
    public string MetodoPago { get; set; } = string.Empty;

    /// <summary>Postal code of the issuer (LugarExpedicion).</summary>
    [XmlAttribute("LugarExpedicion")]
    public string LugarExpedicion { get; set; } = string.Empty;

    [XmlElement("Emisor")]
    public Emisor Emisor { get; set; } = new();

    [XmlElement("Receptor")]
    public Receptor Receptor { get; set; } = new();

    [XmlArray("Conceptos")]
    [XmlArrayItem("Concepto")]
    public List<Concepto> Conceptos { get; set; } = new();

    [XmlElement("Impuestos")]
    public Impuestos? Impuestos { get; set; }

    /// <summary>Complemento — populated by PAC with TimbreFiscalDigital after stamping.</summary>
    [XmlElement("Complemento")]
    public ComplementoContainer? Complemento { get; set; }

    public bool ShouldSerializeComplemento() => Complemento?.Items != null && Complemento.Items.Count > 0;
}

public class ComplementoContainer
{
    [XmlAnyElement]
    public List<XmlElement> Items { get; set; } = new();
}
