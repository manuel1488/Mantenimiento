# List / Detail / Form Page Pattern

Reference implementation: **Cotizaciones** (`src/App.Web/Components/Admin/Cotizaciones/`). Use it as the base design whenever building a new business-domain resource (Obras, Clientes, Servicios, and anything added later already loosely follow parts of this — Cotizaciones is the most complete/current expression of the pattern and should be the one new screens copy). Update this guide whenever the pattern gains a new element (a new icon convention, a new responsive trick, a new shared component) so it keeps growing with the codebase instead of drifting out of sync.

## The three pages

Every CRUD-capable resource gets three routed pages, not dialogs, once it has more than a couple of fields or needs its own sharing/PDF/approval actions:

| Page | Route | Purpose |
|---|---|---|
| **List** | `/gestion/{recurso}` | `MudDataGrid`, search box, "Nuevo {Recurso}" button, per-row actions |
| **Detail** | `/gestion/{recurso}/{id}` | Read-only view + contextual actions (approve/reject, share, etc.) |
| **Form** | `/gestion/{recurso}/nueva` and `/gestion/{recurso}/{id}/editar` | One shared component for both create and edit |

Reserve `MudDialog` for short, self-contained interactions (confirmations, a single email-address prompt like `SendCotizacionEmailDialog`) — not for viewing or editing the resource itself. Editing a multi-field resource inside a modal makes it hard to give it room to breathe and doesn't deep-link; a page does both for free.

**Form page shares one component for create and edit.** `CotizacionFormPage.razor` maps both `@page "/gestion/cotizaciones/nueva"` and `@page "/gestion/cotizaciones/{Id:int}/editar"` to the same component; `Id is null` distinguishes create from edit. Don't build a separate create dialog and a separate edit page — one form, two routes.

## Container widths — keep them consistent within a resource

- List pages: `MaxWidth.ExtraLarge` (a grid wants the room).
- Detail and Form pages: `MaxWidth.Large` — **not** `MaxWidth.Medium`. This was a real bug: Cotización's detail/form pages originally shipped at `Medium` and looked cramped next to Obra's detail page at `Large`. When adding a new resource, match the width of the most similar existing resource (Obras is the reference for `Large`) rather than picking one that "feels right" for a first draft.

## Icon conventions (be consistent across every resource)

| Action | Icon | Notes |
|---|---|---|
| View detail (row → detail page) | `Icons.Material.Filled.Visibility` | The 👁 eye is *always* "go look at the read-only page", never "open a PDF" |
| View PDF (opens in a new tab) | `Icons.Material.Filled.PictureAsPdf` | Rendered as a real `<a target="_blank">` via `MudIconButton Href="..." Target="_blank"`, not `NavigationManager.NavigateTo` in an `OnClick` — a plain hyperlink isn't blocked by Safari's popup blocker on iPad/iPhone the way a JS-triggered `window.open` can be |
| Download a file | `Icons.Material.Filled.Download` | Same `Href` pattern, no `?inline=true` query param so the server sends `Content-Disposition: attachment` |
| Edit | `Icons.Material.Filled.Edit` | Only shown when the resource is in an editable state (e.g. `Estado == Pendiente`) |
| Delete | `Icons.Material.Filled.Delete`, `Color="Color.Error"` | Only shown when deletion is actually allowed; always behind `ConfirmDialog` |
| Send by email | `Icons.Material.Filled.Email` | See "Sharing a PDF" below |
| Share via WhatsApp / native share sheet | `Icons.Material.Filled.Share` | See "Sharing a PDF" below |
| Overflow / "more actions" | `Icons.Material.Filled.MoreVert` inside a `MudMenu` | See "Actions column" below |

Never reuse the eye icon for "view PDF" — that conflates two different actions (see detail page below) and was corrected mid-build once already; don't reintroduce it.

## Actions column (list page) — keep only 2 actions inline, the rest in a menu

A `MudDataGrid` row on a phone has very little horizontal room. The rule: show at most **View** + **Edit** (when applicable) as inline `MudIconButton`s, and push everything else (Download, Delete, and any future action) into a `MudMenu`:

