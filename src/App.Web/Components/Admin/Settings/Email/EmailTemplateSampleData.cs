namespace App.Web.Components.Admin.Settings.Email;

internal static class EmailTemplateSampleData
{
    public static Dictionary<string, object> GetSampleData(string templateName, string language, string baseUrl = "") =>
        new()
        {
            { "culture", language },
            { "date_year", DateTime.UtcNow.Year.ToString() },
            { "app_name", "AppBase" },
            { "company_logo_url", string.IsNullOrEmpty(baseUrl) ? "" : baseUrl.TrimEnd('/') + "/images/logo.webp" }
        };
}
