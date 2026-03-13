# Growl Authoring Surface Mix

## Purpose

This document defines a cleaner player-facing authoring style for Growl:

- words first, for qualitative plant design choices
- integers where counting, ranking, or stepping feels natural
- decimals kept mostly inside the runtime and compiler lowering layer
- object-oriented visual structure for common plant references and behaviors

The goal is to make Growl easier to read and write without removing the low-level APIs that already exist.

## Core Rule

Growl should be:

- word-based for form, qualities, and named strategies
- integer-based for counts, steps, priorities, and durations
- object-oriented in how the player references plant parts, metrics, stores, and recipes
- low-level only when the player explicitly wants direct control

The current runtime stays valid as the underlying target:

- `morph.create_part(...)`
- `morph.attach(...)`
- `root.absorb(...)`
- `leaf.open_stomata(...)`
- `leaf.track_light(...)`
- `stem.store_water(...)`
- `stem.store_energy(...)`
- `stem.store_glucose(...)`
- `photo.process()`
- `photo.get_limiting_factor()`
- `photo.set_glucose_storage_bias(...)`
- `photo.set_energy_storage_bias(...)`

## Why This Mix

Decimals are useful internally, but noisy in source code.

The player-facing surface should also stay visually programmable, not slide into pure data configuration.

Growl should feel closer to:

- `Recipes.WaterSaver.Apply()`
- `Plant.Stems.Main.Thickness = Thick`
- `Plant.Leaves.Canopy.Count = 2`
- `if Metrics.RootSupply.IsWeak():`

and less like:

- large blocks of decimal-heavy setup
- quasi-YAML configuration without object references

This mix keeps the parts of programming that are interesting:

- choosing an organism design
- expressing tradeoffs
- defining adaptation logic
- writing strategy

It removes friction in areas that often become messy:

- manual morphology constants
- repeated fractional growth amounts
- fine-grained storage bias tuning in ordinary scripts

## Use Words For

Use words when the player is making a qualitative design choice.

Recommended examples:

- stem thickness
- leaf size category
- root depth style
- canopy density
- material style
- stomata mode
- storage attitude
- growth attitude

Example word sets:

- stem thickness: `thin`, `medium`, `thick`, `swollen`
- leaf size: `small`, `medium`, `large`
- root depth: `shallow`, `medium`, `deep`
- storage attitude: `low`, `balanced`, `high`, `reserve_first`

## Use Integers For

Use integers when the player is counting, ranking, or advancing in steps.

Recommended examples:

- leaf count
- branch count
- growth steps
- storage priority
- adaptation delay
- signal radius
- cycle length
- allocation points

## Object-Oriented Surface

The new authoring layer should stay visually object-oriented.

Recommended top-level namespaces:

- `Plant`
- `Policies`
- `Metrics`
- `Stores`
- `Recipes`

Recommended examples:

- `Plant.Stems.Main.Thickness = Thick`
- `Plant.Roots.Tap.Size = Medium`
- `Plant.Leaves.Canopy.Count = 2`
- `Policies.Stomata.Mode = Balanced`
- `Policies.Storage.GlucosePriority = 3`
- `Metrics.RootSupply.IsWeak()`
- `Stores.Glucose.IsHigh()`
- `Recipes.WaterSaver.Apply()`

This keeps the language feeling like code, while still allowing higher-level sugar over the current runtime.

## Named Part Groups

The object-oriented surface should support named groups for parts that players naturally think about as cohorts.

Examples:

- `Plant.Leaves.Canopy`
- `Plant.Roots.Foragers`
- `Plant.Branches.Laterals`

In this model, a handle such as `Plant.Leaves.Canopy` is not just a naming shortcut. It is a collection-level object that can:

- define how many members exist
- apply shared settings to every member
- expose group-level operations
- allow optional per-instance overrides

Example:

```growl
Plant.Leaves.Canopy.Count = 2
Plant.Leaves.Canopy.Size = Medium
Plant.Leaves.Canopy.AttachTo(Plant.Stems.Main)
Plant.Leaves.Canopy.TrackLight(True)
```

