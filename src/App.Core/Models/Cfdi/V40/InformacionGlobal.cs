using System.Xml.Serialization;

namespace App.Core.Models.Cfdi.V40;

/// <summary>
/// CFDI 4.0 — Nodo InformacionGlobal requerido en facturas de público en general.
/// Periodicidad: 01=Diaria, 02=Semanal, 03=Quincenal, 04=Mensual.
/// </summary>
public class InformacionGlobal
{
    /// <summary>SAT periodicity code: 01=Daily, 02=Weekly, 03=Biweekly, 04=Monthly.</summary>
    [XmlAttribute("Periodicidad")]
    public string Periodicidad { get; set; } = string.Empty;

    /// <summary>Month(s) covered, zero-padded: "01"–"12".</summary>
    [XmlAttribute("Meses")]
    public string Meses { get; set; } = string.Empty;

    /// <summary>4-digit year of the period.</summary>
    [XmlAttribute("A\u00f1o")]
    public string Anio { get; set; } = string.Empty;
}
