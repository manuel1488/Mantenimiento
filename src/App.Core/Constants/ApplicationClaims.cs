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

        // Auditoría
        public const string ViewAudit = "Admin.Audit.View";

        // Permisos
        public const string ViewPermissions = "Admin.Permissions.View";
        public const string ManagePermissions = "Admin.Permissions.Manage";
    }

    public static class Shared
    {
        public const string SharedAccess = "Shared.Access";

        // Dashboard
        public const string ViewDashboard = "Shared.Dashboard.View";
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
