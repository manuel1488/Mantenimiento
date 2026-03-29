# Workflow Diagram Guide

Patrón para visualizar flujos de estados en cualquier módulo.
**Implementación de referencia**: `src/App.Web/Components/Shop/Quotations/QuotationWorkflowDialog.razor`

---

## Por qué SVG inline (no Z.Blazor.Diagrams)

Z.Blazor.Diagrams v3 no renderiza flechas dentro de `MudDialog` — su SVG layer no se inicializa en el portal de rendering del dialog. SVG inline en Blazor es 100% confiable sin dependencia de JS interop.

---

## Estructura de archivos

```
src/App.Web/Components/{Area}/{Module}/
  {Module}WorkflowDialog.razor

src/App.Web/Resources/Components/{Area}/{Module}/
  {Module}WorkflowDialog.en.resx
  {Module}WorkflowDialog.es.resx
```

---

## Integración en la página de listado

El botón de ayuda va **solo en el encabezado de la página**, nunca por fila.

```razor
<MudStack Row="true" AlignItems="AlignItems.Center" Spacing="1" Class="mb-2">
    <MudText Typo="Typo.h4">@L["Page Title"]</MudText>
    <MudTooltip Text="@L["View workflow"]">
        <MudIconButton Icon="@Icons.Material.Outlined.Help"
                       Size="Size.Small"
                       Color="Color.Default"
                       OnClick="@(() => OpenWorkflowDialog(null))" />
    </MudTooltip>
</MudStack>
```

```csharp
private async Task OpenWorkflowDialog(MyStatus? status)
{
    var parameters = new DialogParameters<{Module}WorkflowDialog>
    {
        { x => x.CurrentStatus, status }
    };
    var options = new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true };
    await DialogService.ShowAsync<{Module}WorkflowDialog>(L["{Module} Workflow"], parameters, options);
}
```

---

## Esqueleto del componente

