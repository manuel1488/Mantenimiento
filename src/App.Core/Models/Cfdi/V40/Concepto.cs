using System.Xml.Serialization;

namespace App.Core.Models.Cfdi.V40;

/// <summary>CFDI 4.0 line item (product or service).</summary>
public class Concepto
{
    [XmlAttribute("ClaveProdServ")]
    public string ClaveProdServ { get; set; } = string.Empty;

    [XmlAttribute("NoIdentificacion")]
    public string? NoIdentificacion { get; set; }

    [XmlIgnore]
    public decimal Cantidad { get; set; }

    [XmlAttribute("Cantidad")]
    public string CantidadString
    {
        get => CfdiFormatHelper.FormatQuantity(Cantidad);
        set => Cantidad = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    [XmlAttribute("ClaveUnidad")]
    public string ClaveUnidad { get; set; } = string.Empty;

    [XmlAttribute("Unidad")]
    public string? Unidad { get; set; }

    [XmlAttribute("Descripcion")]
    public string Descripcion { get; set; } = string.Empty;

    [XmlIgnore]
    public decimal ValorUnitario { get; set; }

    [XmlAttribute("ValorUnitario")]
    public string ValorUnitarioString
    {
        get => CfdiFormatHelper.FormatUnitPrice(ValorUnitario);
        set => ValorUnitario = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    [XmlIgnore]
    public decimal Importe { get; set; }

    [XmlAttribute("Importe")]
    public string ImporteString
    {
        get => CfdiFormatHelper.FormatAmount(Importe);
        set => Importe = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
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

    /// <summary>01=No objeto, 02=Sí objeto, 03=Sí objeto pero no desglosado</summary>
    [XmlAttribute("ObjetoImp")]
    public string ObjetoImp { get; set; } = "02";

    [XmlElement("Impuestos")]
    public ConceptoImpuestos? Impuestos { get; set; }
}
