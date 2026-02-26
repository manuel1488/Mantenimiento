using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Billing.Mexico;

public class CreateMexicoInvoiceDto
{
    [Required]
    public long SaleId { get; set; }

    /// <summary>c_UsoCFDI catalog code (e.g. G01, G03, S01)</summary>
    [Required]
    [StringLength(5)]
    public string CfdiUse { get; set; } = string.Empty;

    /// <summary>c_FormaPago catalog code (e.g. 01=Efectivo, 03=Transferencia, 04=Tarjeta)</summary>
    [Required]
    [StringLength(5)]
    public string PaymentForm { get; set; } = string.Empty;

    /// <summary>c_MetodoPago: PUE = Pago en una sola exhibición, PPD = Pago en parcialidades</summary>
    [Required]
    [StringLength(5)]
    public string PaymentMethod { get; set; } = "PUE";

    // Override customer fiscal data (pre-filled from customer but editable)
    [Required]
    [StringLength(20)]
    public string CustomerRfc { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string CustomerLegalName { get; set; } = string.Empty;

    [Required]
    [StringLength(10)]
    public string CustomerPostalCode { get; set; } = string.Empty;

    [Required]
    [StringLength(5)]
    public string CustomerFiscalRegime { get; set; } = string.Empty;

    /// <summary>Optional email to send the invoice to. If null, no email is sent.</summary>
    public string? SendToEmail { get; set; }
}
