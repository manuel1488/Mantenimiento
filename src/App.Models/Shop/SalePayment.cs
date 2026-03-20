using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Core.Base;
using App.Core.Enums.Shop;
using App.Models.Settings;

namespace App.Models.Shop;

[Table("sh_sale_payments")]
public class SalePayment : BaseEntity<long>
{
    public long SaleId { get; set; }

    public int PaymentMethodId { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Last four digits of the card, when applicable.
    /// </summary>
    [StringLength(4)]
    public string? CardLastFour { get; set; }

    /// <summary>
    /// Authorization/approval code returned by the card terminal.
    /// </summary>
    [StringLength(20)]
    public string? AuthorizationCode { get; set; }

    /// <summary>
    /// Card brand (Visa, Mastercard, Amex, etc.), when applicable.
    /// </summary>
    public CardBrand? CardBrand { get; set; }

    /// <summary>
    /// Transfer or check reference number, when applicable.
    /// </summary>
    [StringLength(100)]
    public string? Reference { get; set; }

    /// <summary>
    /// Amount the customer gave (cash payments only).
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal? CashTendered { get; set; }

    /// <summary>
    /// Change returned to the customer (cash payments only).
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal? CashChange { get; set; }

    [ForeignKey(nameof(SaleId))]
    public virtual Sale Sale { get; set; } = null!;

    [ForeignKey(nameof(PaymentMethodId))]
    public virtual PaymentMethod PaymentMethod { get; set; } = null!;
}