This should lower to two concrete leaf parts under the hood, while preserving `Plant.Leaves.Canopy` as a source-level abstraction.

### Grouping Rules

Group handles should work best for:

- leaves
- fine roots
- branches
- tendrils
- other repeated support or surface organs

Group handles should be used more carefully for:

- main stems
- trunks
- tap roots
- other topology-defining core organs

Recommended rule:

- repeated organs can be grouped by default
- core structural organs should stay singular by default

### Group Behavior

Assignments to a group should broadcast to all members.

Example:

```growl
Plant.Roots.Foragers.Count = 3
Plant.Roots.Foragers.Size = Small
Plant.Roots.Foragers.Grow(1)
```

This should affect the whole group.

Optional per-member overrides should still be possible.

Example:

```growl
Plant.Leaves.Canopy[0].Size = Large
Plant.Leaves.Canopy[1].Coating = Waxy
```

### Group Reads Should Be Explicit

Reads from grouped handles should avoid ambiguity.

Good examples:

- `Plant.Leaves.Canopy.Count`
- `Plant.Leaves.Canopy.AnyDamaged()`
- `Plant.Leaves.Canopy.AverageSize()`
- `Plant.Roots.Foragers.TotalDepth()`

Avoid implicit reads that are unclear on collections.

Example to avoid:

```growl
x = Plant.Leaves.Canopy.Size
```

If a grouped property is read, the source should make it clear whether the result is:

- a count
- an average
- a maximum
- a boolean summary
- an indexed member value

## Example 1: Morphology

Authoring:

```growl
Plant.Stems.Main.Size = Medium
Plant.Stems.Main.Thickness = Thick

Plant.Roots.Tap.Size = Medium
Plant.Roots.Tap.AttachTo(Plant.Stems.Main)

Plant.Leaves.Canopy.Count = 2
Plant.Leaves.Canopy.Size = Medium
Plant.Leaves.Canopy.AttachTo(Plant.Stems.Main)
```

Lowered form:

```growl
if org_memory_get("_built") != 1:
    morph.create_part("main", "stem", size: 1.2, thickness: 1.4)
    morph.create_part("tap", "root", size: 1.2)
    morph.create_part("canopy_1", "leaf", size: 1.2)
    morph.create_part("canopy_2", "leaf", size: 1.2)
    morph.attach("tap", "main")
    morph.attach("canopy_1", "main")
    morph.attach("canopy_2", "main")
    org_memory_set("_built", 1)
```

Suggested starter mappings:

- stem size
  - `small` -> `1.0`
  - `medium` -> `1.2`
  - `large` -> `1.5`
- stem thickness
  - `thin` -> `0.8`
  - `medium` -> `1.0`
  - `thick` -> `1.4`
  - `swollen` -> `1.8`
- leaf size
  - `small` -> `0.9`
  - `medium` -> `1.2`
  - `large` -> `1.5`
- root size
  - `small` -> `1.0`
  - `medium` -> `1.2`
  - `large` -> `1.6`

These exact values can change later. The important part is the source format.

Suggested lowering rules:

- `Plant.Stems.Main.Size = Medium`
  - creates `main` as a stem if it does not exist
- `Plant.Roots.Tap.AttachTo(Plant.Stems.Main)`
  - lowers to `morph.attach("tap", "main")`
- `Plant.Leaves.Canopy.Count = 2`
  - lowers to repeated `morph.create_part(...)` calls for the named leaf group
- `Plant.Leaves.Canopy.Size = Medium`
  - applies the same leaf size to each member of the `Canopy` group
- `Plant.Leaves.Canopy.AttachTo(Plant.Stems.Main)`
  - lowers to `morph.attach(...)` for every member of the group

## Example 2: Mixed Word + Integer Control

Authoring:

```growl
Policies.Stomata.Mode = Balanced
Policies.Tracking.Mode = Light
Policies.Storage.GlucosePriority = 3
Policies.Storage.EnergyPriority = 1

if Metrics.RootSupply.IsWeak():
    Plant.Roots.Tap.Grow(1)
elif Stores.Glucose.IsHigh():
    Plant.Leaves.Canopy.Grow(1)
```

