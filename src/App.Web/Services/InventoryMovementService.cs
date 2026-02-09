using App.Core.Constants;
using Microsoft.Extensions.Localization;
using MudBlazor;

namespace App.Web.Services;

public class InventoryMovementService
{
    private readonly IStringLocalizer<InventoryMovementService> L;

    public InventoryMovementService(IStringLocalizer<InventoryMovementService> localizer)
    {
        L = localizer;
    }

    public string GetMovementTypeDisplay(string movementType) => movementType switch
    {
        InventoryMovementType.InitialLoad => L["InitialLoad"],
        InventoryMovementType.Purchase => L["Purchase"],
        InventoryMovementType.Sale => L["Sale"],
        InventoryMovementType.Transfer => L["Transfer"],
        InventoryMovementType.Adjustment => L["Adjustment"],
        InventoryMovementType.Return => L["Return"],
        InventoryMovementType.StockIn => L["StockIn"],
        InventoryMovementType.StockOut => L["StockOut"],
        InventoryMovementType.ReturnToSupplier => L["ReturnToSupplier"],
        _ => movementType
    };

    public string GetMovementSubTypeDisplay(string movementSubType) => movementSubType switch
    {
        // Existing transfer subtypes
        InventoryMovementSubType.StandardTransfer => L["SubType_Standard"],
        InventoryMovementSubType.RushTransfer => L["SubType_Rush"],
        InventoryMovementSubType.Rebalancing => L["SubType_Rebalancing"],
        
        // Existing StockIn subtypes
        InventoryMovementSubType.NewProduct => L["SubType_NewProduct"],
        InventoryMovementSubType.SystemMigration => L["SubType_SystemMigration"],
        InventoryMovementSubType.InitialCount => L["SubType_InitialCount"],
        InventoryMovementSubType.OpeningBalance => L["SubType_OpeningBalance"],
        
        // Existing StockOut subtypes
        InventoryMovementSubType.CustomerOrder => L["SubType_CustomerOrder"],
        InventoryMovementSubType.DirectSale => L["SubType_DirectSale"],
        InventoryMovementSubType.ProductionUse => L["SubType_ProductionUse"],
        InventoryMovementSubType.Consumption => L["SubType_Consumption"],
        InventoryMovementSubType.Damage => L["SubType_Damage"],
        InventoryMovementSubType.Expiry => L["SubType_Expiry"],
        
        // Existing Adjustment subtypes
        InventoryMovementSubType.PhysicalCount => L["SubType_PhysicalCount"],
        InventoryMovementSubType.Shrinkage => L["SubType_Shrinkage"],
        InventoryMovementSubType.Damaged => L["SubType_Damaged"],
        InventoryMovementSubType.Expired => L["SubType_Expired"],
        InventoryMovementSubType.SystemAdjustment => L["SubType_SystemAdjustment"],
        
        _ => movementSubType
    };

    public Color GetMovementTypeColor(string movementType) => movementType switch
    {
        InventoryMovementType.InitialLoad => Color.Info,
        InventoryMovementType.Purchase => Color.Success,
        InventoryMovementType.Sale => Color.Error,
        InventoryMovementType.Transfer => Color.Warning,
        InventoryMovementType.Adjustment => Color.Default,
        InventoryMovementType.Return => Color.Secondary,
        InventoryMovementType.StockIn => Color.Success,
        InventoryMovementType.StockOut => Color.Error,
        InventoryMovementType.ReturnToSupplier => Color.Warning,
        _ => Color.Default
    };
    
    // Método adicional para obtener colores específicos para los subtipos de ajuste
    public Color GetAdjustmentSubTypeColor(string adjustmentSubType) => adjustmentSubType switch
    {
        InventoryMovementSubType.PhysicalCount => Color.Info,
        InventoryMovementSubType.Shrinkage => Color.Error,
        InventoryMovementSubType.Damaged => Color.Warning,
        InventoryMovementSubType.Expired => Color.Warning,
        InventoryMovementSubType.SystemAdjustment => Color.Secondary,
        _ => Color.Default
    };
}