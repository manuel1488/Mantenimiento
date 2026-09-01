namespace App.Core.Constants;

public static class ApplicationClaims
{
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

        // Configuración de Email
        public const string ViewEmailSettings = "Admin.EmailSettings.View";
        public const string ManageEmailSettings = "Admin.EmailSettings.Manage";

        // Configuración de Telegram
        public const string ViewTelegramSettings = "Admin.TelegramSettings.View";
        public const string ManageTelegramSettings = "Admin.TelegramSettings.Manage";

        // Auditoría
        public const string ViewAudit = "Admin.Audit.View";

        // Permisos
        public const string ViewPermissions = "Admin.Permissions.View";
        public const string ManagePermissions = "Admin.Permissions.Manage";

        // Catálogo de Claves de Unidad SAT (solo lectura, catálogo oficial CFDI)
        public const string ViewCatalogoSat = "Admin.CatalogoSat.View";
    }

    public static class Shared
    {
        public const string SharedAccess = "Shared.Access";

        // Dashboard
        public const string ViewDashboard = "Shared.Dashboard.View";

        // Servicios (catálogo)
        public const string ViewServicios = "Shared.Servicios.View";
        public const string ManageServicios = "Shared.Servicios.Manage";
        public const string DeleteServicios = "Shared.Servicios.Delete";

        // Unidades de Medida (catálogo)
        public const string ViewUnidadesMedida = "Shared.UnidadesMedida.View";
        public const string ManageUnidadesMedida = "Shared.UnidadesMedida.Manage";
        public const string DeleteUnidadesMedida = "Shared.UnidadesMedida.Delete";

        // Clientes
        public const string ViewClientes = "Shared.Clientes.View";
        public const string ManageClientes = "Shared.Clientes.Manage";
        public const string DeleteClientes = "Shared.Clientes.Delete";

        // Obras
        public const string ViewObras = "Shared.Obras.View";
        public const string ManageObras = "Shared.Obras.Manage";
        public const string DeleteObras = "Shared.Obras.Delete";

        // Cotizaciones
        public const string ViewCotizaciones = "Shared.Cotizaciones.View";
        public const string ManageCotizaciones = "Shared.Cotizaciones.Manage";
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
