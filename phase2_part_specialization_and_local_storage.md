# Phase 2 Part Specialization and Local Storage

## Purpose
Phase 2 builds directly on the Phase 1 loop:

`light + H2O + CO2 -> energy -> glucose`

Phase 2 adds:

- stronger part specialization
- local storage and buffering
- clearer internal resource flow
- better player feedback about why a design works

Phase 2 does **not** yet add:

- sulfur chemistry
- nitrogen chemistry
- salvage chemistry
- advanced donor/fixer compatibility
- facility logistics as a gameplay system
- waste routing as a major mechanic

The goal of Phase 2 is to deepen the photosynthesis sandbox without opening the full metabolism game yet.

## New Player-Facing Goals
The player should now be optimizing:

- leaf area vs root capacity
- transport and storage capacity in stems
- local water buffering
- local energy and glucose buffering
- growth vs storage policy
- simple adaptive regulation

## Core Design Rule
**Parts are now functional hardware, not just geometry.**

- leaves are collectors
- roots are intake organs
- stems are transport and storage organs

This should reinforce the existing module structure in Growl and the runtime.

## Phase 2 Mechanics
Phase 2 should introduce or strengthen:

- local glucose state
- local water storage
- local energy storage
- root supply ratio
- leaf utilization
- storage allocation
- growth allocation

These mechanics should make different organism architectures perform differently in understandable ways.

## Growl Phase 2 API
Phase 2 should stay close to the current Growl model.

### Keep using existing concepts
- `morph.create_part(...)`
- `morph.attach(...)`
- `root.absorb("H2O")`
- `leaf.open_stomata(x)`
- `leaf.close_stomata()`
- `leaf.track_light(true/false)`
- `stem.store_water(x)`
- `stem.store_energy(x)`
- `photo.process()`
- `photo.get_limiting_factor()`

### Add only small helper APIs if needed
- `photo.get_water_efficiency()`
- `photo.get_light_capture()`
- `photo.get_root_supply_ratio()`
- `org.glucose`
- `org.glucose_per_tick`

Avoid introducing full depot/facility behavior in Phase 2.

## Resource Model for Phase 2
Phase 2 still only uses:

- light
- H2O
- CO2
- energy
- glucose

Oxygen may exist internally or as informational output, but should not become an optimization branch yet.

## Simulation Goals
Phase 2 should make these relationships matter:

- larger leaves increase production but increase water demand
- larger roots improve water supply and stability
- larger stems improve buffering and storage
- different storage policies change resilience and throughput
- limiting factor reporting helps the player debug designs

## Metrics to Expose
Phase 2 should expose these in state, debug UI, or a simple dashboard:

- glucose per tick
- stored glucose
- stored water
- stored energy
- root supply ratio
- light capture %
- leaf utilization
- limiting factor

The player should be able to understand why a design is underperforming.

## Example Organism Types to Support
Phase 2 should support example strategies such as:

- BalancedPlant
- WaterSaver
- StoragePlant
- FastGrower
- WideCanopy

These should differ in architecture, buffering behavior, and regulation.

## Implementation Notes
Likely files touched when Phase 2 is eventually implemented:

- `Assets/Scripts/PhotoModule.cs`
- `Assets/Scripts/GrowlGameStateBridge.cs`
- `Assets/Scripts/OrganismEntity.cs`
- `Assets/Scripts/ResourceGrid.cs`
- `Assets/Scripts/GrowlCompletionProvider.cs`
- `Assets/Scripts/GrowlSignatureHintProvider.cs`
- analyzer/runtime/editor support only if new builtins are exposed

## Exit Criteria
Phase 2 is complete when:

- different leaf/root/stem architectures produce clearly different outcomes
- glucose and water can be buffered locally
- growth-vs-storage strategies feel meaningfully different
- at least 3–5 example organisms show distinct design philosophies
- players can understand why a design is bottlenecked

## What Phase 2 Should Not Become
Phase 2 should not become:

- donor/fixer class programming
- waste repositories
- facility requests/publishing
- salvage or wreckage processing
- sulfur/nitrogen bootstrap systems

Those belong to later phases.

## Guiding Principle
**Phase 2 deepens the photosynthesis sandbox by making leaves, roots, and stems functionally distinct. The player still works only with light, H2O, and CO2, but now optimizes internal storage, water supply, transport, and growth allocation. The goal is to make organism architecture and simple regulation meaningfully programmable before introducing advanced chemistry.**
