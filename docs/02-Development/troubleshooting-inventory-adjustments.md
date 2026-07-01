# Troubleshooting: Inventory Adjustments

## Stock Actual shows "–" for a product that has stock

### Symptoms

In the **Nuevo Ajuste** dialog (`/shop/inventory` → Ajustes → Nuevo Ajuste), a product's **Stock Actual** column displays a dash (`—`) instead of a number, even though the Inventory Status page (Existencias) shows the product has stock.

### Root Cause

The `GetProductStockAsync` service method short-circuits for products with `RequiresInventory = false`:

```csharp
// InventoryQueryService.cs
if (!product.RequiresInventory)
{
    return new ProductStockDto
    {
        RequiresInventory = false,
        LocationStock = []  // empty — no DB query is made
    };
}
```

If a product has `RequiresInventory = false` but still has records in `sh_inventory` (e.g., the flag was changed after stock was entered), the dialog receives an empty `LocationStock` list and renders `—`.

### How to Diagnose

Run against the production database:

```sql
-- Find products with this inconsistency
SELECT p.Id, p.Code, p.Name, p.RequiresInventory,
       i.Quantity, i.LocationId, l.Name AS Location
FROM sh_products p
INNER JOIN sh_inventory i ON i.ProductId = p.Id
JOIN sh_locations l ON l.Id = i.LocationId
WHERE p.RequiresInventory = 0
  AND i.IsDeleted = 0
  AND i.Quantity > 0;
```

Any row returned has conflicting data: the product says "no inventory tracking" but there is actual stock recorded.

### Resolution Options

**Option A — Correct the product flag (recommended when the product does require inventory tracking):**

```sql
UPDATE sh_products
SET RequiresInventory = 1
WHERE Code = 'P0258';  -- replace with the affected product code
```

Verify in the app: the Stock Actual column should now display the correct quantity.

**Option B — Accept the inconsistency (when the product genuinely should not track inventory):**

The `—` display is technically correct: the product is configured to not use inventory. The stock records are leftover data. No action is required in the adjustment dialog, but you may want to zero out or delete the orphaned inventory records for cleanliness.

### Known Occurrences

| Date | Product | Resolution |
|------|---------|------------|
| 2026-06-30 | P0258 – PEGA MOSCAS LION TOOLS | Identified; `RequiresInventory = false` with 4.00 units in Tienda 1 |

### Related Code

| File | Relevance |
|------|-----------|
| `src/App.Services/Inventory/InventoryQueryService.cs:130` | Short-circuit for `RequiresInventory = false` |
| `src/App.Web/Components/Shop/Inventory/CreateAdjustmentEntryDialog.razor:388` | Null-propagation renders `—` when `LocationStock` is empty |
