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
- `when AirHazard:` applies a wax coating to vents so the plant solves both water and air problems together.
- `when Overflow:` lets the plant tell the player or client that safe water is available.
- `when Sick:` adds a self-protection routine, which is important for complex commissions where the environment fights back.

These examples show the kind of Growl behavior that should feel rewarding to write:

- `when` blocks that respond to changing conditions autonomously
- coordinated actions across multiple part groups
- recipe-defined handles that make scripts readable
- player-defined conditions stored in variables for clarity and reuse
- built-in conditions that let beginners write reactive code immediately

## Air Toxin Response: Wax Coating vs. Convert

When air toxins are present, the plant has two leaf-level defenses:

- **Wax coating (default):** Leaves with a waxy coating passively prevent toxin absorption. Cheap — costs a small amount of glucose to maintain. The leaf can still photosynthesize normally through the wax, but it does not process the toxins at all. No special method call is needed — the coating itself provides the protection.
- **Convert (unlockable):** The leaf actively converts air toxins into usable gases such as CO2, which the plant then absorbs through normal photosynthesis. Expensive — costs significantly more glucose per tick than wax alone. The payoff is that the converted CO2 feeds back into the plant's carbon economy, so a well-resourced plant can profit from toxic air rather than just surviving it.

### Design Intent

The mechanic creates a resource allocation decision within a single environmental threat. The player is not choosing between "defend" and "don't defend" — they are choosing how much of their canopy to invest in active conversion vs. passive wax coating.

The tradeoff:

- A plant that converts on every leaf burns glucose faster than the CO2 benefit replenishes it, especially under compound stress (drought + toxins).
- A plant that waxes every leaf survives cheaply but misses the CO2 upside.
- The optimal strategy is a ratio: a few leaves or one leaf group converts, while the rest stay waxed. The exact split depends on the plant's glucose reserves, canopy size, and how toxic the air is.

Because `Convert` is an unlockable method, beginners start with wax-only defense (simple, safe) and graduate to the mixed strategy once they understand the glucose economy well enough to manage the cost.

### Example 1: Simple Wax-Only Defense

A beginner's approach — every leaf is waxed, no conversion. Safe and cheap.

```growl
Recipes.FilterReed.Apply()

when AirHazard:
    Plant.Leaves.All.Coating = Waxy
```

### Example 2: Single Converter Group with Waxed Majority

The player splits their canopy into two groups: a small converter set and a larger waxed set. The converters spend glucose to turn toxins into CO2, while the waxed leaves protect cheaply.

```growl
Recipes.FilterReed.Apply()

Plant.Leaves.Converters.Count = 2
Plant.Leaves.Waxed.Count = 4

when AirHazard:
    Plant.Leaves.Converters.Convert("toxin", "CO2")
    Plant.Leaves.Waxed.Coating = Waxy
```

### Example 3: Glucose-Aware Conversion Budget

The player ties conversion to glucose reserves. When stores are healthy, converters run and the plant profits from the extra CO2. When glucose drops, converters fall back to wax coating so the plant doesn't starve itself fighting toxins.

```growl
Recipes.FilterReed.Apply()

Plant.Leaves.Converters.Count = 3
Plant.Leaves.Waxed.Count = 3

CanAffordConversion = Stores.Glucose > Medium and not Starving

when AirHazard and CanAffordConversion:
    Plant.Leaves.Converters.Convert("toxin", "CO2")
    Plant.Leaves.Waxed.Coating = Waxy

when AirHazard and not CanAffordConversion:
    Plant.Leaves.Converters.Coating = Waxy
    Plant.Leaves.Waxed.Coating = Waxy
```

### Example 4: Scaling Conversion With Canopy Size

A more advanced program that dynamically adjusts how many leaves convert based on available surplus. The plant grows into its conversion capacity rather than overcommitting early.