Lowered form:

```growl
leaf.open_stomata(0.45)
leaf.track_light(true)
photo.set_glucose_storage_bias(0.70)
photo.set_energy_storage_bias(0.25)

if org_get("root_supply_ratio", 0) < 0.85:
    root.grow_down(0.25)
elif org_get("stored_glucose", 0) > 0.45:
    leaf.grow(0.22, "stem_main")
```

Suggested mappings:

- stomata mode
  - `closed` -> `leaf.open_stomata(0.0)`
  - `conservative` -> `leaf.open_stomata(0.15)`
  - `balanced` -> `leaf.open_stomata(0.45)`
  - `open` -> `leaf.open_stomata(0.70)`
- storage priority
  - `0` -> `0.00`
  - `1` -> `0.25`
  - `2` -> `0.45`
  - `3` -> `0.70`
  - `4` -> `0.90`
- growth steps
  - `Plant.Roots.Tap.Grow(1)` -> `root.grow_down(0.25)`
  - `Plant.Roots.Tap.Grow(2)` -> `root.grow_down(0.50)`
  - `Plant.Stems.Main.Grow(1)` -> `stem.grow_up(0.15)`
  - `Plant.Leaves.Canopy.Grow(1)` -> `leaf.grow(0.22, "stem_main")`

## Example 3: Words For Shape, Integers For Quantity

Authoring:

```growl
Plant.Stems.Main.Thickness = Swollen
Plant.Roots.Primary.Count = 1
Plant.Roots.Primary.Size = Large
Plant.Leaves.Canopy.Count = 3
Plant.Leaves.Canopy.Size = Small

Policies.Water.Mode = Conservative
Policies.Storage.ReserveTarget = 3
```

Lowered form:

```growl
morph.create_part("main", "stem", size: 1.2, thickness: 1.8)
morph.create_part("root_main", "root", size: 1.6)
morph.create_part("leaf_1", "leaf", size: 0.9)
morph.create_part("leaf_2", "leaf", size: 0.9)
morph.create_part("leaf_3", "leaf", size: 0.9)

leaf.open_stomata(0.15)
photo.set_glucose_storage_bias(0.70)
```

This pattern keeps the code readable:

- words describe what the plant is like
- integers describe how much of something exists or how hard to push it
- object references describe where that behavior lives
- named groups describe how repeated organs are organized

## Recommended Player-Facing Conventions

Prefer words for:

- `thickness`
- `size`
- `depth`
- `spread`
- `density`
- `mode`
- `material`
- `attitude`
- `state band`

Prefer integers for:

- `count`
- `priority`
- `steps`
- `ticks`
- `radius`
- `allocation`

## Recipes / Bodyplans

Bodyplans should fit this same mix.

A bodyplan is a reusable authored pattern that expands into lower-level Growl. It can be:

- bought
- earned
- gifted
- unlocked from successful commissions

Example:

```growl
Recipes.WaterSaver.Apply()

Plant.Stems.Main.Thickness = Thick
Plant.Leaves.Canopy.Count = 1
Plant.Roots.Primary.Count = 1
Policies.Storage.GlucosePriority = 3
```

This keeps recipes valuable while still letting the player customize them.

## Reactive / Dataflow Commission Examples

Reactive / dataflow Growl is useful when the player is automating behavior that would be tedious to manage through UI sliders, toggles, and per-part menus.

The point of these examples is not just syntax. The point is that the player is writing plant behavior that keeps running and adapting on its own once deployed.

### Simple Example 1: Drought Windowbox Crop

Commission: "Make a food crop for rooftop trays with inconsistent watering."

```growl
Recipes.BasicCrop.Apply()

signal Dry when State.Water < Low
signal Stable when Metrics.RootSupply > Strong

on Dry:
    Policies.Stomata.Mode = Conservative
    Policies.Storage.GlucosePriority = 3

on Stable:
    Plant.Roots.Foragers.Absorb("H2O")
    Plant.Leaves.Canopy.TrackLight(True)

when Stores.Glucose.IsHigh():
    Plant.Fruit.Cluster.Grow(1)
```

