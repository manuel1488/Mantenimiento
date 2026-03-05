using Microsoft.Extensions.Localization;

namespace App.Web.Services;

public class PermissionTranslationService
{
    private readonly IStringLocalizer<PermissionTranslationService> L;

    public PermissionTranslationService(IStringLocalizer<PermissionTranslationService> localizer)
    {
        L = localizer;
    }

    public string GetModuleDisplayName(string moduleName) => moduleName switch
    {
        "Shop" => L["Module.Shop"],
        "Admin" => L["Module.Admin"],
        "Shared" => L["Module.Shared"],
        "Labels" => L["Module.Labels"],
        _ => moduleName
    };

    public string GetPermissionDisplayName(string permission)
    {
        // Extraer el último segmento del permiso (ej: "Shop.Products.View" -> "View")
        var parts = permission.Split('.');
        if (parts.Length < 2)
            return permission;

        var module = parts[0];
        var feature = parts[1];
        var action = parts.Length > 2 ? parts[2] : "";

        // Handling special "Access" permissions (Shop.Access, etc.)
        if (feature == "Access")
            return L["Permission.Access"];

        // Handle specific permission cases
        if (module == "Shop" && feature == "Sales" && action == "ViewHistory")
            return L["Permission.ViewHistory"];
            
        return action switch
        {
            "View" => L["Permission.View"],
            "Manage" => L["Permission.Manage"],
            "Delete" => L["Permission.Delete"],
            "Export" => L["Permission.Export"],
            "Create" => L["Permission.Create"],
            "Cancel" => L["Permission.Cancel"],
            "Generate" => L["Permission.Generate"],
            "Access" => L["Permission.Access"],
            "Close" => L["Permission.Close"],
            "Print" => L["Permission.Print"],
            "ViewReports" => L["Permission.ViewReports"],
            "Authorize" => L["Permission.Authorize"],
            "ViewHistory" => L["Permission.ViewHistory"],
            "ViewDailySummary" => L["Permission.ViewDailySummary"],
            "ExportDailySummary" => L["Permission.ExportDailySummary"],
            "ReceiveAlerts" => L["Permission.ReceiveAlerts"],
            "ReceiveEmails" => L["Permission.ReceiveEmails"],
            "Withdraw" => L["Permission.Withdraw"],
            "ViewReport" => L["Permission.ViewReport"],
            "BulkImport" => L["Permission.BulkImport"],
            "ResetPassword" => L["Permission.ResetPassword"],
            _ => L[$"Permission.{action}"]
        };
    }

    public string GetFeatureDisplayName(string permission)
    {
        // Extraer el segmento del medio (ej: "Shop.Products.View" -> "Products")
        var parts = permission.Split('.');
        if (parts.Length < 2)
            return permission;

        var feature = parts[1];

        return feature switch
        {
            "Access" => L["Feature.ModuleAccess"],
            "Products" => L["Feature.Products"],
            "Inventory" => L["Feature.Inventory"],
            "InventoryHistory" => L["Feature.InventoryHistory"],
            "InventoryTransfers" => L["Feature.InventoryTransfers"],
            "InventoryInputs" => L["Feature.InventoryInputs"],
            "InventoryAdjustments" => L["Feature.InventoryAdjustments"],
            "InventoryAlerts" => L["Feature.InventoryAlerts"],
            "InventoryAlertsReceiveEmails" => L["Feature.InventoryAlerts"],
            "Prices" => L["Feature.Prices"],
            "Discounts" or "Disccounts" => L["Feature.Discounts"],
            "Sales" => L["Feature.Sales"],
            "Invoice" => L["Feature.Invoice"],
            "StampBalance" => L["Feature.StampBalance"],
            "Warehouses" => L["Feature.Warehouses"],
            "CashRegister" => L["Feature.CashRegister"],
            "Users" => L["Feature.Users"],
            "Roles" => L["Feature.Roles"],
            "Settings" => L["Feature.Settings"],
            "FiscalSettings" => L["Feature.FiscalSettings"],
            "BillingSettings" => L["Feature.BillingSettings"],
            "WarehouseSettings" => L["Feature.WarehouseSettings"],
            "BranchSettings" => L["Feature.BranchSettings"],
            "EmailSettings" => L["Feature.EmailSettings"],
            "TaxRates" => L["Feature.TaxRates"],
            "UnitMeasures" => L["Feature.UnitMeasures"],
            "InitialSetup" => L["Feature.InitialSetup"],
            "Audit" => L["Feature.Audit"],
            "Permissions" => L["Feature.Permissions"],
            "TicketSettings" => L["Feature.TicketSettings"],
            "Cashiers" => L["Feature.Cashiers"],
            "CashStations" => L["Feature.CashStations"],
            "Customers" => L["Feature.Customers"],
            "Suppliers" => L["Feature.Suppliers"],
            "Reports" => L["Feature.Reports"],
            _ => L[$"Feature.{feature}"]
        };
    }

    public string GetFullPermissionDisplayName(string permission)
    {
        var parts = permission.Split('.');
        if (parts.Length < 2)
            return permission;

        var module = parts[0];

        // Special handling for module access permissions like "Shop.Access"
        if (parts.Length == 2 && parts[1] == "Access")
            return string.Format(L["Permission.ModuleAccess"], GetModuleDisplayName(module));

        // Labels and similar 2-part non-access permissions like "Labels.View", "Labels.Print"
        if (parts.Length == 2)
            return $"{GetModuleDisplayName(module)} - {GetPermissionDisplayName(permission)}";

        var feature = GetFeatureDisplayName(permission);
        var action = GetPermissionDisplayName(permission);

        return $"{feature} - {action}";
    }
}