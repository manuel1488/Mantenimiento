namespace App.Core.Enums.Billing;

/// <summary>
/// Determines how the CFDI FormaPago is resolved when a sale has multiple payment methods.
/// </summary>
public enum MultiPaymentFormPolicy
{
    /// <summary>Use the CFDI form code of the payment method with the highest amount.
    /// When there is a tie, the method with the lowest SortOrder wins.</summary>
    UseHighestAmount = 0,

    /// <summary>Always use code "99" (Por definir).</summary>
    UseUndefined99 = 1
}