```razor
<TemplateColumn Title="@L["Actions"]" Sortable="false" Filterable="false"
                HeaderStyle="width: 190px;" CellClass="d-flex justify-end">
    <CellTemplate>
        <MudIconButton Icon="@Icons.Material.Filled.Visibility" Size="Size.Medium"
                       OnClick="@(() => NavigationManager.NavigateTo($"/gestion/cotizaciones/{context.Item.Id}"))"
                       title="@L["View Cotización"]" />
        @if (context.Item.Estado == CotizacionEstado.Pendiente)
        {
            <AuthorizeView Policy="@ApplicationClaims.Shared.ManageCotizaciones">
                <Authorized Context="authContext">
                    <MudIconButton Icon="@Icons.Material.Filled.Edit" Size="Size.Medium"
                                   OnClick="@(() => NavigationManager.NavigateTo($"/gestion/cotizaciones/{context.Item.Id}/editar"))"
                                   title="@L["Edit Cotización"]" />
                </Authorized>
            </AuthorizeView>
        }
        <MudMenu Icon="@Icons.Material.Filled.MoreVert" Size="Size.Medium"
                 AnchorOrigin="Origin.BottomRight" TransformOrigin="Origin.TopRight">
            <MudMenuItem Href="@($"/api/cotizaciones/{context.Item.Id}/pdf")" Icon="@Icons.Material.Filled.Download">
                @L["Download PDF"]
            </MudMenuItem>
            @* Delete etc. go here too, gated by the same claim/state checks as inline actions *@
        </MudMenu>
    </CellTemplate>
</TemplateColumn>
```

Two things that make this actually work on a phone:
1. **Give the Actions column a fixed `HeaderStyle="width: ...px;"`.** Without one, `MudDataGrid` stretches the last unconstrained column to fill leftover space, leaving a huge empty gap before the icons sit flush right — this happened on first pass and had to be fixed with `HeaderStyle="width: 190px;"`. Size the width to the actual icon count for that resource.
2. **Use `Size="Size.Medium"` on list-row icon buttons, `Size.Large` on detail-page action buttons.** `Size.Small` measures under the ~44px touch-target minimum and was the original mistake here — corrected across both pages.

## Sharing a PDF (email + native share)

This is a full mini-pattern in itself, built once for Cotizaciones and meant to be reused verbatim for any future PDF-producing resource:

- **PDF endpoint** (`Controllers/{Resource}PdfController.cs`): one `GET /api/{recurso}/{id}/pdf` action with an `inline` query flag —
  ```csharp
  [HttpGet("{id:int}/pdf")]
  public async Task<IActionResult> GetPdf([FromRoute] int id, [FromQuery] bool inline = false)
  {
      var result = await _service.GetPdfAsync(id);
      if (!result.IsSuccess) return NotFound(result.Error);

      var fileName = $"{recurso}-{id}.pdf";
      if (inline)
      {
          Response.Headers.ContentDisposition = $"inline; filename=\"{fileName}\"";
          return File(result.Value!, "application/pdf");
      }
      return File(result.Value!, "application/pdf", fileName);
  }
  ```
  `?inline=true` → renders in the browser tab (View PDF). No query param → downloads (Download PDF). One endpoint, two behaviors — don't build separate endpoints for view vs. download.

- **PDF generation itself** goes through the already-shared `IPdfService` (PuppeteerSharp) + Scriban HTML/CSS templating pattern — see `sat-clave-prodserv-classification-guide.md`'s sibling doc on templates if one exists, or `CotizacionService.GetCotizacionPdfAsync` as the concrete example: template body/CSS come from a DB-overridable settings row (`ICotizacionTemplateSettingsService`) with an on-disk default fallback, branding (logo/colors) comes from `ICompanySettingsService` falling back to `BrandingOptions` (`Branding/{profile}.json`).

