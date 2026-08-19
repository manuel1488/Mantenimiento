namespace App.Web.Components.Admin.Settings.Cotizacion;

/// <summary>Fake data used only by the Preview button in <see cref="CotizacionSettingsTab"/>.</summary>
public static class CotizacionTemplateSampleData
{
    public static object GetSampleData() => new
    {
        company_name = "Mantenimiento Demo S.A. de C.V.",
        logo_base64 = string.Empty,
        has_logo = false,
        primary_color = "#1A6868",
        secondary_color = "#7B3FA0",
        cotizacion_id = 1,
        fecha_generacion = DateTime.Now.ToString("dd/MM/yyyy"),
        cliente_nombre = "Juan Pérez",
        total = (15000m).ToString("C2"),
        label_quotation = "Cotización",
        label_client = "Cliente",
        label_service = "Servicio",
        label_quantity = "Cantidad",
        label_unit_price = "Precio Unitario",
        label_subtotal = "Subtotal",
        label_total = "Total",
        lineas = new[]
        {
            new
            {
                servicio_nombre = "Impermeabilizante",
                unidad_medida = "Metro Cuadrado",
                cantidad = (150m).ToString("F2"),
                precio_unitario = (60m).ToString("C2"),
                subtotal = (9000m).ToString("C2")
            },
            new
            {
                servicio_nombre = "Pintura",
                unidad_medida = "Metro Cuadrado",
                cantidad = (200m).ToString("F2"),
                precio_unitario = (30m).ToString("C2"),
                subtotal = (6000m).ToString("C2")
            }
        }
    };
}
