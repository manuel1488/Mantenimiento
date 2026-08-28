# Text Field Length Limits — MudTextField ↔ DB Column Rule

Triggered by [Incident: Cotización foto description too long for DB column](../03-Incidents/2026-08-26-text-field-length-mismatch.md).
Read that incident for the full story; this doc is the standing rule.

## The rule

**Every `MudTextField` (or any editable text input) bound to a string property that has a
length constraint at the model/DB level MUST carry a matching `MaxLength` attribute.**

The constraint can come from:
- `[StringLength(N)]` or `[MaxLength(N)]` on the entity property (`App.Models/**`)
- `[StringLength(N)]` on the DTO property (`App.Core/DTOs/**`) — DTOs normally mirror the
  entity's limit, but check both if they ever diverge
- `.HasMaxLength(N)` in a Fluent API configuration (`App.Models.Data/Configurations/**`)

When you add or edit a `MudTextField`, look up the bound property's limit and set:

```razor
<MudTextField @bind-Value="Model.Descripcion"
              Label="@L["Description"]"
              Lines="2"
              MaxLength="500" Counter="500" />
```

- Always set `MaxLength` to the exact DB limit — never a rounder/larger number "to be safe".
- Add `Counter="{N}"` (same value as `MaxLength`) on any multiline field (`Lines > 1`) so the
  user sees how much room is left instead of hitting the wall silently. Single-line fields
  don't need the visible counter, `MaxLength` alone is enough since the field is short.
- If a property has no `[StringLength]`/`[MaxLength]` at all, it maps to MySQL's default
  (or `longtext`/`text` if explicitly configured that way, e.g. HTML/CSS template content).
  Don't add an arbitrary `MaxLength` in that case — there's nothing to match.

## Why this matters

Without a client-side `MaxLength`, a user can type past the DB column's limit and the app
only discovers this at `SaveChangesAsync()` — surfacing as a raw MySQL exception
(`Data too long for column '...'`) that the service layer turns into a generic, unhelpful
error toast. The `MaxLength` attribute is the cheapest possible fix: it stops the problem
at the point of entry and gives the user a visible signal (`Counter`) instead of a failed
save after the fact.

## When adding a new string field end-to-end

1. Decide the real-world limit and set it once, consistently, on:
   - the entity property (`[StringLength(N)]`)
   - the DTO property(ies) that carry it (`[StringLength(N)]`)
   - the `MudTextField` (`MaxLength="N"`, plus `Counter="N"` if multiline)
2. Generate the EF migration for the new/changed column.
3. If you're ever unsure whether the UI field already matches, grep the entity/DTO for
   the property's `[StringLength]` and confirm the `MudTextField` has the same number —
   don't assume it was set correctly when the field was first built.