```razor
@using System.Text
@inject IStringLocalizer<{Module}WorkflowDialog> L

<MudDialog>
    <DialogContent>
        <MudText Typo="Typo.body2" Class="mb-3" Color="Color.Secondary">
            @L["Workflow description"]
        </MudText>

        @if (CurrentStatus.HasValue)
        {
            <MudStack Row="true" AlignItems="AlignItems.Center" Spacing="1" Class="mb-3">
                <MudText Typo="Typo.body2">@L["Current status:"]</MudText>
                <MudChip T="string" Size="MudBlazor.Size.Small" Color="@GetMudColor(CurrentStatus.Value)">
                    @L[CurrentStatus.Value.ToString()]
                </MudChip>
            </MudStack>
        }

        <div style="width:100%;overflow-x:auto;">
            <svg viewBox="0 0 520 415" xmlns="http://www.w3.org/2000/svg"
                 style="width:100%;max-width:520px;display:block;margin:auto;font-family:Roboto,sans-serif;">
                <defs>
                    <marker id="wf-arrow" markerWidth="9" markerHeight="7" refX="8" refY="3.5" orient="auto">
                        <polygon points="0 0, 9 3.5, 0 7" fill="#BDBDBD" />
                    </marker>
                </defs>

                <!-- ❗ Flechas ANTES que los nodos — los nodos se renderizan encima -->
                <line x1="260" y1="53" x2="260" y2="105"
                      stroke="#BDBDBD" stroke-width="1.5" marker-end="url(#wf-arrow)" />
                <path d="M x1,y1 C cx1,cy1 cx2,cy2 x2,y2"
                      stroke="#BDBDBD" stroke-width="1.5" fill="none" marker-end="url(#wf-arrow)" />

                <!-- Etiquetas de flechas (font-size 9, color #9E9E9E) -->
                <text x="268" y="79" font-size="9" fill="#9E9E9E">@L["Transition label"]</text>

                <!-- Nodos de estado: pill (rx = height/2) -->
                @DrawNode(MyStatus.Draft, "#9E9E9E", 190, 17, 140, 36)

                <!-- Nodos outcome (Sale, Remision): rect redondeado (rx=8) -->
                @DrawOutcome(L["Sale"].Value, "#1565C0", 80, 325, 120, 36)
            </svg>
        </div>

        <MudDivider Class="my-3" />

        <MudText Typo="Typo.caption" Color="Color.Secondary" Class="mb-1">
            <strong>@L["Legend"]</strong>
        </MudText>
        <MudStack Row="true" Spacing="2" Wrap="Wrap.Wrap">
            @foreach (var (color, key) in _legendItems)
            {
                <MudStack Row="true" AlignItems="AlignItems.Center" Spacing="1">
                    <div style="width:12px;height:12px;border-radius:50%;background:@color;flex-shrink:0;"></div>
                    <MudText Typo="Typo.caption">@L[key]</MudText>
                </MudStack>
            }
        </MudStack>
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="@(() => MudDialog.Close())">@L["Close"]</MudButton>
    </DialogActions>
</MudDialog>

@code {
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = null!;
    [Parameter] public MyStatus? CurrentStatus { get; set; }

    private static readonly (string Color, string Key)[] _legendItems = [ /* ... */ ];

    private MarkupString DrawNode(MyStatus status, string color, double x, double y, double w, double h)
    {
        bool isCurrent = CurrentStatus == status;
        string label = L[status.ToString()].Value;
        double rx = h / 2, cx = x + w / 2, cy = y + h / 2;
        var sb = new StringBuilder("<g>");
        if (isCurrent)
        {
            sb.Append($"<rect x='{x-4}' y='{y-4}' width='{w+8}' height='{h+8}' rx='{rx+4}' fill='none' stroke='{color}' stroke-width='3' opacity='0.45'/>");
            sb.Append($"<rect x='{x-2}' y='{y-2}' width='{w+4}' height='{h+4}' rx='{rx+2}' fill='none' stroke='{color}' stroke-width='1.5' opacity='0.7'/>");
        }
        sb.Append($"<rect x='{x}' y='{y}' width='{w}' height='{h}' rx='{rx}' fill='{color}'/>");
        sb.Append($"<text x='{cx}' y='{cy}' text-anchor='middle' dominant-baseline='middle' fill='white' font-size='13' font-weight='{(isCurrent ? "700" : "500")}'>{System.Net.WebUtility.HtmlEncode(label)}</text>");
        sb.Append("</g>");
        return new MarkupString(sb.ToString());
    }

    private MarkupString DrawOutcome(string label, string color, double x, double y, double w, double h)
    {
        double cx = x + w / 2, cy = y + h / 2;
        var sb = new StringBuilder("<g>");
        sb.Append($"<rect x='{x}' y='{y}' width='{w}' height='{h}' rx='8' fill='{color}'/>");
        sb.Append($"<text x='{cx}' y='{cy}' text-anchor='middle' dominant-baseline='middle' fill='white' font-size='13' font-weight='500'>{System.Net.WebUtility.HtmlEncode(label)}</text>");
        sb.Append("</g>");
        return new MarkupString(sb.ToString());
    }
}
```

---

## Guía de coordenadas SVG

| Concepto | Valor |
|---|---|
| ViewBox | `0 0 520 N` — N ≈ 90px por fila + 50px margen |
| Nodo estándar | `width=140, height=36, rx=18` (pill) |
| Nodo outcome | `width=120–130, height=36, rx=8` |
| Puerto bottom | `(x + w/2, y + h)` |
| Puerto top | `(x + w/2, y)` |

**Línea recta:**
```svg
<line x1="cx" y1="bottom_source" x2="cx" y2="top_target" stroke="#BDBDBD" stroke-width="1.5" marker-end="url(#wf-arrow)" />
```

**Curva diagonal (bezier cúbico):**
```svg
<path d="M x1,y1 C cx1,cy1 cx2,cy2 x2,y2" stroke="#BDBDBD" stroke-width="1.5" fill="none" marker-end="url(#wf-arrow)" />
```
Control points típicos: misma Y que el origen para salir horizontal, luego descienden hacia el destino.

---

## Colores estándar

| Estado    | SVG      | MudBlazor     |
|-----------|----------|---------------|
| Draft     | #9E9E9E  | Color.Default |
| Pending   | #2196F3  | Color.Info    |
| Accepted  | #4CAF50  | Color.Success |
| Rejected  | #F44336  | Color.Error   |
| Expired   | #FF9800  | Color.Warning |
| Sale      | #1565C0  | —             |
| Remission | #E65100  | —             |

---

## Módulos candidatos

| Módulo | Enum | Transiciones |
|---|---|---|
| Remisiones | `RemissionStatus` | Pending → Consolidated / Cancelled |
| Facturas CFDI | — | Draft → Stamped → Cancelled |
