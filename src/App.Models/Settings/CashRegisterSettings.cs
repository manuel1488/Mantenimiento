using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Core.Base;

namespace App.Models.Settings;

/// <summary>
/// Singleton settings row for cash register configuration (Id = 1 always).
/// </summary>
[Table("stg_cash_register_settings")]
public class CashRegisterSettings : BaseEntity<int>
{
    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal MaxWithdrawalAmount { get; set; } = 1000;

    [Column(TypeName = "decimal(10,2)")]
    public decimal? DefaultInitialFund { get; set; }

    public bool IsStrictCashLimit { get; set; } = false;
}
