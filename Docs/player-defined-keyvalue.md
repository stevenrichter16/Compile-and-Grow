# Player-Defined Key/Value On `org`

## Summary

Player-defined organism state already mostly exists in the runtime today. Unknown keys passed through `org_set`, `org_get`, and `org_add` are stored in `OrganismEntity._customState`.

The implementation work is not about inventing a new storage system. It is about formalizing the feature so players can use it safely and predictably.

Recommended direction:

- keep engine-owned organism keys unprefixed
- require player-defined organism keys to use a namespace
- keep `org_memory` for private script/controller state
- keep custom `org` state for player-visible organism properties/resources

Recommended namespace:

- `u.`

Example usage:

- `org_set("u.root_bias", 0.7)`
- `org_get("u.root_bias", 0)`
- `org_add("u.sugar_budget", 1)`

## Design Goals

- give players more expressive power without adding a builtin for every concept
- avoid collisions with engine-owned keys
- keep organism state readable in UI/debug tools
- keep `org_memory` distinct from organism-facing state
- prevent silent typo-driven junk state

## State Model

Two separate concepts should remain distinct:

- `org_memory`
  - private controller memory
  - mode flags, cooldowns, temporary logic
  - not part of the organism's public biology model

- player-defined `org` state
  - persistent organism-facing state
  - inspectable and debuggable
  - part of the organism model

Examples:

- `org_memory_set("_water_saver", 1)` is controller state
- `org_set("u.root_bias", 0.7)` is organism state

## Proposed Rules

### 1. Reserved engine keys stay unprefixed

Examples:

- `water`
- `energy`
- `health`
- `stress`
- `age`
- `glucose`
- `glucose_per_tick`
- `net_energy_per_tick`
- `water_efficiency`
- `light_capture_pct`
- `root_supply_ratio`
- `limiting_factor`

### 2. Player-defined keys must use a namespace

Recommended:

- `u.*`

Examples:

- `u.root_bias`
- `u.signal`
- `u.pigment_bias`

### 3. Supported value types should stay narrow

Allow:

- number
- bool
- string

Avoid for now:

- nested dictionaries
- arbitrary lists
- container objects

Keeping types simple will make save/load, UI inspection, and debugging much easier.

### 4. `org_add` should be numeric-only for custom keys

This should work:

- `org_add("u.sugar_budget", 1)`

This should fail clearly:

- `org_add("u.mode", 1)` when `u.mode` is a string

## Implementation Plan

### Phase 1. Formalize the contract in `OrganismEntity`

Touch:

- `Assets/Scripts/OrganismEntity.cs`

Add helper methods:

- `IsBuiltInKey(string key)`
- `IsReservedEngineKey(string key)`
- `IsPlayerDefinedKey(string key)` for `u.*`
- `IsSupportedPlayerValue(object value)`

Change behavior:

- `TryGetState`
  - keep current built-in reads
  - allow reads from `u.*`
  - optionally preserve legacy unprefixed custom reads during migration

- `TrySetState`
  - keep writable built-ins working
  - allow writes to `u.*`
  - reject unsupported custom value types
  - reject or warn on unknown unprefixed keys

- `TryAddState`
  - keep built-in numeric add paths
  - allow adds to `u.*` only if numeric
  - reject adds to non-numeric custom state with a clear error

### Phase 2. Keep migration soft first

Current reality:

- unknown unprefixed keys already become custom state

Recommended migration:

- keep that behavior temporarily
- emit a warning when an unknown unprefixed key is written
- update docs/examples/editor hints to use `u.*`
- later decide whether to fully reject unprefixed unknown keys

This avoids breaking existing scripts immediately.

### Phase 3. Improve Growl-facing errors

Touch:

- `Assets/Scripts/GrowlGameStateBridge.cs`

Add clearer failures for:

- invalid custom key name
- unsupported custom value type
- numeric add on non-numeric custom state
- write attempts to reserved engine-only containers

The bridge already owns the user-facing builtin behavior, so it is the right place for clean errors.

### Phase 4. Surface custom state in UI/debug views

Touch:

- `Assets/UI/Scripts/PlantDetailController.cs`
- optionally `Assets/UI/Scripts/PlantSidebarController.cs`

Recommended behavior:

- keep core metrics in dedicated rows
- show player-defined `u.*` keys in a separate `Custom` section in the detail panel
- do not dump every `u.*` key into the compact sidebar by default

Possible later extension:

- only show custom keys in the sidebar if they follow a display convention, such as `u.display_*`

### Phase 5. Update editor guidance

Touch:

- `Assets/Scripts/GrowlCompletionProvider.cs`
- `Assets/Scripts/GrowlSignatureHintProvider.cs`
- starter/example Growl snippets if desired

Add:

- docs/help text explaining the `u.*` rule
- examples for `org_get`, `org_set`, and `org_add`
- completion/snippet guidance for `u.*`

### Phase 6. Add tests

Add focused tests under:

- `Assets/Editor/Tests`

Cover:

- `org_set("u.foo", 1)` succeeds
- `org_get("u.foo", 0)` returns the stored value
- `org_add("u.foo", 2)` works for numeric custom state
- `org_add("u.label", 1)` fails cleanly for non-numeric state
- built-in keys still work unchanged
- unknown unprefixed keys warn or fail depending on migration phase
- state snapshots include player-defined keys
- UI detail surface shows `u.*` keys in a custom section

## File-Level Change List

Primary runtime files:

- `Assets/Scripts/OrganismEntity.cs`
- `Assets/Scripts/GrowlGameStateBridge.cs`

Supporting UX files:

- `Assets/UI/Scripts/PlantDetailController.cs`
- `Assets/Scripts/GrowlCompletionProvider.cs`
- `Assets/Scripts/GrowlSignatureHintProvider.cs`

Testing:

- add new focused tests under `Assets/Editor/Tests`

## Rollout Order

1. formalize key validation in `OrganismEntity`
2. add better bridge errors and warnings
3. update docs/examples/editor hints
4. expose custom state in the detail UI
5. later decide whether to enforce the namespace strictly

## Why This Shape

This gives players the flexibility to define organism-facing properties while keeping the simulation model coherent.

Benefits:

- avoids collisions with engine-owned state
- preserves a clear distinction between script memory and organism biology
- keeps UI/debugging sane
- makes future engine expansion safer

The important design boundary is:

- `_foo` style keys belong in `org_memory`
- `u.foo` style keys belong in player-defined organism state