```growl
Recipes.PurifierCanopy.Apply()

Plant.Leaves.Converters.Count = 2
Plant.Leaves.Waxed.Count = 4

GlucoseRich = Stores.Glucose.IsHigh() and Metrics.PhotoRate > Strong
ToxinHeavy = Env.Air.Chemicals.Severity > High

when AirHazard:
    Plant.Leaves.Waxed.Coating = Waxy

when AirHazard and GlucoseRich:
    Plant.Leaves.Converters.Convert("toxin", "CO2")

when AirHazard and ToxinHeavy and GlucoseRich:
    Plant.Leaves.Converters.Grow(1)
    Policies.Storage.GlucosePriority = 3

when AirHazard and Starving:
    Plant.Leaves.Converters.Coating = Waxy
    Plant.Leaves.Converters.ShedOldest(1)
```

The progression across these examples:

1. **Wax-only** — beginners survive toxins without understanding glucose costs
2. **Fixed split** — intermediate players discover the converter unlock and assign a ratio
3. **Budget-aware split** — the player ties conversion to glucose state, so the plant adapts
4. **Dynamic scaling** — advanced players grow their converter capacity when the economy supports it and shed it when it doesn't

## Multi-Stem Growth (Unlockable)

By default, a plant grows with a single stem — one vertical trunk that concentrates the plant's energy upward. The **multi-stem unlock** lets the player grow additional stems from the base, changing the plant's growth profile.

### Core Mechanic

- **Leaf efficiency:** Multiple stems produce more leaves for less glucose than a single stem would need to grow the same number of leaves. Each additional stem branches out with its own leaf sites, so the plant gets more photosynthetic surface per unit of growth energy.
- **Water cost:** Water demand scales linearly with the number of stems. Two stems need twice the water intake of one. Three stems need three times. This is a hard, predictable cost the player can plan around.
- **Height penalty:** Energy is divided among stems rather than focused on a single trunk, so multi-stemmed plants grow shorter. Each stem is individually weaker and slower to gain height than a single-stem plant's trunk.

### Design Intent

The tradeoff is leaf output vs. water budget. A multi-stemmed plant is a better photosynthesizer in water-rich environments — it builds canopy cheaply and floods itself with glucose. But in dry conditions, the water overhead can starve the plant faster than the extra leaves can compensate.

This creates an environmental read: the player looks at the scenario's water availability and decides whether to invest in multi-stem or stay single. It's not a permanent commitment — stems can be grown or shed over the course of a run — but adding stems costs glucose upfront, so constant switching is wasteful.

The height penalty matters if light competition is present. A single-stem plant can reach higher light tiers that multi-stem plants cannot access. In open environments with no shading, height doesn't matter and multi-stem is strictly better (if water allows). In crowded canopies, the player has to weigh cheap leaves against reachable light.

### Interaction With Other Systems

- **Toxin response:** More leaves means more surface area to wax during air toxin events. The glucose cost of maintaining wax coatings across a large multi-stem canopy can add up. Players who unlock Convert can offset this by turning a subset of their abundant leaves into converters.
- **Structural risk:** Multiple stems are individually weaker. If storm or wind mechanics are present, a multi-stem plant may lose individual stems but survive through redundancy. A single-stem plant is sturdier but has no backup if its trunk is damaged.

### Example 1: Basic Multi-Stem Setup

The player unlocks multi-stem and grows a bushy, water-hungry plant with high leaf output.

```growl
Plant.Stems.Count = 3
Plant.Stems.Growth = Outward

when Wet:
    Plant.Stems.GrowNew(1)

when Dry:
    Plant.Stems.ShedWeakest(1)
```

### Example 2: Adaptive Stem Count

The player ties stem count to water availability, growing stems when water is plentiful and shedding them when it drops.

```growl
Plant.Stems.Count = 2

WaterRich = Stores.Water.IsHigh() and Env.Soil.Moisture > Damp
WaterTight = Stores.Water < Low or Env.Soil.Moisture < Dry

when WaterRich:
    Plant.Stems.GrowNew(1)
    Plant.Leaves.All.Coating = None

when WaterTight:
    Plant.Stems.ShedWeakest(1)
```

### Example 3: Multi-Stem With Toxin Management

