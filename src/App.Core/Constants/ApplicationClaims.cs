namespace App.Core.Constants;

public static class ApplicationClaims
{
    public static class Shop
    {
        public const string ShopAccess = "Shop.Access";
        // Inventory
        public const string ViewInventory = "Shop.Inventory.View";
        public const string ManageInventory = "Shop.Inventory.Manage";
        public const string ExportInventory = "Shop.Inventory.Export";

        // Inventoy History
        public const string ViewInventoryHistory = "Shop.InventoryHistory.View";

        // Inventory Transfers
        public const string ViewInventoryTransfers = "Shop.InventoryTransfers.View";
        public const string ManageInventoryTransfers = "Shop.InventoryTransfers.Manage";

        // Inventory Inputs
        public const string ViewInventoryInputs = "Shop.InventoryInputs.View";
        public const string ManageInventoryInputs = "Shop.InventoryInputs.Manage";

        // Inventory Adjustments
        public const string ViewInventoryAdjustments = "Shop.InventoryAdjustments.View";
        public const string ManageInventoryAdjustments = "Shop.InventoryAdjustments.Manage";

        // Inventory Alerts
        public const string ViewInventoryAlerts = "Shop.InventoryAlerts.View";
        public const string ReceiveInventoryAlertEmails = "Shop.InventoryAlerts.ReceiveEmails";

        // Products
        public const string ViewProducts = "Shop.Products.View";
        public const string ManageProducts = "Shop.Products.Manage";
        public const string DeleteProducts = "Shop.Products.Delete";
        public const string BulkImportProducts = "Shop.Products.BulkImport";

        // Precios y Descuentos
        public const string ViewPrices = "Shop.Prices.View";
        public const string ManagePrices = "Shop.Prices.Manage";
        public const string ManageDiscounts = "Shop.Discounts.Manage";
        public const string AuthorizeDiscounts = "Shop.Discounts.Authorize";

        // Ventas
        public const string ViewSales = "Shop.Sales.View";
        public const string ViewHistorySales = "Shop.Sales.ViewHistory";
        public const string CreateSale = "Shop.Sales.Create";
        public const string CancelSale = "Shop.Sales.Cancel";
        public const string ViewSalesReport = "Shop.Sales.ViewReports";
        public const string ViewDailySalesSummary = "Shop.Sales.ViewDailySummary";
        public const string ExportDailySalesSummary = "Shop.Sales.ExportDailySummary";

        // Facturación
        public const string CreateInvoice = "Shop.Invoice.Create";
        public const string ViewInvoice = "Shop.Invoice.View";
        public const string CancelInvoice = "Shop.Invoice.Cancel";
        
        // Almacenes
        public const string ViewWarehouses = "Shop.Warehouses.View";
        public const string ManageWarehouses = "Shop.Warehouses.Manage";
        public const string DeleteWarehouses = "Shop.Warehouses.Delete";

        // Caja (Cash Register)
        public const string ViewCashRegister = "Shop.CashRegister.View";
        public const string ManageCashRegister = "Shop.CashRegister.Manage";
        public const string WithdrawCashRegister = "Shop.CashRegister.Withdraw";
        public const string ViewCashRegisterReport = "Shop.CashRegister.ViewReport";
    }

    public static class Admin
    {
        public const string AdminAccess = "Admin.Access";
        // Usuarios
        public const string ViewUsers = "Admin.Users.View";
        public const string ManageUsers = "Admin.Users.Manage";
        public const string DeleteUsers = "Admin.Users.Delete";
        public const string ResetPassword = "Admin.Users.ResetPassword";

        // Roles
        public const string ViewRoles = "Admin.Roles.View";
        public const string ManageRoles = "Admin.Roles.Manage";
        public const string DeleteRoles = "Admin.Roles.Delete";

        // Configuración
        public const string ViewSettings = "Admin.Settings.View";
        public const string ManageSettings = "Admin.Settings.Manage";
        
        // Configuración Fiscal
        public const string ViewFiscalSettings = "Admin.FiscalSettings.View";
        public const string ManageFiscalSettings = "Admin.FiscalSettings.Manage";

        // Warehouse Settings
        public const string ViewWarehouseSettings = "Admin.WarehouseSettings.View";
        public const string ManageWarehouseSettings = "Admin.WarehouseSettings.Manage";

        // Branch Settings
        public const string ViewBranchSettings = "Admin.BranchSettings.View";
        public const string ManageBranchSettings = "Admin.BranchSettings.Manage";
        
        // Configuración de Email
        public const string ViewEmailSettings = "Admin.EmailSettings.View";
        public const string ManageEmailSettings = "Admin.EmailSettings.Manage";
        
        // Tasas de Impuesto
        public const string ViewTaxRates = "Admin.TaxRates.View";
        public const string ManageTaxRates = "Admin.TaxRates.Manage";
        public const string DeleteTaxRates = "Admin.TaxRates.Delete";
        
        // Unidades de Medida
        public const string ViewUnitMeasures = "Admin.UnitMeasures.View";
        public const string ManageUnitMeasures = "Admin.UnitMeasures.Manage";
        
        // Configuración Inicial
        public const string ManageInitialSetup = "Admin.InitialSetup.Manage";

        // Auditoría
        public const string ViewAudit = "Admin.Audit.View";
        public const string ViewAuditReports = "Admin.Audit.ViewReports";
        
        // Permisos
        public const string ViewPermissions = "Admin.Permissions.View";
        public const string ManagePermissions = "Admin.Permissions.Manage";

        //Discccounts
        public const string ViewDisccounts = "Admin.Disccounts.View";
        public const string ManageDisccounts = "Admin.Disccounts.Manage";

        // Settings for Tickets
        public const string ViewTicketSettings = "Admin.TicketSettings.View";
        public const string ManageTicketSettings = "Admin.TicketSettings.Manage";

        // Cajeros (Cashier Profiles)
        public const string ViewCashiers = "Admin.Cashiers.View";
        public const string ManageCashiers = "Admin.Cashiers.Manage";

        // Cash Stations
        public const string ViewCashStations = "Admin.CashStations.View";
        public const string ManageCashStations = "Admin.CashStations.Manage";
    }

    public static class Shared
    {
        public const string SharedAccess = "Shared.Access";
        // Clientes
        public const string ViewCustomers = "Shared.Customers.View";
        public const string ManageCustomers = "Shared.Customers.Manage";
        public const string DeleteCustomers = "Shared.Customers.Delete";

        // Reportes Generales
        public const string ViewReports = "Shared.Reports.View";
        public const string GenerateReports = "Shared.Reports.Generate";
    }

    public static IEnumerable<string> GetAllClaims()
    {
        return typeof(ApplicationClaims)
            .GetNestedTypes()
            .SelectMany(t => t.GetFields())
            .Where(f => f.IsLiteral && !f.IsInitOnly)
            .Select(f => (string)f.GetValue(null)!)
            .ToList();
    }
}