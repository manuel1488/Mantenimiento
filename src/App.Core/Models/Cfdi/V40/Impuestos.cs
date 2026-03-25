using System.Xml.Serialization;

namespace App.Core.Models.Cfdi.V40;

/// <summary>Tax summary at Comprobante level.</summary>
public class Impuestos
{
    [XmlArray("Traslados")]
    [XmlArrayItem("Traslado")]
    public List<Traslado>? Traslados { get; set; }

    [XmlIgnore]
    public decimal TotalImpuestosTrasladados { get; set; }

    [XmlAttribute("TotalImpuestosTrasladados")]
    public string TotalImpuestosTrasladadosString
    {
        get => CfdiFormatHelper.FormatAmount(TotalImpuestosTrasladados);
        set => TotalImpuestosTrasladados = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    [XmlIgnore]
    public bool TotalImpuestosTrasladadosStringSpecified => Traslados != null && Traslados.Any();
}

public class Traslado
{
    [XmlIgnore]
    public decimal Base { get; set; }

    [XmlAttribute("Base")]
    public string BaseString
    {
        get => CfdiFormatHelper.FormatAmount(Base);
        set => Base = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>002 = IVA</summary>
    [XmlAttribute("Impuesto")]
    public string Impuesto { get; set; } = "002";

    /// <summary>Tasa | Cuota | Exento</summary>
    [XmlAttribute("TipoFactor")]
    public string TipoFactor { get; set; } = "Tasa";

    [XmlIgnore]
    public decimal TasaOCuota { get; set; }

    [XmlAttribute("TasaOCuota")]
    public string TasaOCuotaString
    {
        get => CfdiFormatHelper.FormatRate(TasaOCuota);
        set => TasaOCuota = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    [XmlIgnore]
    public bool TasaOCuotaStringSpecified => TipoFactor?.ToLower() != "exento";

    [XmlIgnore]
    public decimal Importe { get; set; }

    [XmlAttribute("Importe")]
    public string ImporteString
    {
        get => CfdiFormatHelper.FormatAmount(Importe);
        set => Importe = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    [XmlIgnore]
    public bool ImporteStringSpecified => TipoFactor?.ToLower() != "exento";
}

/// <summary>Taxes at Concepto level. Traslados MUST be declared before Retenciones per SAT XSD.</summary>
public class ConceptoImpuestos
{
    [XmlArray("Traslados")]
    [XmlArrayItem("Traslado")]
    public List<ConceptoTraslado>? Traslados { get; set; }
}

public class ConceptoTraslado
{
    [XmlIgnore]
    public decimal Base { get; set; }

    [XmlAttribute("Base")]
    public string BaseString
    {
        get => CfdiFormatHelper.FormatLineAmount(Base);
        set => Base = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>002 = IVA</summary>
    [XmlAttribute("Impuesto")]
    public string Impuesto { get; set; } = "002";

    [XmlAttribute("TipoFactor")]
    public string TipoFactor { get; set; } = "Tasa";

    [XmlIgnore]
    public decimal TasaOCuota { get; set; }

    [XmlAttribute("TasaOCuota")]
    public string TasaOCuotaString
    {
        get => CfdiFormatHelper.FormatRate(TasaOCuota);
        set => TasaOCuota = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    [XmlIgnore]
    public bool TasaOCuotaStringSpecified => TipoFactor?.ToLower() != "exento";

    [XmlIgnore]
    public decimal Importe { get; set; }

    [XmlAttribute("Importe")]
    public string ImporteString
    {
        get => CfdiFormatHelper.FormatLineAmount(Importe);
        set => Importe = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    [XmlIgnore]
    public bool ImporteStringSpecified => TipoFactor?.ToLower() != "exento";
}
