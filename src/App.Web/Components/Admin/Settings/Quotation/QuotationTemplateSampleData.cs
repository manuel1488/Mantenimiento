namespace App.Web.Components.Admin.Settings.Quotation;

internal static class QuotationTemplateSampleData
{
    public static Dictionary<string, object> GetSampleData() => new()
    {
        // ── Document ──────────────────────────────────────────────────────────
        { "quotation_number",   "COT-2026-0042" },
        { "quote_date",         "16/04/2026" },
        { "valid_until",        "30/04/2026" },

        // ── Company ───────────────────────────────────────────────────────────
        { "company_name", "Empresa Demo S.A. de C.V." },
        { "has_logo",     false },
        { "logo_base64",  string.Empty },

        // ── Customer (commercial) ─────────────────────────────────────────────
        { "customer_name",          "Ferretería El Clavo" },
        { "has_contact_name",       true },
        { "customer_contact_name",  "Juan Pérez García" },
        { "show_legal_name",        true },
        { "customer_legal_name",    "HERRAMIENTAS Y MATERIALES DEL NORTE S.A. DE C.V." },
        { "has_customer_email",     true },
        { "customer_email",         "facturacion@distribuidoraperez.com" },
        { "has_customer_phone",     true },
        { "customer_phone",         "+52 81 1234 5678" },
        { "has_customer_address",   true },
        { "customer_address",       "Av. Constitución 1234, Col. Centro, Monterrey, N.L." },

        // ── Customer (fiscal) ─────────────────────────────────────────────────
        { "customer_has_fiscal_data",   true },
        { "customer_tax_id",            "PEGJ800202ABC" },
        { "has_fiscal_regime",          true },
        { "customer_fiscal_regime",     "601 – General de Ley Personas Morales" },

        // ── Line items ────────────────────────────────────────────────────────
        { "has_discounts", true },
        { "details", new List<object>
            {
                new Dictionary<string, object>
                {
                    { "index",          1 },
                    { "product_code",   "PROD-001" },
                    { "product_name",   "Laptop Pro 15\" 16 GB RAM" },
                    { "quantity",       "2" },
                    { "unit_price",     "$8,500.00" },
                    { "discount_amount","$850.00" },
                    { "tax_amount",     "$2,584.00" },
                    { "total",          "$18,234.00" }
                },
                new Dictionary<string, object>
                {
                    { "index",          2 },
                    { "product_code",   "PROD-002" },
                    { "product_name",   "Mouse Inalámbrico Ergonómico" },
                    { "quantity",       "5" },
                    { "unit_price",     "$450.00" },
                    { "discount_amount","" },
                    { "tax_amount",     "$360.00" },
                    { "total",          "$2,610.00" }
                },
                new Dictionary<string, object>
                {
                    { "index",          3 },
                    { "product_code",   "PROD-003" },
                    { "product_name",   "Mochila para Laptop 15\"" },
                    { "quantity",       "2" },
                    { "unit_price",     "$750.00" },
                    { "discount_amount","" },
                    { "tax_amount",     "$240.00" },
                    { "total",          "$1,740.00" }
                }
            }
        },

        // ── Totals ────────────────────────────────────────────────────────────
        { "subtotal",       "$19,224.14" },
        { "has_discount",   true },
        { "discount_amount","$850.00" },
        { "tax_amount",     "$3,184.00" },
        { "total",          "$22,584.00" },

        // ── Notes ─────────────────────────────────────────────────────────────
        { "has_notes", true },
        { "notes",     "Los precios no incluyen gastos de envío. Entrega estimada de 5 a 7 días hábiles tras confirmación de pedido." },

        // ── Payment Terms ─────────────────────────────────────────────────────
        { "has_payment_terms",  true },
        { "payment_terms_text", "50% de anticipo al confirmar pedido. Saldo al momento de entrega." },

        // ── Bank / Wire Transfer ──────────────────────────────────────────────
        { "show_bank_details",      true },
        { "has_bank_beneficiary",   true },
        { "bank_beneficiary",       "Empresa Demo S.A. de C.V." },
        { "has_bank_rfc",           true },
        { "bank_rfc",               "EDC900101ABC" },
        { "has_bank_name",          true },
        { "bank_name",              "BBVA México" },
        { "has_bank_account_number",true },
        { "bank_account_number",    "0123456789" },
        { "has_bank_clabe",         true },
        { "bank_clabe_number",      "012345678901234567" },
        { "has_bank_swift",         false },
        { "bank_swift",             string.Empty },

        // ── Contact / Social Media ────────────────────────────────────────────
        { "show_contact_info",    true },
        { "has_contact_website",  true },
        { "contact_website",      "www.empresademo.com.mx" },
        { "has_contact_phone",    true },
        { "contact_phone",        "+52 81 8888 0000" },
        { "has_contact_email",    true },
        { "contact_email",        "ventas@empresademo.com.mx" },
        { "has_contact_whatsapp", true },
        { "contact_whatsapp",     "+52 81 9999 1234" },
        { "has_contact_facebook", false },
        { "contact_facebook",     string.Empty },
        { "has_contact_instagram",false },
        { "contact_instagram",    string.Empty },

        // ── Localized labels (Spanish) ────────────────────────────────────────
        { "label_quotation",             "Cotización" },
        { "label_date",                  "Fecha" },
        { "label_valid_until",           "Válida hasta" },
        { "label_quotation_number",      "No. Cotización" },
        { "label_customer",              "Cliente" },
        { "label_fiscal_data",           "Datos fiscales" },
        { "label_tax_id",                "RFC" },
        { "label_fiscal_regime",         "Régimen fiscal" },
        { "label_code",                  "Código" },
        { "label_product",               "Producto" },
        { "label_qty",                   "Cant." },
        { "label_unit_price",            "Precio unitario" },
        { "label_discount",              "Descuento" },
        { "label_tax",                   "IVA" },
        { "label_total",                 "Total" },
        { "label_subtotal",              "Subtotal" },
        { "label_notes_conditions",      "Notas y condiciones" },
        { "label_payment_terms",         "Condiciones de pago" },
        { "label_wire_transfer_details", "Datos para transferencia" },
        { "label_beneficiary",           "Beneficiario" },
        { "label_rfc",                   "RFC" },
        { "label_bank",                  "Banco" },
        { "label_account_number",        "No. de cuenta" },
        { "label_end_of_document",       "Fin del documento" },
        { "label_valid_until_footer",    "Esta cotización es válida hasta" }
    };
}
