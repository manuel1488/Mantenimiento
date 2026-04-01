namespace App.Core.Constants;

public static class InventoryMovementSubType
{
    // Subtypes for StockIn
     public const string NewProduct = "NEW_PRODUCT";        // Producto nuevo
    public const string SystemMigration = "SYSTEM_MIGRATION"; // Migración de sistema
    public const string InitialCount = "INITIAL_COUNT";    // Conteo inicial
    public const string OpeningBalance = "OPENING_BALANCE"; // Saldo inicial              
    
    // Subtypes for StockOut
    public const string CustomerOrder = "CUSTOMER_ORDER";
    public const string DirectSale = "DIRECT_SALE";
    public const string Remission = "REMISSION";
    public const string ProductionUse = "PRODUCTION_USE";
    public const string Consumption = "CONSUMPTION";
    public const string Damage = "DAMAGE";
    public const string Expiry = "EXPIRY";                 

    // Subtypes for Transfer
    public const string StandardTransfer = "STANDARD";              
    public const string RushTransfer = "RUSH";                     
    public const string Rebalancing = "REBALANCING";

    // Subtypes para Adjustment
    public const string PhysicalCount = "PHYSICAL_COUNT";    // Ajuste tras conteo físico
    public const string Shrinkage = "SHRINKAGE";            // Merma
    public const string Damaged = "DAMAGED";                // Productos dañados
    public const string Expired = "EXPIRED";                // Productos caducados
    public const string SystemAdjustment = "SYSTEM_ADJUSTMENT"; // Ajuste del sistema
}