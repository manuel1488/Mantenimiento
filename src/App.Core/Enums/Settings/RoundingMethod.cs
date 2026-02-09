namespace App.Core.Enums.Settings;

/// <summary>
/// Defines the rounding method to apply to sale totals
/// </summary>
public enum RoundingMethod
{
    /// <summary>
    /// Always round up (ceiling) - Default
    /// </summary>
    Ceiling = 1,

    /// <summary>
    /// Always round down (floor)
    /// </summary>
    Floor = 2,

    /// <summary>
    /// Round to nearest (standard mathematical rounding)
    /// </summary>
    Nearest = 3
}
