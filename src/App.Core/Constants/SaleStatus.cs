namespace App.Core.Constants;

/// <summary>
/// Constants for sale status values
/// </summary>
public static class SaleStatus
{
    /// <summary>
    /// Sale has been created and is active
    /// </summary>
    public const string Created = "Created";
    
    /// <summary>
    /// Sale has been cancelled and inventory returned
    /// </summary>
    public const string Cancelled = "Cancelled";
}