A multi-stem plant that takes advantage of its large canopy during toxin events — waxing most leaves cheaply while running a small converter group.

```growl
Plant.Stems.Count = 4
Plant.Stems.Growth = Outward

Plant.Leaves.Converters.Count = 2
Plant.Leaves.Waxed.Count = Plant.Leaves.Total - Plant.Leaves.Converters.Count

CanAffordConversion = Stores.Glucose > Medium and not Starving

when AirHazard and CanAffordConversion:
    Plant.Leaves.Converters.Convert("toxin", "CO2")
    Plant.Leaves.Waxed.Coating = Waxy

when AirHazard and not CanAffordConversion:
    Plant.Leaves.Converters.Coating = Waxy
    Plant.Leaves.Waxed.Coating = Waxy

when WaterTight:
    Plant.Stems.ShedWeakest(1)
```

The progression:

1. **Single stem (default)** — beginners grow tall with predictable water costs
2. **Fixed multi-stem** — intermediate players unlock multi-stem and pick a count
3. **Adaptive stem count** — the player ties stem growth to water state so the plant responds to conditions
4. **Multi-stem with layered systems** — advanced players combine multi-stem's leaf abundance with toxin conversion and environmental reads

## Light Levels (Commission-Defined)

Light is the primary resource that makes height matter. Every commission defines its own **light profile** — four light levels (0–3) mapped to height bands that change from commission to commission. This means "grow tall" is not universally correct. The player has to read the environment and place their leaves in the right band.

### Core Mechanic

- **Four light levels (0–3).** Level 3 is the brightest, level 0 is the dimmest. The levels always mean the same thing in terms of photosynthesis output — a leaf at level 3 always produces more glucose than a leaf at level 1. What changes is *where* those levels sit in the vertical space.
- **Commission-defined height bands.** Each commission assigns light levels to height ranges. A forest-floor commission might put level 3 near the ground (sunlight through a canopy gap) and level 0 at the top (dense canopy shade). A meadow commission might stack them conventionally with level 3 at the top. The player sees the light profile before the run starts and plans accordingly.
- **Photosynthesis scales with light level.** Leaves at higher light levels produce more glucose per tick. This is passive — the player doesn't control it directly, they control it by choosing where to grow. A leaf at level 3 might produce 3x the glucose of a leaf at level 0. The exact multiplier is a balance knob, but the relationship is always "higher level = more output."

### Light Profile Examples

A conventional open-sky environment where light increases with height:

```
Commission: "Open Meadow"
Light Profile:
  Level 3: 15m+       (full sun, open sky)
  Level 2: 10m – 15m  (bright, minimal obstruction)
  Level 1: 5m – 10m   (partial shade from neighbors)
  Level 0: 0m – 5m    (ground shade)
```

An inverted forest-floor environment where a canopy gap lets light hit the ground:

```
Commission: "Forest Floor Recovery"
Light Profile:
  Level 3: 0m – 2m    (clearing gap, full sun)
  Level 2: 2m – 5m    (partial shade)
  Level 1: 5m – 12m   (heavy canopy shade)
  Level 0: 12m+       (dense upper canopy, almost no light)
```

A cliff face where light only hits a narrow band:

```
Commission: "Cliff Face"
Light Profile:
  Level 3: 3m – 6m    (direct sun hits the rock face here)
  Level 2: 0m – 3m    (reflected light from the ground)
  Level 1: 6m – 10m   (shadowed by overhang)
  Level 0: 10m+       (deep overhang shadow)
```

### Interaction With Multi-Stem

Light profiles are the main reason the player can't default to one growth strategy across all commissions.

