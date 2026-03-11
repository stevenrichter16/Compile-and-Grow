# Phase 2 Implementation Checklist

Source brief:

- [phase2_part_specialization_and_local_storage.md](/Users/steven/Compile%20and%20Grow/phase2_part_specialization_and_local_storage.md)

## Goal

Deepen the Phase 1 photosynthesis sandbox by making leaves, roots, and stems functionally distinct through:

- local storage
- buffering
- clearer internal resource flow
- stronger architectural tradeoffs

## Scope

Stay inside the existing sandbox:

- light
- H2O
- CO2
- energy
- glucose

Do not add in this phase:

- donor/fixer programming
- sulfur or nitrogen systems
- salvage systems
- facility logistics
- waste-routing gameplay

## Runtime

- [x] Add local glucose storage on parts via `stored_glucose`
- [x] Add aggregate organism metrics:
  - [x] `stored_glucose`
  - [x] `stored_water`
  - [x] `stored_energy`
  - [x] `leaf_utilization`
- [x] Make `photo.process()` update the new metrics every tick
- [x] Keep `photo.process()` compatible with Phase 1 scripts
- [x] Keep `photo.get_limiting_factor()` snapshot-based
- [x] Make thicker stems improve local storage capacity
- [ ] Keep roots, leaves, and stems functionally distinct:
  - [ ] roots drive intake stability
  - [ ] leaves drive production
  - [ ] stems drive buffering and storage

## Growl Surface

- [x] Keep existing Phase 1 calls working:
  - [x] `root.absorb("H2O")`
  - [x] `leaf.open_stomata(...)`
  - [x] `leaf.close_stomata()`
  - [x] `leaf.track_light(...)`
  - [x] `stem.store_water(...)`
  - [x] `stem.store_energy(...)`
  - [x] `photo.process()`
  - [x] `photo.get_limiting_factor()`
- [x] Add `stem.store_glucose(...)`
- [ ] Decide whether to add helper getters:
  - [ ] `photo.get_water_efficiency()`
  - [ ] `photo.get_light_capture()`
  - [ ] `photo.get_root_supply_ratio()`
  - [ ] `photo.get_leaf_utilization()`
- [x] Add explicit storage allocation controls:
  - [x] `photo.set_glucose_storage_bias(...)`
  - [x] `photo.set_energy_storage_bias(...)`

## Allocation

- [x] Add a simple growth-vs-storage policy
- [x] Add a simple immediate-vs-buffered glucose policy
- [x] Keep the first version narrow and understandable
- [ ] Avoid transport-graph or depot behavior

## UI And Debug

- [x] Show the following somewhere visible:
  - [x] glucose per tick
  - [x] stored glucose
  - [x] stored water
  - [x] stored energy
  - [x] root supply ratio
  - [x] light capture %
  - [x] leaf utilization
  - [x] limiting factor
- [ ] Keep compact sidebar focused
- [x] Put the fuller Phase 2 breakdown in the detail panel and process log
- [x] Expose root supply, light capture, and limiting factor in the compact sidebar

## Examples

- [x] Add `Assets/GrowlExamples/Phase2/`
- [x] Add:
  - [x] `BalancedPlant`
  - [x] `WaterSaver`
  - [x] `StoragePlant`
  - [x] `FastGrower`
  - [x] `WideCanopy`
- [x] Make the examples differ by:
  - [x] architecture
  - [x] buffering behavior
  - [x] regulation
  - [x] storage policy

## Tests

- [ ] Add runtime tests for:
  - [x] local glucose storage
  - [x] aggregate storage metrics
  - [x] leaf utilization
  - [x] stem thickness affecting storage capacity
  - [ ] different architectures producing different outcomes
- [x] Add Growl bridge/runtime tests for any new builtins
- [ ] Keep Phase 1 regression coverage intact

## Suggested Commit Order

- [x] `docs: add phase 2 implementation checklist`
- [x] `runtime: add phase 2 glucose storage and utilization`
- [x] `runtime: add phase 2 storage allocation and capacity scaling`
- [x] `ux: expose phase 2 storage and utilization metrics`
- [x] `examples: add phase 2 organism strategies`
- [x] `tests: add phase 2 runtime and growl coverage`

## Exit Check

- [ ] Different leaf/root/stem architectures produce clearly different outcomes
- [ ] Water and glucose can both be buffered locally
- [ ] Storage strategy meaningfully affects resilience and throughput
- [ ] At least 3 to 5 examples show distinct design philosophies
- [ ] The player can understand why a design is bottlenecked
