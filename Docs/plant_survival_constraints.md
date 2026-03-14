# Plant Survival Constraints

## Purpose

This document defines the baseline survival requirements for every plant in Compile and Grow. These constraints exist to make organism design meaningful: every organ the player adds has a cost, every resource has a tension, and every design choice has a tradeoff.

The constraints should be:

- biologically grounded (real plants face these pressures)
- mechanically clear (the player can understand why their plant is struggling)
- design-forcing (they push the player toward interesting decisions, not busywork)

## The Three Non-Negotiables

Every plant must have:

1. At least one photosynthetic surface (a leaf or equivalent)
2. At least one water-absorbing surface (a root or equivalent)
3. A structural connection between them (a stem)

If any of these is missing, the plant cannot photosynthesize and will die. This is the absolute floor. The game should not allow a plant to exist without all three, and recipes/bodyplans should always provide them.

A fourth implicit requirement: stomata must be at least partially open for CO2 to enter the leaf. A plant with fully closed stomata is alive but starving — it can survive on stores for a while, but it cannot produce new glucose.

## The Core Resource Loop

Phase 1 has five resources:

- **Light** — environmental, captured by leaves, not storable
- **H2O** — absorbed by roots, lost through stomata, storable in stems
- **CO2** — enters through stomata, environmental, not storable
- **Energy** — intermediate product of photosynthesis, semi-storable
- **Glucose** — the universal currency, storable, consumed by everything

The production formula follows Liebig's law of the minimum:

```
glucose produced = min(light captured, water available, CO2 intake) × efficiency
```

Production is limited by whichever input is scarcest. This is already exposed via `photo.get_limiting_factor()`. It means the player always has a weakest link and must decide whether to fix it or accept the limit.

This single rule drives most of the interesting gameplay. A plant with huge leaves but tiny roots will be water-limited. A plant with open stomata in a drought will crash. The player's job is to balance inputs so that no single factor chokes the whole system.

## Maintenance Cost: The Scaling Tax

Every organ costs glucose per tick to stay alive. This is the survival constraint that makes organism design a real optimization problem rather than "just add more organs."

Suggested cost tiers:

| Organ type | Maintenance cost | Why |
|---|---|---|
| Stems | Low | Mostly structural tissue, low metabolic activity |
| Roots | Moderate | Active absorption requires energy |
| Leaves | Moderate | Active photosynthetic tissue, constant gas exchange |
| Flowers / fruit | High | Reproductive and product organs are metabolically expensive |

The maintenance floor is the sum of all organ costs. It represents the minimum glucose income the plant needs just to stay alive, before any growth, storage, or product output.

**The scaling dynamic:** Each organ the player adds raises the maintenance floor. A plant with 6 leaves and 4 roots earns more glucose than one with 2 leaves and 1 root, but it also spends more per tick just existing. There is always a point where adding another organ no longer pays for itself — diminishing returns on organ investment.

This creates the core economic question of the game: **how big should my plant be?** A small plant is cheap to run but earns little. A large plant earns a lot but is expensive and fragile if conditions change. The right size depends on the environment and the commission.

## Water Balance

Water is the resource that creates the most interesting moment-to-moment tension, because it connects leaves and roots through a constraint the player must actively manage.

```
water in  = root absorption rate × root capacity
water out = transpiration rate × stomata openness × leaf surface area
net water = water in − water out
```

If net water is negative, the plant draws from water stores. If water stores run dry, the plant's stomata are forced partially closed to prevent desiccation. This reduces CO2 intake, which reduces photosynthesis, which reduces glucose income.

This is the stomata dilemma:

- **Open stomata** → more CO2 → more photosynthesis → more glucose
- **Open stomata** → more water loss → need more/bigger roots → higher maintenance cost
- **Closed stomata** → less water loss → but less CO2 → less photosynthesis → less glucose

The player cannot fully resolve this tension. They can only choose where on the spectrum to sit, and the optimal position changes with the environment. A wet environment rewards open stomata. A dry environment rewards conservation. A variable environment rewards reactive code.

## Structural Load

Structural support should be a real constraint, not just a visual property.

Each stem or trunk has a support capacity determined by its thickness. Every organ attached above that point contributes load. If load exceeds capacity, the plant risks breakage — losing organs it invested in.