- **Email**: a real MIME attachment via the existing `IEmailService`/`EmailMessage`/`EmailAttachment` — generate the PDF bytes server-side and attach them, never just email a link. `SendCotizacionEmailAsync` in the service is the template; the UI side is a tiny `MudDialog` (`SendCotizacionEmailDialog.razor`) that only collects the recipient address (pre-filled from the resource's own contact email when available) and returns it via `DialogResult`.

- **WhatsApp / native share**: there is **no WhatsApp Business API integration** in this codebase and none should be added for this purpose — it uses the browser's native **Web Share API** (`navigator.share` with a `File`), wired through `IWhatsAppShareService`/`WhatsAppShareService.cs` (C# → JS interop) and `window.shareFile` in `wwwroot/js/app.js`. On iOS/iPadOS Safari and Android Chrome this opens the OS share sheet and the file attaches for real if the user picks WhatsApp (or Mail, or anything else installed). On desktop browsers that don't support `navigator.canShare({files})`, it falls back to opening `https://wa.me/?text=...` with only a pre-filled text message and no attachment — always surface that fallback to the user via a snackbar (`This browser can't attach the file directly — ...`) rather than silently degrading.

## Detail page anatomy

Modeled on `ObraDetallePage.razor` and `CotizacionDetallePage.razor`:

1. **Back link row**: small `MudIconButton` `ArrowBack` + a `mud-text-secondary` breadcrumb-style label back to the list page.
2. **Header card** (`MudPaper Class="da-card pa-4 pa-sm-6 mb-4" Elevation="2"`): title (resource's primary display name), a secondary line (date or subtitle), status `MudChip`(s), and the row of action buttons (PDF/Download/Email/Share/Edit) — `Wrap="Wrap.Wrap"` on the containing `MudStack` so they drop to a second line on narrow screens instead of overflowing.
3. **`CamposDetalleGrid`** (`src/App.Web/Components/Shared/CamposDetalleGrid.razor`) immediately below a `MudDivider` inside the header card — the shared "field card grid" component (icon + label + value tiles, `xs=12 sm=6 md=4`). Feed it from a `List<CampoDetalle>` builder method (`CamposCotizacion()`, `CamposObra()` — same naming convention: `Campos{Resource}()`). **Reuse this component for any new resource's header fields instead of hand-rolling another field grid** — it's already generic (originally lifted out of Cliente's page specifically so it could be shared).
4. **Body sections** as their own `MudPaper Class="da-card pa-2 pa-sm-4 mb-4" Elevation="2"` blocks — one per logical grouping (e.g. "Líneas", "Actividades", "Approval"). Don't cram everything into the header card.
5. **Contextual action sections** (approve/reject, etc.) only rendered when the resource's state allows the action, and gated behind the relevant `Manage*` claim via `AuthorizeView` — never just hidden by CSS.

## Responsive tables → stacked cards on mobile

A `MudSimpleTable`/`MudDataGrid` with 4+ columns and any long text column (service/product names) will force horizontal scroll and wrapped headers on a phone — this happened on Cotización's línea table and was fixed with a dual-render pattern: keep the table for `sm`+ and swap to one stacked `MudPaper` card per row below `sm`, both driven from the same collection:

```razor
<MudSimpleTable Dense="true" Hover="true" Class="d-none d-sm-block">
    @* full table markup *@
</MudSimpleTable>

<div class="d-sm-none">
    @foreach (var linea in _cotizacion.Lineas)
    {
        <MudPaper Class="pa-3 mb-2" Outlined="true">
            <MudText Typo="Typo.subtitle2" Class="mb-1">@linea.ServicioNombre</MudText>
            <MudStack Row="true" Justify="Justify.SpaceBetween">
                <MudText Typo="Typo.body2" Class="mud-text-secondary">@L["Quantity"]</MudText>
                <MudText Typo="Typo.body2">@linea.Cantidad.ToString("F2") @linea.UnidadMedida</MudText>
            </MudStack>
            @* one label/value MudStack row per column, the last one bold for the money total *@
        </MudPaper>
    }
</div>
```

Use plain Bootstrap-style display-breakpoint classes (`d-none d-sm-block` / `d-sm-none`) — no JS, no `MudHidden` component needed, and it matches how the rest of this codebase already toggles responsive visibility. Apply this to **any** multi-column table showing user-entered text of unpredictable length (product/service names, addresses) — short, fixed-format columns (dates, small numeric totals) are fine to leave as a plain table that just gets dense on mobile.

## State/status color + label helpers

Every resource with a workflow enum (`CotizacionEstado`, `ObraEstado`, `ActividadEstado`, ...) gets two small static/instance helper methods on its list page, reused by the detail page too:

```csharp
public static Color EstadoColor(CotizacionEstado estado) => estado switch
{
    CotizacionEstado.Pendiente => Color.Warning,
    CotizacionEstado.Aprobada => Color.Success,
    CotizacionEstado.Rechazada => Color.Error,
    _ => Color.Default
};

private string EstadoLabel(CotizacionEstado estado) => estado switch
{
    CotizacionEstado.Pendiente => L["Pending"],
    CotizacionEstado.Aprobada => L["Approved"],
    CotizacionEstado.Rechazada => L["Rejected"],
    _ => estado.ToString()
};
```

Keep the color mapping consistent app-wide: Warning = pending/needs-attention, Success = approved/finished/good, Error = rejected/cancelled, Default = anything else. `EstadoColor` is made `public static` on the list page specifically so other pages (detail pages, dialogs) can call `ObrasPage.EstadoColor(...)` instead of redefining the mapping — follow that when adding a new one.

## Buttons vs. icon buttons

- Primary page-level actions with a text label ("Nueva Cotización", "Guardar", "Aprobar"/"Rechazar") → `MudButton`, `Class="da-button-rounded"`, `Variant="Variant.Filled"` for the primary/affirmative action and `Variant="Variant.Outlined"` for secondary ones (Edit, Cancel).
- Row-level or icon-only actions (view/PDF/download/edit/delete/share) → `MudIconButton` with a `title="@L[...]"` tooltip — every icon button needs a localized `title`, no exceptions, since it has no visible label.

## Growing this guide

When the next resource (or the next revision of an existing one) introduces something genuinely new to the pattern — a new shared action type, a new responsive technique, a different kind of approval flow — add it here as its own subsection rather than starting a parallel guide. If a change contradicts something already written here (like the `Medium`→`Large` container-width fix), update the existing text in place and keep a one-line note of *why*, the way the icon and sizing notes above do — that's what keeps this guide trustworthy as the codebase grows instead of turning into stale aspirational documentation.
