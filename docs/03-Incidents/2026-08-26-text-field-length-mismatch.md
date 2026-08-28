# Incident: Cotización foto description too long for DB column

**Date**: 2026-08-26
**Severity**: Low (caught in production, no data loss — the write was rejected, not corrupted)
**Status**: Fixed

## What happened

A user editing the description of a Cotización photo (`CotizacionFotoViewerDialog.razor`)
typed a description longer than 300 characters and hit blur. The save failed with:

```
Microsoft.EntityFrameworkCore.DbUpdateException: An error occurred while saving the
entity changes. See the inner exception for details.
 ---> MySqlConnector.MySqlException (0x80004005): Data too long for column 'Descripcion' at row 1
```

The service caught the exception and returned `Result.Failure`, so the app didn't crash —
but the user saw a generic "Error updating foto" toast with no indication of *why*, and
nothing had stopped them from typing past the limit in the first place.

## Root cause

`CotizacionFoto.Descripcion` was `[StringLength(300)]` (`varchar(300)` in MySQL), but the
`MudTextField` bound to it in `CotizacionFotoViewerDialog.razor` had no `MaxLength`. The
client-side input was unbounded while the database column was not — any text over 300
chars would type, display, and only fail at the very last step (`SaveChangesAsync`).

## Fix

1. Raised `CotizacionFoto.Descripcion` to `[StringLength(3000)]` (more realistic for a
   free-text photo caption) and added EF migration `ExpandCotizacionFotoDescripcion`
   (`ALTER COLUMN` widening `varchar(300)` → `varchar(3000)`, no data loss).
2. Added `MaxLength="3000" Counter="3000"` to the `MudTextField` so the client-side limit
   matches the DB column exactly, and the user sees a live character counter instead of
   discovering the limit only on save failure.

## Wider audit

The same shape of bug (model/DB `StringLength` present, but the bound `MudTextField` has
no matching `MaxLength`) was found and fixed in ~24 other fields across the codebase:
`CotizacionDetallePage` (AprobadaPor, MedioAprobacion), `ConvertirCotizacionAObraDialog`
and `ObraFormPage` (Direccion), `ServicioFormPage` (Nombre, Descripcion),
`UnidadMedidaDialog` (Codigo, Nombre, Descripcion), the entire `ClienteFormPage` form, and
`CotizacionSettingsTab` (payment terms, address, banking details, social/contact fields).

## Prevention

See [Text field length limits guide](../02-Development/text-field-length-limits-guide.md) —
new development rule: every `MudTextField`/`MudTextField` bound to a length-constrained
string property must carry a matching `MaxLength` (and `Counter` on multiline fields).