Explanation:

- `Recipes.BasicCrop.Apply()` gives the player a viable starter organism and defines named handles such as `Plant.Roots.Foragers`, `Plant.Leaves.Canopy`, and `Plant.Fruit.Cluster`.
- `signal Dry` and `signal Stable` create readable named conditions instead of forcing the player to repeat the same threshold checks everywhere.
- `on Dry` switches the plant into water-saving behavior and increases storage preference, which is exactly the kind of repetitive response logic that is annoying to configure through UI.
- `on Stable` restores active intake and light tracking once the plant is no longer under stress.
- `when Stores.Glucose.IsHigh()` turns surplus glucose into fruit growth, so the commission becomes a behavior program instead of a fixed growth preset.

### Simple Example 2: Self-Shading Window Vine

Commission: "Keep this west-facing room cooler in the afternoon without killing the plant."

```growl
Recipes.WindowVine.Apply()

signal HarshLight when Env.Light.Intensity > High
signal Dry when State.Water < Low

on HarshLight:
    Plant.Leaves.Awning.TrackLight(True)
    Plant.Leaves.Awning.Grow(1)

on Dry:
    Policies.Stomata.Mode = Conservative

when Metrics.RootSupply.IsStrong() and Stores.Glucose.IsHigh():
    Plant.Branches.Awning.Grow(1)
```

Explanation:

- `Recipes.WindowVine.Apply()` defines a vine-oriented plant shape and handles such as `Plant.Leaves.Awning` and `Plant.Branches.Awning`.
- `signal HarshLight` makes the commission about environmental sensing rather than manual leaf positioning.
- `on HarshLight` grows and reorients the shading canopy automatically, which is more satisfying to script than to micromanage part by part.
- `on Dry` prevents the plant from overcommitting to shade growth when water becomes a constraint.
- The final `when` clause ties structural expansion to healthy root supply and sugar reserves, so the vine only spreads when it can afford to.

### Simple Example 3: Night Marker Moss

Commission: "Mark a safe path after dusk, but stay dim in daytime."

```growl
Recipes.GlowMoss.Apply()

signal Night when Env.Light.Phase == Night
signal Day when Env.Light.Phase != Night
signal Dry when State.Water < Low

on Night:
    Products.MarkerGlow.Emit(1)
    Policies.Stomata.Mode = Balanced

on Day:
    Products.MarkerGlow.Stop()
    Plant.Surface.Patch.Camouflage = Dark

on Dry:
    Policies.Stomata.Mode = Conservative
```

Explanation:

- `Recipes.GlowMoss.Apply()` defines the core moss structure plus a product/output handle such as `Products.MarkerGlow`.
- The day/night signals let the player write a very clear schedule-driven behavior instead of clicking between separate daytime and nighttime modes in UI.
- `on Night` activates the marker output and keeps the plant operational.
- `on Day` shuts the effect off and returns the surface to a less conspicuous state.
- `on Dry` adds a separate water-preservation rule, which shows how multiple concerns can layer cleanly in code.

### Complex Example 4: Structural Brace Vine

Commission: "Climb a damaged catwalk, brace weak sections, and thicken if the structure starts shifting."

```growl
Recipes.BraceVine.Apply()

signal SupportSeen when Plant.Stems.Main.TouchingSupport()
signal LoadHigh when State.Stress > Medium
signal Dry when State.Water < Low
signal Damage when Events.Damage

flow Stores.Glucose -> Plant.Stems.Main when SupportSeen
flow Stores.Water -> Plant.Branches.Braces when LoadHigh

on SupportSeen:
    Plant.Stems.Main.AttachTo("catwalk_rail")
    Plant.Branches.Braces.GrowToward(Support)
    Plant.Roots.Anchors.Anchor(2)

on LoadHigh:
    Plant.Stems.Main.Grow(1)
    Plant.Stems.Main.Thickness = Thick
    Plant.Branches.Braces.SupportWeight(All)

on Dry:
    Policies.Stomata.Mode = Conservative
    Plant.Branches.Braces.Hold()

on Damage:
    Plant.Stems.Main.Heal(1)
    Plant.Roots.Anchors.Grow(1)
```