- **Conventional profiles (light at the top):** Single-stem plants dominate. They reach the high bands where glucose production is strongest. Multi-stem plants spread their energy across shorter stems and their leaves sit at lower, dimmer levels. Multi-stem can still work here if water is abundant and the sheer leaf count at level 1–2 outproduces fewer leaves at level 3, but it's an uphill fight.
- **Inverted profiles (light at the bottom):** Multi-stem plants dominate. Their wide, low canopy sits squarely in the brightest band. A single-stem plant grows through the good light and into the dark. The player either stays short (wasting single-stem's height advantage) or goes tall and loses photosynthesis output.
- **Banded profiles (light in the middle):** Both strategies are viable. Single-stem can target the band precisely. Multi-stem can spread across it with more leaves. The player picks based on water budget and commission goals.

This means the player has to re-read the light profile on every commission and adjust their Growl script. A script that dominates one commission may be terrible for the next.

### Commission Requirements

Some commissions require the plant to have leaves at a specific light level, independent of photosynthesis. These are explicit goals the player must meet to complete the commission.

```
Commission: "Canopy Restoration"
Objective: Maintain at least 4 leaves at Light Level 3 for 30 ticks
Light Profile:
  Level 3: 20m+
  Level 2: 12m – 20m
  Level 1: 5m – 12m
  Level 0: 0m – 5m
```

This commission is hard for multi-stem plants — reaching 20m with multiple stems requires heavy glucose investment in a single dominant stem while keeping the others short. The player might use a focused growth command:

```growl
Plant.Stems.Count = 3
Plant.Stems.Leader = Tallest
Plant.Stems.Leader.Priority = High

when Plant.Stems.Leader.Height < 20:
    Plant.Stems.Leader.GrowUp(2)
    Plant.Stems.Others.GrowUp(0)

when Plant.Stems.Leader.Height >= 20:
    Plant.Stems.Others.GrowOut(1)
```

### Visibility

The light profile must be visible to the player at two points:

1. **Commission select screen.** The player sees the light profile as part of the commission briefing before committing. This lets them plan their script.
2. **During the run.** A vertical bar on the HUD shows the four light bands with the plant's current height marked. The player can see which band their leaves occupy in real time.

This prevents the "discovered mid-run" frustration. The light profile is part of the puzzle, not a hidden gotcha.

### Example 1: Script for an Inverted Light Commission

The player reads the light profile, sees that light level 3 is near the ground, and writes a wide, low multi-stem build.

```growl
Plant.Stems.Count = 4
Plant.Stems.Growth = Outward
Plant.Stems.MaxHeight = 2

when Wet:
    Plant.Stems.GrowNew(1)

when Plant.Stems.Any.Height > 2:
    Plant.Stems.TrimTo(2)
```

### Example 2: Script for a Banded Light Commission

Light level 3 is in a narrow band (3m–6m). The player uses a moderate stem count and targets the band precisely.

```growl
Plant.Stems.Count = 2
Plant.Stems.Growth = Upward

TargetMin = 3
TargetMax = 6

when Plant.Stems.Tallest.Height < TargetMin:
    Plant.Stems.All.GrowUp(1)

when Plant.Stems.Tallest.Height > TargetMax:
    Plant.Stems.All.GrowUp(0)
    Plant.Leaves.All.GrowOut(1)
```

### Example 3: Adaptive Light Targeting

An advanced player writes a script that reads the light level at the plant's current height and adjusts growth direction accordingly.

```growl
Plant.Stems.Count = 2

CurrentLight = Env.Light.AtHeight(Plant.Stems.Tallest.Height)
LightAbove = Env.Light.AtHeight(Plant.Stems.Tallest.Height + 2)

when LightAbove > CurrentLight:
    Plant.Stems.Leader.GrowUp(2)

when LightAbove < CurrentLight:
    Plant.Stems.All.GrowOut(1)
    Plant.Stems.All.GrowUp(0)

when LightAbove == CurrentLight:
    Plant.Leaves.All.GrowOut(1)
```

The progression:

1. **Read the profile, pick a height** — beginners look at the commission's light profile and aim for a fixed height
2. **Match stem strategy to profile** — intermediate players choose single-stem or multi-stem based on where the light is
3. **Target the band precisely** — the player caps height and spreads leaves within the brightest zone
4. **Adaptive light chasing** — advanced players query light at runtime and steer growth toward the best available level

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
