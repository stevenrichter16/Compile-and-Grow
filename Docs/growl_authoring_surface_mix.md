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

## Reactive Conditions

Growl uses the `when` keyword for all reactive behavior. Conditions come in two tiers:

### Built-In Conditions

The game ships with named conditions that are always available. These are precalculated by the simulation based on the plant's actual state — `Dry` means water is below what the plant needs to sustain its current configuration, not an arbitrary number.

Examples of built-in conditions: `Dry`, `Starving`, `Night`, `Day`, `Damaged`, `Sick`.

```growl
when Dry:
    Policies.Stomata.Mode = Conservative

when Starving:
    Growth.Halt()
```

The player doesn't need to understand the threshold math to use these. They're the on-ramp that lets beginners write reactive code without understanding the full resource model.

### Player-Defined Conditions

For finer control, the player defines custom conditions and stores them in variables. This keeps the `when` blocks clean and readable — the condition name describes what it means, the variable definition specifies the exact threshold.

```growl
HarshLight = Env.Light.Intensity > High
LowCO2HighLoad = CO2 < 20 and State.Stress > Medium
ReadyToFruit = Stores.Glucose.IsHigh() and Plant.Leaves.Canopy.Count >= 3

when HarshLight:
    Plant.Leaves.Awning.TrackLight(True)

when LowCO2HighLoad:
    Plant.Leaves.Canopy.ShedOldest(1)

when ReadyToFruit:
    Plant.Fruit.Cluster.Grow(1)
```

Named variables also support reuse — the same condition can appear in multiple `when` blocks or in `flow` rules without repeating the threshold logic. As the player's programs grow in complexity, this keeps the code maintainable.

The progression: beginners use built-in conditions → intermediate players define custom conditions inline → advanced players name and store conditions as variables for reuse across complex programs.

## Reactive / Dataflow Commission Examples

Reactive Growl is useful when the player is writing plant behavior that keeps running and adapting on its own once deployed. Since the player cannot intervene mid-run, `when` blocks are the only way to handle changing conditions.

The point of these examples is not just syntax. The point is that the player is writing autonomous plant intelligence.

### Simple Example 1: Drought Windowbox Crop

Commission: "Make a food crop for rooftop trays with inconsistent watering."

```growl
Recipes.BasicCrop.Apply()

when Dry:
    Policies.Stomata.Mode = Conservative
    Policies.Storage.GlucosePriority = 3

when Metrics.RootSupply > Strong:
    Plant.Roots.Foragers.Absorb("H2O")
    Plant.Leaves.Canopy.TrackLight(True)

when Stores.Glucose.IsHigh():
    Plant.Fruit.Cluster.Grow(1)
```

Explanation:

- `Recipes.BasicCrop.Apply()` gives the player a viable starter organism and defines named handles such as `Plant.Roots.Foragers`, `Plant.Leaves.Canopy`, and `Plant.Fruit.Cluster`.
- `when Dry:` uses the built-in `Dry` condition — the game knows the plant's water is below what it needs. No threshold math required.
- The second `when` uses an inline condition for a more specific check. The player could also store this in a variable (see Reactive Conditions section).
- `when Stores.Glucose.IsHigh()` turns surplus glucose into fruit growth, so the commission becomes a behavior program instead of a fixed growth preset.

### Simple Example 2: Self-Shading Window Vine

Commission: "Keep this west-facing room cooler in the afternoon without killing the plant."

```growl
Recipes.WindowVine.Apply()

HarshLight = Env.Light.Intensity > High

when HarshLight:
    Plant.Leaves.Awning.TrackLight(True)
    Plant.Leaves.Awning.Grow(1)

when Dry:
    Policies.Stomata.Mode = Conservative

when Metrics.RootSupply.IsStrong() and Stores.Glucose.IsHigh():
    Plant.Branches.Awning.Grow(1)
```

Explanation:

- `Recipes.WindowVine.Apply()` defines a vine-oriented plant shape and handles such as `Plant.Leaves.Awning` and `Plant.Branches.Awning`.
- `HarshLight` is a player-defined condition stored in a variable. This makes `when HarshLight:` as readable as a built-in, while giving the player full control over the threshold.
- `when Dry:` uses the built-in condition — no custom threshold needed for basic drought response.
- The final `when` clause ties structural expansion to healthy root supply and sugar reserves, so the vine only spreads when it can afford to.

### Simple Example 3: Night Marker Moss

Commission: "Mark a safe path after dusk, but stay dim in daytime."

```growl
Recipes.GlowMoss.Apply()

when Night:
    Products.MarkerGlow.Emit(1)
    Policies.Stomata.Mode = Balanced

when Day:
    Products.MarkerGlow.Stop()
    Plant.Surface.Patch.Camouflage = Dark

when Dry:
    Policies.Stomata.Mode = Conservative
```