```
support capacity = stem thickness × material factor
current load     = sum of organ weights attached above this point
```

This creates the height dilemma:

- **Taller plants** get better light access, especially when competing with neighbors
- **Taller plants** need thicker stems, which cost more maintenance glucose
- **Top-heavy plants** (many leaves on a thin stem) risk structural failure

The structural constraint means the player can't just stack leaves infinitely. Height is an investment with real costs, and the player must decide whether the light advantage is worth the structural tax.

Suggested rule: structural failure should damage or detach the heaviest organ above the failure point, not kill the whole plant. This makes overbuilding punishing but recoverable.

## The Five Core Tradeoffs

These are the decisions that should feel interesting every time the player designs a plant. Each one is a genuine dilemma with no single correct answer — the right choice depends on environment, commission, and strategy.

### 1. Leaves vs. Water Demand

Each leaf is both a solar panel and a water leak. More leaves capture more light and produce more glucose, but they also lose more water through transpiration. The player must balance leaf area against root capacity, or accept water stress.

A player who adds leaves without adding roots will see `limiting_factor = water`. A player who adds roots without adding leaves will see `limiting_factor = light`. The metrics teach the tradeoff.

### 2. Stomata Openness vs. Water Conservation

This is the most immediate, tick-by-tick tension. Open stomata maximize CO2 intake (and therefore glucose production), but they also maximize water loss. In stable wet conditions, running open stomata is clearly correct. In drought, it's clearly wrong. In variable conditions, the player must write code that adapts.

This tradeoff is why reactive Growl is more powerful than static configuration. A plant that responds to conditions will always outperform one that doesn't.

### 3. Root Investment vs. Maintenance Budget

Roots solve the water problem but create a maintenance problem. Each root costs glucose per tick. A plant with many large roots will never be water-limited, but its maintenance floor will be high, leaving less glucose for growth, storage, or products.

The player must decide: how much water security is worth the cost? A minimalist root system is efficient in good conditions but fragile. A robust root system is resilient but expensive.

### 4. Growth vs. Storage

Every unit of glucose can be spent on growth (bigger plant, more future income) or saved in storage (buffer against bad conditions). This is the classic invest-vs-save dilemma.

- **Growth-first** plants get big fast and out-earn conservative plants in stable conditions. But they have no reserves. One drought can kill them.
- **Storage-first** plants are resilient and can survive extended stress. But they grow slowly and may miss opportunities.

The storage priority settings (`Stores.Glucose.Priority`, etc.) are how the player expresses this tradeoff in code.

### 5. Size vs. Efficiency

A large plant with many organs has a higher gross income than a small plant. But it also has a higher maintenance floor. **Net income** (gross minus maintenance) is what actually matters.

A small, efficient plant with 2 well-placed leaves might have higher net income than a sprawling plant with 6 leaves that barely cover their own maintenance cost. The player should discover this through play, not be told.

This tradeoff also means that environmental downturns hit large plants harder. A plant that was profitable at 6 leaves might become unprofitable if light drops, because its maintenance cost hasn't changed but its income has.

## Failure Modes

Failure should be gradual, visible, and recoverable — not instant death. The player should always have a chance to react and should always understand what went wrong.

### Water Deficit Cascade

1. Water intake falls below water loss
2. Water stores begin draining
3. When stores reach a low threshold, stomata partially close automatically
4. Reduced stomata → reduced CO2 → reduced photosynthesis
5. Growth stalls, glucose income drops
6. If sustained: leaves begin to wilt (reduced efficiency), oldest leaves may drop
7. If the player adds roots, opens water sources, or reduces leaf area, recovery begins

### Glucose Deficit Cascade

1. Glucose income falls below maintenance cost
2. Glucose stores begin draining
3. When stores reach a low threshold, growth halts entirely
4. If stores reach zero, the plant begins shedding organs — oldest and most expensive first
5. Each shed organ reduces maintenance cost, which may restore balance
6. The plant stabilizes at a smaller size, or if it can't shed enough, it dies

### Structural Failure

1. Load on a stem exceeds its support capacity
2. The heaviest or highest organ above that point detaches or takes damage
3. The plant loses the investment but remains alive
4. The player can prevent this by thickening stems before adding weight