Explanation:

- `Recipes.BraceVine.Apply()` is especially important here because it can define recipe-owned handles such as `Plant.Stems.Main`, `Plant.Branches.Braces`, and `Plant.Roots.Anchors`.
- The signals turn structural support into a reactive system: detect support, detect strain, detect drought, detect damage.
- The `flow` lines express resource routing as a policy instead of a series of manual per-tick storage calls.
- `on SupportSeen` attaches, extends braces, and strengthens anchors as a single coordinated response.
- `on LoadHigh` thickens and reinforces the plant only when stress demands it.
- `on Dry` pauses aggressive brace growth, which prevents the structure-solving behavior from blindly killing the organism.
- `on Damage` enables automatic recovery and re-anchoring, which is exactly the sort of repeated repair behavior that benefits from code.

### Complex Example 5: Purifier Reed Bed

Commission: "Clean contaminated runoff, vent fewer toxins into the air, and signal when water is safe."

```growl
Recipes.FilterReed.Apply()

signal Contaminated when Env.Soil.Toxins.Any()
signal AirHazard when Env.Air.Chemicals.Any()
signal Overflow when Stores.Water.IsHigh()
signal Sick when State.Stress > High

flow Plant.Roots.Intake.Water -> Plant.Stems.FilterBed
flow Plant.Stems.FilterBed -> Products.FilterResin when Contaminated

on Contaminated:
    Plant.Roots.Intake.AbsorbFiltered(["H2O"])
    Plant.Roots.Intake.Exude("binding_agent", 1)
    Products.FilterResin.Produce(1)

on AirHazard:
    Plant.Leaves.Vents.FilterGas("toxin", "block")
    Plant.Leaves.Vents.Coating = Waxy

on Overflow:
    Products.CleanWaterSignal.Emit(1)

on Sick:
    Plant.Defense.ResistDisease("all", 2)
    Plant.Defense.Fever(1)
    Plant.Defense.QuarantineDamaged()
```

Explanation:

- `Recipes.FilterReed.Apply()` defines the specialized intake, vent, filter, and signal handles that this commission needs.
- The four signals break the problem into understandable operating modes: polluted water, polluted air, full storage, and organism distress.
- The `flow` rules make purification feel like building a living processing pipeline rather than manually toggling production steps.
- `on Contaminated` shifts the root system into filtered intake and byproduct production.
- `on AirHazard` changes how the leaf/vent surface behaves so the plant solves both water and air problems together.
- `on Overflow` lets the plant tell the player or client that safe water is available.
- `on Sick` adds a self-protection routine, which is important for complex commissions where the environment fights back.

These examples show the kind of Growl behavior that should feel rewarding to write:

- recurring responses to changing conditions
- coordinated actions across multiple part groups
- recipe-defined handles that make scripts readable
- automation that would be tedious to configure with a purely menu-driven UI

## Design Benefits

This mix should improve Growl in the following ways:

- less decimal clutter
- more readable scripts
- easier onboarding
- clearer morphology language
- preserved strategic control
- preserved low-level escape hatches for advanced players

It should still feel like programming, but more like designing an organism and less like hand-authoring simulation constants.

It should also preserve the feeling that the player is working with named objects in a system, not filling out a static config file.

## Non-Goal

This authoring surface should not remove the current low-level APIs.

Advanced players should still be able to write:

```growl
leaf.open_stomata(0.32)
stem.grow_thick(0.20)
photo.set_glucose_storage_bias(0.70)
```

The mixed authoring surface is a cleaner default, not a hard replacement.

## Guiding Principle

Words should define the plant's character.

Integers should define count, emphasis, and timing.

Decimals should mostly disappear from ordinary Growl source and remain part of lowering and runtime balance.