Explanation:

- `Recipes.GlowMoss.Apply()` defines the core moss structure plus a product/output handle such as `Products.MarkerGlow`.
- `Night`, `Day`, and `Dry` are all built-in conditions. The player doesn't need to define thresholds — the game knows what these mean. This makes the code read almost like English.
- `when Night:` activates the marker output and keeps the plant operational.
- `when Day:` shuts the effect off and returns the surface to a less conspicuous state.
- `when Dry:` adds a separate water-preservation rule, which shows how multiple concerns layer cleanly with `when` blocks.

### Complex Example 4: Structural Brace Vine

Commission: "Climb a damaged catwalk, brace weak sections, and thicken if the structure starts shifting."

```growl
Recipes.BraceVine.Apply()

SupportSeen = Plant.Stems.Main.TouchingSupport()
LoadHigh = State.Stress > Medium

flow Stores.Glucose -> Plant.Stems.Main when SupportSeen
flow Stores.Water -> Plant.Branches.Braces when LoadHigh

when SupportSeen:
    Plant.Stems.Main.AttachTo("catwalk_rail")
    Plant.Branches.Braces.GrowToward(Support)
    Plant.Roots.Anchors.Anchor(2)

when LoadHigh:
    Plant.Stems.Main.Grow(1)
    Plant.Stems.Main.Thickness = Thick
    Plant.Branches.Braces.SupportWeight(All)

when Dry:
    Policies.Stomata.Mode = Conservative
    Plant.Branches.Braces.Hold()

when Damaged:
    Plant.Stems.Main.Heal(1)
    Plant.Roots.Anchors.Grow(1)
```

Explanation:

- `Recipes.BraceVine.Apply()` is especially important here because it can define recipe-owned handles such as `Plant.Stems.Main`, `Plant.Branches.Braces`, and `Plant.Roots.Anchors`.
- `SupportSeen` and `LoadHigh` are player-defined conditions stored in variables. They read like built-ins in `when` blocks but the player controls the exact thresholds.
- `Dry` and `Damaged` are built-in conditions. Mixing built-ins with player-defined conditions in the same program is natural — no syntactic difference.
- The `flow` lines express resource routing as a policy, using the same named conditions.
- `when SupportSeen:` attaches, extends braces, and strengthens anchors as a single coordinated response.
- `when Dry:` pauses aggressive brace growth, which prevents the structure-solving behavior from blindly killing the organism.
- `when Damaged:` enables automatic recovery and re-anchoring, which is exactly the sort of repeated repair behavior that benefits from code.

### Complex Example 5: Purifier Reed Bed

Commission: "Clean contaminated runoff, vent fewer toxins into the air, and signal when water is safe."

```growl
Recipes.FilterReed.Apply()

Contaminated = Env.Soil.Toxins.Any()
AirHazard = Env.Air.Chemicals.Any()
Overflow = Stores.Water.IsHigh()

flow Plant.Roots.Intake.Water -> Plant.Stems.FilterBed
flow Plant.Stems.FilterBed -> Products.FilterResin when Contaminated

when Contaminated:
    Plant.Roots.Intake.AbsorbFiltered(["H2O"])
    Plant.Roots.Intake.Exude("binding_agent", 1)
    Products.FilterResin.Produce(1)

when AirHazard:
    Plant.Leaves.Vents.FilterGas("toxin", "block")
    Plant.Leaves.Vents.Coating = Waxy

when Overflow:
    Products.CleanWaterSignal.Emit(1)

when Sick:
    Plant.Defense.ResistDisease("all", 2)
    Plant.Defense.Fever(1)
    Plant.Defense.QuarantineDamaged()
```

Explanation:

- `Recipes.FilterReed.Apply()` defines the specialized intake, vent, filter, and condition handles that this commission needs.
- `Contaminated`, `AirHazard`, and `Overflow` are player-defined conditions stored in variables. `Sick` is a built-in. All four use the same `when` syntax.
- The four `when` blocks break the problem into understandable operating modes: polluted water, polluted air, full storage, and organism distress.
- The `flow` rules make purification feel like building a living processing pipeline, using the same named conditions.
- `when Contaminated:` shifts the root system into filtered intake and byproduct production.
- `when AirHazard:` changes how the leaf/vent surface behaves so the plant solves both water and air problems together.
- `when Overflow:` lets the plant tell the player or client that safe water is available.
- `when Sick:` adds a self-protection routine, which is important for complex commissions where the environment fights back.

These examples show the kind of Growl behavior that should feel rewarding to write:

- `when` blocks that respond to changing conditions autonomously
- coordinated actions across multiple part groups
- recipe-defined handles that make scripts readable
- player-defined conditions stored in variables for clarity and reuse
- built-in conditions that let beginners write reactive code immediately

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
