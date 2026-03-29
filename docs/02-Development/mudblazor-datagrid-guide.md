# MudDataGrid vs MudTable — Column Width Control Guide

## Why MudDataGrid over MudTable

**Always prefer `MudDataGrid` over `MudTable` for data listing pages.** MudTable uses flexbox layout which makes column width control unreliable — `ColGroup`, `Style="width:"` on `MudTh`, and percentage widths are all ignored or behave inconsistently. MudDataGrid renders as a real HTML `<table>` and respects column widths properly.

## Column Width with MudDataGrid

Use `HeaderStyle` on columns to set widths. Columns without `HeaderStyle` expand to fill remaining space:

```razor
<MudDataGrid T="ItemDto"
             ServerData="@LoadServerData"
             Dense="true"
             Hover="true"
             Striped="true"
             ColumnResizeMode="ResizeMode.Container"
             RowsPerPage="10"
             Filterable="false"
             SortMode="SortMode.None">
    <Columns>
        <PropertyColumn Property="x => x.Id" Title="ID" HeaderStyle="width: 50px;" />
        <PropertyColumn Property="x => x.Date" Title="Date" HeaderStyle="width: 100px;" />
        <PropertyColumn Property="x => x.Name" Title="Name" />  @* No width = expands *@
        <TemplateColumn Title="Amount" HeaderStyle="text-align: right; width: 90px;"
                        CellStyle="text-align: right; white-space: nowrap;">
            <CellTemplate>
                @context.Item.Amount.ToString("C")
            </CellTemplate>
        </TemplateColumn>
        <TemplateColumn Title="Actions" Sortable="false" Filterable="false"
                        CellClass="d-flex justify-end">
            <CellTemplate>
                <MudButtonGroup Variant="Variant.Text" Size="Size.Small">
                    <!-- action buttons -->
                </MudButtonGroup>
            </CellTemplate>
        </TemplateColumn>
    </Columns>
    <PagerContent>
        <MudDataGridPager T="ItemDto" />
    </PagerContent>
</MudDataGrid>
```

## Key Properties

| Property | Level | Description |
|----------|-------|-------------|
| `ColumnResizeMode="ResizeMode.Container"` | DataGrid | Enables drag-to-resize columns; grid stays within container width |
| `ColumnResizeMode="ResizeMode.Column"` | DataGrid | Enables drag-to-resize; grid width can expand beyond container |
| `HeaderStyle="width: 90px;"` | Column | Sets initial column width |
| `CellStyle="white-space: nowrap;"` | Column | Prevents cell content from wrapping (good for numbers/currency) |
| `CellClass="d-flex justify-end"` | Column | Right-aligns action buttons using flex |
| `Resizable` | Column | Per-column override to allow/disallow resizing |

## ServerData Migration: MudTable to MudDataGrid

| MudTable | MudDataGrid |
|----------|-------------|
| `MudTable<T>` ref | `MudDataGrid<T>` ref |
| `ServerData="@LoadServerData"` | Same |
| `TableState state` parameter | `GridState<T> state` parameter |
| `TableData<T>` return type | `GridData<T>` return type |
| `CancellationToken` 2nd param | Not needed |
| `<MudTablePager />` | `<MudDataGridPager T="..." />` |
| `<MudTh>` / `<MudTd>` | `<PropertyColumn>` / `<TemplateColumn>` |
| `_table.ReloadServerData()` | `_dataGrid.ReloadServerData()` |

## Common Patterns

### Numeric/Currency Columns
- Use `CellStyle="text-align: right; white-space: nowrap;"` for currency/number columns
- Use `HeaderStyle="text-align: right; width: 90px;"` to right-align header and set compact width
- Columns without explicit width will absorb remaining space (ideal for text like Customer, Name, Description)

### Action Columns
```razor
<TemplateColumn Title="Actions" Sortable="false" Filterable="false" CellClass="d-flex justify-end">
    <CellTemplate>
        @{ var item = context.Item; }
        <MudButtonGroup Variant="Variant.Text" Size="Size.Small">
            <MudIconButton Icon="@Icons.Material.Filled.Edit"
                           Size="Size.Small"
                           Color="Color.Primary"
                           OnClick="@(() => Edit(item))"
                           title="Edit" />
            <MudIconButton Icon="@Icons.Material.Filled.Delete"
                           Size="Size.Small"
                           Color="Color.Error"
                           OnClick="@(() => Delete(item))"
                           title="Delete" />
        </MudButtonGroup>
    </CellTemplate>
</TemplateColumn>
```

### Status Columns
```razor
<TemplateColumn Title="Status" Sortable="false" Filterable="false" HeaderStyle="width: 75px;">
    <CellTemplate>
        <MudChip Size="Size.Small" T="string" Color="@GetStatusColor(context.Item.Status)">
            @GetStatusText(context.Item.Status)
        </MudChip>
    </CellTemplate>
</TemplateColumn>
```

## Tables with Many Columns (10+)

When a table has many columns, the key strategy is:
1. Set `ColumnResizeMode="ResizeMode.Container"` so users can adjust
2. Give fixed `HeaderStyle="width: Xpx;"` to compact columns (ID, dates, numbers, status)
3. Leave text columns (names, descriptions) without width so they absorb remaining space
4. Use `white-space: nowrap` on numeric cells to prevent them from expanding unnecessarily
