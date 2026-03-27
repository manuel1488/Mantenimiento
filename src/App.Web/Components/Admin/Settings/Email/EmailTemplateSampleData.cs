using System.Text.RegularExpressions;

namespace App.Web.Components.Admin.Settings.Email;

internal static class EmailTemplateSampleData
{
    public static Dictionary<string, object> GetSampleData(string templateName, string language, string baseUrl = "") =>
        templateName switch
        {
            "invoice-cfdi" => new Dictionary<string, object>
            {
                { "culture", language },
                { "issuer_legal_name", "EMPRESA DEMO S.A. DE C.V." },
                { "issuer_rfc", "EDC900101ABC" },
                { "issuer_fiscal_regime", "601 - General de Ley Personas Morales" },
                { "issuer_postal_code", "64000" },
                { "folio", "F-0042" },
                { "issue_date", "2026-03-07T10:30:00" },
                { "uuid", "6BA9A4D7-37B4-4A3C-BFBA-123456789ABC" },
                { "payment_form", "03" },
                { "payment_form_description", "Transferencia electrónica de fondos" },
                { "payment_method", "PUE" },
                { "payment_method_description", "Pago en una sola exhibición" },
                { "currency", "MXN" },
                { "customer_legal_name", "CLIENTE EJEMPLO S.A." },
                { "customer_rfc", "CEJ800202XYZ" },
                { "customer_fiscal_regime", "601" },
                { "customer_fiscal_regime_description", "General de Ley Personas Morales" },
                { "customer_postal_code", "64010" },
                { "cfdi_use", "G03" },
                { "cfdi_use_description", "Gastos en general" },
                { "subtotal", "862.07" },
                { "tax_amount", "137.93" },
                { "total", "1000.00" },
                { "no_cert_cfdi", "20001000000300022815" },
                { "no_cert_sat", "20001000000300023708" },
                { "stamp_date", "2026-03-07T10:35:00" },
                { "has_pdf", true },
                { "date_year", "2026" },
                { "app_name", "Cleeny" },
                { "company_logo_url", string.IsNullOrEmpty(baseUrl) ? "" : baseUrl.TrimEnd('/') + "/images/logo.webp" },
                {
                    "items", new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            { "sat_code", "43211503" },
                            { "description", "Laptop Pro 15\" 16GB RAM" },
                            { "quantity", "1" },
                            { "unit_code", "H87" },
                            { "unit_name", "Pieza" },
                            { "tax_object", "02 - Sí objeto de impuesto" },
                            { "unit_price", "500.00" },
                            { "discount", "" },
                            { "has_discount", false },
                            { "amount", "500.00" }
                        },
                        new Dictionary<string, object>
                        {
                            { "sat_code", "43211507" },
                            { "description", "Mouse Inalámbrico" },
                            { "quantity", "2" },
                            { "unit_code", "H87" },
                            { "unit_name", "Pieza" },
                            { "tax_object", "02 - Sí objeto de impuesto" },
                            { "unit_price", "172.41" },
                            { "discount", "34.48" },
                            { "has_discount", true },
                            { "amount", "310.34" }
                        },
                        new Dictionary<string, object>
                        {
                            { "sat_code", "44111905" },
                            { "description", "Mochila para Laptop" },
                            { "quantity", "1" },
                            { "unit_code", "H87" },
                            { "unit_name", "Pieza" },
                            { "tax_object", "02 - Sí objeto de impuesto" },
                            { "unit_price", "51.73" },
                            { "discount", "" },
                            { "has_discount", false },
                            { "amount", "51.73" }
                        }
                    }
                }
            },
            _ => new Dictionary<string, object> { { "culture", language } }
        };
}