### Light Starvation

1. Available light drops below what leaves can use (shade, competition, time of day)
2. Photosynthesis drops proportionally
3. If glucose income falls below maintenance, the glucose deficit cascade begins
4. This is especially relevant in competitive multi-plant scenarios

Each cascade follows the same pattern: a resource imbalance → draw from stores → stores deplete → automatic protective response → if sustained, organ loss → possible recovery or death. The player can intervene at any point in the cascade.

## The Minimum Viable Plant

The simplest plant that can survive:

```growl
Plant.Stems.Main.Size = Small
Plant.Stems.Main.Thickness = Thin

Plant.Roots.Tap.Size = Small
Plant.Roots.Tap.AttachTo(Plant.Stems.Main)

Plant.Leaves.Single.Count = 1
Plant.Leaves.Single.Size = Small
Plant.Leaves.Single.AttachTo(Plant.Stems.Main)
Plant.Leaves.Single.Stomata = Balanced
```

This plant photosynthesizes. It won't be efficient, productive, or resilient. But it meets all survival requirements:

- one leaf for light capture and CO2 intake
- one root for water absorption
- one stem connecting them
- stomata open enough for gas exchange

The minimum viable plant is the starting point. Everything the player does from here is optimization: adding organs to increase income, adjusting stomata to balance water, storing glucose for resilience, growing taller for better light.

## How Constraints Shape the Beginner Organisms

The six beginner organisms from Phase 1 should each represent a different answer to the same survival constraints. They are not arbitrary presets — they are strategic positions on the tradeoff landscape.

### Basic Plant

A balanced answer to all constraints. Moderate leaves, moderate roots, balanced stomata. No extreme strength, no extreme weakness. Good for learning, mediocre at any specific commission.

Tradeoff position: center of every spectrum.

### Water Saver

Prioritizes water security over glucose income. Few small leaves, conservative stomata, large deep root, swollen stem for water storage. Low income but very resilient to drought. Wins in dry, variable environments.

Tradeoff position: conservation over production, storage over growth.

### Fast Grower

Prioritizes glucose income and growth speed over everything. Many leaves, open stomata, minimal storage. High income in good conditions, but fragile — one drought or light drop can trigger the glucose deficit cascade because there are no reserves.

Tradeoff position: maximum growth, minimum safety margin.

### Storage Plant

Prioritizes resilience over growth. Swollen stems, high glucose storage priority, moderate organs. Grows slowly but can survive extended stress. Useful for environments with unpredictable disruptions.

Tradeoff position: maximum buffer, moderate income.

### Light Tracker

Optimizes light capture efficiency rather than raw leaf area. Tracks light, moderate leaf count. Gets more glucose per leaf than a static plant, which means lower maintenance cost for the same income.

Tradeoff position: efficiency over scale.

### Simple Adaptive Plant

Uses conditional Growl to shift between strategies based on conditions. Opens stomata when water is abundant, closes them when it's scarce. Grows when glucose is high, stores when it's low. More complex to write but outperforms any static strategy in variable environments.

Tradeoff position: dynamic — moves across the landscape as conditions change.

## The Design Test

A well-designed constraint system should pass these tests:

1. **The minimum viable plant should be boring.** It works, but it's clearly not optimized. The player should immediately see room for improvement.

2. **Adding an organ should feel like a real decision.** Not "more is always better," but "is the income from this leaf worth the maintenance cost and water demand?"

3. **Two different valid designs should produce different outcomes.** A water saver and a fast grower placed in the same environment should perform measurably differently, and neither should be strictly better.

4. **Environmental changes should shift which designs are optimal.** A plant that thrives in wet conditions should struggle in dry ones, and vice versa. This is what makes reactive code valuable.

5. **Failure should teach.** When a plant starts failing, the metrics (`limiting_factor`, `glucose per tick`, `water efficiency`) should tell the player exactly what went wrong and suggest what to fix.

6. **The constraints should create the beginner organisms, not the other way around.** If you hand someone the constraints and say "build a water-efficient plant," they should naturally converge on something that looks like the Water Saver preset. The presets should be discoveries, not inventions.
