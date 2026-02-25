namespace App.Core.DTOs.Shop;

public class CashRegisterSettingsDto
{
    public int Id { get; set; }
    public decimal? MaxWithdrawalAmount { get; set; }
    public bool IsStrictWithdrawalLimit { get; set; }
    public decimal? MaxCashLimit { get; set; }
    public bool IsStrictCashLimit { get; set; }
    public decimal? DefaultInitialFund { get; set; }
}

public class UpdateCashRegisterSettingsDto
{
    public decimal? MaxWithdrawalAmount { get; set; }
    public bool IsStrictWithdrawalLimit { get; set; }
    public decimal? MaxCashLimit { get; set; }
    public bool IsStrictCashLimit { get; set; }
    public decimal? DefaultInitialFund { get; set; }
}
