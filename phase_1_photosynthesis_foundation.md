# Phase 1 Photosynthesis Foundation

## Vision
Compile and Grow is a programming game about designing organisms in Growl. The long-term direction is a modular metabolism game: parts, harvesters, donors, fixers, waste, salvage chemistry, and organism-based supply chains.

Phase 1 should not try to build that whole system. Phase 1 should be a polished photosynthesis sandbox where the player can use Growl to optimize resource production from only **light**, **H2O**, and **CO2**.

The goal of Phase 1 is to prove that **programming organisms is fun even before advanced chemistry exists**.

## Non-goals for Phase 1
Phase 1 does **not** include:

- sulfur chemistry
- nitrogen chemistry
- salvage/refinement chains
- advanced donor/fixer compatibility
- facility logistics gameplay
- complex waste routing
- broad depot/material contract mechanics
- non-photosynthetic energy systems as core progression

These can exist as future-facing architecture in code, but they should not be required for the first playable loop.

## Phase 1 Core Loop
The Phase 1 loop is:

`light + H2O + CO2 -> energy -> glucose`

The player should be able to optimize this loop through code and organism design.

## What the Player Optimizes
The player should be able to tune:

- leaf area
- root size
- stomata openness
- light tracking
- growth vs storage

These are the core programming and morphology knobs for Phase 1.

## Player-Facing Metrics
Phase 1 should expose a small, readable dashboard:

- **glucose per tick**
- **net energy per tick**
- **water efficiency**
- **light capture %**
- **root supply ratio**
- **limiting factor**

Recommended limiting factor values:

- `light`
- `water`
- `carbon`
- `surface_area`
- `none`

### Notes
- **Glucose per tick** is the main success metric.
- **Limiting factor** is the main teaching/debugging metric.
- Oxygen can be tracked internally and optionally displayed as informational output, but it should not be a major Phase 1 gameplay resource.

## Phase 1 Programming Model
Phase 1 should stay close to the current Growl model:

### Structure / morphology
- `morph.create_part(...)`
- `morph.attach(...)`

### Root / leaf / stem behaviors
- `root.absorb("H2O")`
- `leaf.open_stomata(x)`
- `leaf.close_stomata()`
- `leaf.track_light(true/false)`
- `stem.store_water(x)`
- `stem.store_energy(x)`

### Phase 1 photo API
Keep the playable API minimal:

- `photo.process()`
- `photo.get_limiting_factor()`

`photo.process()` should be the main beginner entry point. It should:

- read leaf area
- read stomata openness
- read light
- read air CO2
- read water
- compute energy production
- convert usable production into glucose output/state

## Future-Proof Foundation
Under the hood, the codebase can begin to move toward the future metabolism model, but Phase 1 should expose only one valid setup:

- harvester = sun
- donor = water
- fixer = carbon_oxide

If helpful for architecture, Phase 1 may begin implementing internal support for future APIs like:

- `photo.bind_harvester(...)`
- `photo.bind_donor(...)`
- `photo.bind_fixer(...)`

But these should not be required for the first player-facing version.

## Resource Model for Phase 1
Only these matter for gameplay:

- light
- H2O
- CO2
- energy
- glucose

Oxygen may exist internally as a byproduct, but it should not yet drive strategy.

## Beginner Organism Types to Support
Phase 1 should support at least these example strategies:

- **basic plant**
- **water saver**
- **fast grower**
- **storage plant**
- **light tracker**
- **simple adaptive plant**

These should differ meaningfully in output and stability.

## Implementation Notes
Likely files touched in the current repo:

- `Assets/Scripts/PhotoModule.cs`
- `Assets/Scripts/GrowlGameStateBridge.cs`
- `Assets/Scripts/OrganismEntity.cs`
- `Assets/Scripts/ResourceGrid.cs`
- `Assets/Scripts/GrowlCompletionProvider.cs`
- `Assets/Scripts/GrowlSignatureHintProvider.cs`
- semantic/editor/runtime support only if new builtins are exposed

## Exit Criteria
Phase 1 is complete when:

- the player can write a simple plant in Growl
- the organism can produce glucose from light/H2O/CO2
- the six key metrics are visible in the UI/debug view
- different Growl strategies lead to measurably different outcomes
- at least 3 beginner Growl example programs work end-to-end

## Guiding Principle
**Phase 1 is not the full metabolism game. It is a polished sandbox for optimizing photosynthesis with code, using only light, H2O, and CO2, while laying the architectural foundation for later metabolic complexity.**

