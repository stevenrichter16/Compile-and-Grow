# Plant Survival Constraints

## Purpose

This document defines the baseline survival requirements for every plant in Compile and Grow. These constraints exist to make organism design meaningful: every organ the player adds has a cost, every resource has a tension, and every design choice has a tradeoff.

The constraints should be:

- biologically grounded (real plants face these pressures)
- mechanically clear (the player can understand why their plant is struggling)
- design-forcing (they push the player toward interesting decisions, not busywork)

## Execution Model

The player writes a Growl program, compiles it, and deploys it into a chosen environment. Once deployed, the player cannot intervene. The plant runs autonomously — its code is its only intelligence. The player observes the run, learns from what happened, and iterates on the code for the next deployment.

This means:

- **The plant's code must handle everything.** There is no pause button, no mid-run stomata slider, no emergency root-growing UI. If the player didn't write drought-response logic, the plant dies in a drought. This is the core design pressure that makes reactive Growl (`when`) essential rather than optional.
- **Failure stages serve two purposes.** During the run, they give the plant's own reactive code time to detect and respond to problems. After the run, they give the player observable events to learn from. Both purposes matter.
- **Stores buy time for the code, not the player.** A storage plant is valuable because its reactive logic has more ticks to detect a problem and adapt before the cascade reaches Stage 3. A zero-storage plant's code must react immediately or the plant starts shedding.
- **The iteration loop is where player skill lives.** The player's craft is in writing code that handles conditions they anticipate — and survives conditions they didn't. Each deployment teaches the player something. The quality of the reactive code IS the player's skill expression.

The game's fun loop is: **design → deploy → observe → iterate.** The constraints in this document create the pressure that makes each phase of that loop meaningful.

## The Three Non-Negotiables

Every plant must have:

1. At least one photosynthetic surface (a leaf or equivalent)
2. At least one water-absorbing surface (a root or equivalent)
3. A structural connection between them (a stem)

If any of these is missing, the plant cannot photosynthesize and will die. This is the absolute floor. The game should not allow a plant to exist without all three, and recipes/bodyplans should always provide them.

A fourth implicit requirement: stomata must be at least partially open for CO2 to enter the leaf. A plant with fully closed stomata is alive but starving — it can survive on stores for a while, but it cannot produce new glucose.

## The Core Resource Loop

Phase 1 has four player-facing resources and one derived input:

- **Light** — environmental, captured by leaves, not storable
- **H2O** — absorbed by roots, lost through stomata, storable in stems
- **Energy** — intermediate product of photosynthesis, semi-storable
- **Glucose** — the universal currency, storable, consumed by everything
- **CO2** — enters through stomata, but not independently managed

CO2 is real, but it is not a separate constraint the player thinks about. CO2 intake is entirely determined by stomata openness. When the player opens stomata, CO2 flows in. When they close stomata, CO2 stops. The player manages stomata, and CO2 follows. The limiting factor system can still report `carbon` when stomata are too closed, but the player's response is always "open stomata more" — which brings them back to the stomata/water tradeoff, not to a separate CO2 problem.

The production formula follows Liebig's law of the minimum:

```
glucose produced per leaf = min(light captured, water available, stomata openness) × leaf efficiency
total glucose produced    = sum across all leaves
```

Production is limited by whichever input is scarcest. This is already exposed via `photo.get_limiting_factor()`. It means the player always has a weakest link and must decide whether to fix it or accept the limit.

**Leaf efficiency** is not a single fixed number. It is determined by the leaf's current condition:

- A healthy leaf at full turgor has its base efficiency (determined by size tier)
- A wilting leaf has reduced efficiency (see Failure Modes)
- A self-shaded leaf has reduced effective light input (see Diminishing Returns)
- Light tracking increases effective light capture for that leaf

Efficiency is not a tunable parameter the player sets directly. It is an emergent property of the leaf's situation. The player improves efficiency by keeping leaves healthy, reducing self-shading, and tracking light — not by adjusting a number.

This single rule drives most of the interesting gameplay. A plant with huge leaves but tiny roots will be water-limited. A plant with open stomata in a drought will crash. The player's job is to balance inputs so that no single factor chokes the whole system.

## Maintenance Cost: The Scaling Tax

Every organ costs glucose per tick to stay alive. This is the survival constraint that makes organism design a real optimization problem rather than "just add more organs."

Maintenance cost depends on both organ type and organ size:

| Organ type | Base cost | Size scaling | Why |
|---|---|---|---|
| Stems | Low | Thickness matters more than length | Mostly structural tissue, low metabolic activity |
| Roots | Moderate | Larger roots cost more | Active absorption requires energy |
| Leaves | Moderate | Larger leaves cost more | Active photosynthetic tissue, constant gas exchange |
| Flowers / fruit | High | Larger outputs cost more | Reproductive and product organs are metabolically expensive |

Size must have economic consequences. A Large leaf captures more light than a Small leaf, but it also costs more glucose per tick to maintain and loses more water through transpiration. If size only affected morphology with no cost scaling, the player would always choose Large and the size system would be meaningless as a design choice.

Suggested size multipliers on maintenance cost:

- Small: 0.7×
- Medium: 1.0×
- Large: 1.4×

These don't need to be linear with the production benefit. A Large leaf should produce more than a Small leaf, but whether it's more *efficient* (better net income per maintenance cost) is the design question. Different size tiers could be optimal in different environments — Small leaves might be more water-efficient in drought, Large leaves might be more productive in wet, sunny conditions.

The maintenance floor is the sum of all organ costs. It represents the minimum glucose income the plant needs just to stay alive, before any growth, storage, or product output.

**The scaling dynamic:** Each organ the player adds raises the maintenance floor. But the scaling tax alone is not what creates diminishing returns — the diminishing returns come from self-shading and root competition (see below). Maintenance cost is linear. The *income* from each additional organ is what falls off.

This creates the core economic question of the game: **how big should my plant be?** A small plant is cheap to run but earns little. A large plant earns a lot but is expensive and fragile if conditions change. The right size depends on the environment and the commission.

## Diminishing Returns: Self-Shading and Root Competition

The document's earlier version said "there is always a point where adding another organ no longer pays for itself" but never explained why. The answer is not an arbitrary penalty — it comes from two biological mechanics that the player can discover and reason about.

### Self-Shading

Each leaf captures light. But leaves on the same plant can block each other. The first leaf on a stem gets full sunlight. The second leaf gets slightly less, because the first leaf is already intercepting some of that light. By the fifth or sixth leaf, each additional leaf is mostly in the shadow of the ones above it.

```
effective light per leaf = available light × shading factor
shading factor decreases as total leaf area on the same attachment point increases
```

This means the glucose income from each additional leaf is less than the one before it, while the maintenance cost stays the same. Eventually, a new leaf costs more to maintain than it earns. That's the natural ceiling on leaf count.

Self-shading is also what makes canopy architecture interesting. A plant that spreads its leaves across multiple attachment points (branches) shades itself less than one that stacks all its leaves on a single stem. This gives the player a reason to build branching structures — not because the game tells them to, but because the physics of light capture rewards it.

Light tracking also interacts with self-shading: leaves that track the sun can angle themselves to reduce mutual shading, which is another reason the Light Tracker archetype works differently from raw leaf-count strategies.

### Root Competition

Roots in the same soil zone compete for the same water. The first root in a zone absorbs water freely. The second root in the same zone absorbs less, because the first root is already depleting that area. Adding more roots to the same depth and location gives diminishing water returns.

```
effective absorption per root = soil water availability × competition factor
competition factor decreases as total root area in the same zone increases
```

This creates the depth-vs-breadth tradeoff for roots. Deep roots access a separate water pool (groundwater) and don't compete with shallow roots. Spreading roots horizontally avoids competition more than stacking them vertically. The player can solve root competition through architecture, not just by adding more roots.

**Visibility concern:** Self-shading is visually intuitive — the player can see leaves blocking each other. Root competition happens underground and is invisible. If the player can't see why their 4th root isn't absorbing much, the mechanic feels arbitrary rather than discoverable. The game needs to surface root competition clearly through metrics or visualization:

- `Metrics.Roots.ZoneOverlap` or similar should tell the player when roots are competing
- A soil cross-section view or root map could show water depletion zones
- The limiting factor system should distinguish between "not enough roots" and "roots competing for the same water"

If root competition can't be made visible and legible to the player, it should be simplified or deferred. An invisible constraint that punishes the player without teaching them is worse than no constraint at all.

### Why These Mechanics Matter

Self-shading and root competition are better than arbitrary diminishing return penalties because:

1. **They're discoverable.** The player can look at their plant and reason about why the 5th leaf isn't producing as much as the 1st.
2. **They're solvable.** Branching, spacing, depth choices, and light tracking all reduce the penalties. The player has agency.
3. **They reward architecture over brute force.** A well-designed plant with 3 optimally placed leaves beats a lazy plant with 6 stacked leaves.
4. **They emerge from the simulation.** They don't feel like a game balance number — they feel like physics.

## Water Balance

Water is the resource that creates the most interesting tension, because it connects leaves and roots through a constraint the player's code must manage.

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

The player's code cannot fully resolve this tension. It can only choose where on the spectrum to sit, and the optimal position changes with the environment. A wet environment rewards open stomata. A dry environment rewards conservation. A variable environment rewards reactive code that shifts stomata based on conditions — and since the player cannot intervene mid-run, that reactive code is the only way to handle variability.

## Structural Load (Phase 2+)

Structural support should eventually be a real constraint, not just a visual property. But it is a separate mechanical system from the photosynthesis resource loop and should not be part of Phase 1.

In Phase 1, the player's challenge is balancing light, water, and glucose. Structural mechanics add a fourth system (weight, thickness, breakage) that would dilute the core learning. Self-shading already provides a natural ceiling on leaf stacking without needing structural physics.

When structural load is introduced (Phase 2 or later), the design should follow these principles:

Each stem or trunk has a support capacity determined by its thickness. Every organ attached above that point contributes load. If load exceeds capacity, the plant risks breakage — losing organs it invested in.

```
support capacity = stem thickness × material factor
current load     = sum of organ weights attached above this point
```

This creates the height dilemma:

- **Taller plants** get better light access, especially when competing with neighbors
- **Taller plants** need thicker stems, which cost more maintenance glucose
- **Top-heavy plants** (many leaves on a thin stem) risk structural failure

Suggested rule: structural failure should damage or detach the heaviest organ above the failure point, not kill the whole plant. This makes overbuilding punishing but recoverable.

## The Three Core Tradeoffs

The earlier version of this document listed five tradeoffs, but three of them ("leaves vs. water demand," "root investment vs. maintenance budget," and "size vs. efficiency") were all variations of the same underlying tension: every organ costs maintenance but earns diminishing returns. That's the scaling tax and diminishing returns mechanics already covered above. They don't need to be restated as separate tradeoffs.

The three genuinely distinct tradeoffs are:

### 1. Stomata Openness vs. Water Conservation

This is the most immediate, tick-by-tick tension and the one that most rewards reactive code.

- **Open stomata** → more CO2 → more photosynthesis → more glucose income
- **Open stomata** → more water loss → faster store depletion → closer to wilting

In stable wet conditions, open stomata are clearly correct. In drought, they're clearly wrong. In variable conditions — which is where most interesting commissions live — the player must write code that shifts stomata openness based on water state. This is the tradeoff that makes `when Dry:` feel necessary rather than optional.

A plant whose code never adjusts stomata will either waste water in abundance or starve for CO2 in drought. Reactive code solves both.

### 2. Growth vs. Storage

Every unit of surplus glucose (income minus maintenance) can be spent on growth or saved in storage. This is the invest-vs-save dilemma.

- **Growth-first** plants get big fast and compound their income. More leaves → more glucose → more growth. In stable conditions, they outproduce conservative plants. But they have no reserves. One drought triggers the wilting cascade immediately because there's no buffer.
- **Storage-first** plants grow slowly but can survive extended stress. Their stores buy time during Stage 1 and Stage 2 of any failure cascade. But they take longer to reach full production and may miss opportunities that reward quick scaling.

This tradeoff gains depth because of the failure pacing system. Stores are the pacing mechanism — they determine how long a plant can endure bad conditions before wilting. A growth-first plant with zero stores goes from healthy to wilting in a few ticks. A storage-first plant can ride out a drought that would kill the growth-first plant. But when conditions are stable, the growth-first plant earns more and finishes commissions faster.

### 3. Architecture vs. Brute Force

This tradeoff emerges from self-shading and root competition. The player can try to maximize production by adding more organs (brute force), or by placing fewer organs more effectively (architecture).

- **Brute force:** 6 leaves on one stem, 4 roots in one zone. High maintenance cost, heavy self-shading, root competition. Gross income is high but net income may be low.
- **Architecture:** 3 leaves spread across branches to reduce self-shading, 2 roots at different depths to avoid competition. Lower maintenance cost, each organ earns closer to its full potential. Net income may exceed the brute-force plant despite fewer organs.

This is the tradeoff that makes branching, depth choices, and spatial reasoning matter. It's also why the Light Tracker archetype is interesting — it uses code (light tracking) to solve a spatial problem (self-shading) without needing to build branches.

## How Commissions Exploit These Tradeoffs

Constraints are interesting in the abstract. They become fun when commissions force the player to navigate them under specific pressures.

A well-designed commission should put at least two tradeoffs in tension with each other. Examples:

**"Make a drought-resistant food crop."** This pits stomata conservation (needed for drought survival) against fruit production (which requires glucose, which requires photosynthesis, which requires open stomata). The player must find the stomata position that produces enough glucose for fruit while not hemorrhaging water. Growth-vs-storage matters too: the plant needs enough stored glucose to survive dry spells, but must grow fruit during wet ones.

**"Shade a west-facing window without killing the plant."** This rewards large leaf canopy (for shade coverage) but the plant is in harsh afternoon sun with limited water. More leaves = more shade = more transpiration = more water stress. The player must balance canopy size against water budget, and might use reactive code to track light only during peak hours.

**"Produce maximum glucose in a stable greenhouse."** With stable conditions and abundant water, the stomata tradeoff disappears (just open them). Growth-vs-storage shifts toward pure growth (no drought to buffer against). The commission becomes an architecture problem: how to arrange leaves and roots for maximum net income. This is where self-shading and root competition become the primary constraints.

**"Survive a 50-tick drought with only initial water stores."** Pure survival challenge. Growth is irrelevant. The commission is entirely about stomata management, store sizing, and organ shedding strategy. A Small plant with Swollen stems might outlast a Large plant because its maintenance floor is lower and its stores are deeper.

The pattern: each commission shifts which tradeoffs dominate. The constraints are always present, but different commissions put different ones under pressure.

## Failure Modes

Failure should be gradual, visible, and legible — not instant death. Gradual failure matters for two reasons: it gives the plant's reactive code time to detect and respond to problems during the run, and it gives the player clear observable events to learn from when reviewing the run afterward.

### The Three Stages of Decline

Every failure cascade passes through three stages. This is the most important structural rule for game feel:

**Stage 1: Store Depletion (warning).** The resource balance goes negative. Stores begin draining. Metrics change. The plant is still fully functional, but the trend is visible. During the run, this is where well-written reactive code should detect the problem — `when Dry:` fires here. After the run, this is where the player sees "my code should have caught this earlier."

**Stage 2: Wilting (degraded).** Stores hit a low threshold. The plant enters a visibly degraded state. Wilting leaves have reduced photosynthetic efficiency. Stressed roots have reduced absorption. The plant is weaker but still intact — no organs are lost. If the plant's code includes stress-response logic, it can still stabilize here. This stage must be visually distinct so the player can spot it in the run timeline.

**Stage 3: Shedding (damage).** If wilting persists and stores fully deplete, the plant begins dropping organs — oldest and most expensive first. Each lost organ reduces the maintenance floor, which may restore balance. The plant stabilizes at a smaller size. Shedding is the last resort, not the first response.

### Failure Pacing

How fast a plant moves through these stages matters enormously. The key principle: **stores are the pacing mechanism.** A plant with large glucose and water stores takes longer to move from Stage 1 to Stage 3 than a plant with no stores. This is what makes the Storage Plant archetype valuable — it gives the plant's reactive code more ticks to detect and respond before irreversible damage occurs.

Suggested pacing guidelines:

- Stage 1 (store depletion) should last long enough for reactive code to fire. For a plant with moderate stores, this should be at least 10-20 ticks of sustained deficit. This is the window where `when Dry:` or `when Stores.Glucose.IsLow():` should activate.
- Stage 2 (wilting) should last at least 5-10 ticks before shedding begins. This gives the plant's code a second chance to respond to worsening conditions, and gives the player a visually distinct event to find when reviewing the run.
- Stage 3 (shedding) should happen one organ at a time, not all at once. Each shed event is a distinct moment in the run timeline.

A plant with no stores can go from healthy to shedding quickly. That's the consequence of the growth-over-storage tradeoff. A plant that wrote no reactive code AND has no stores will fail fast — and that's a lesson the player learns by watching the run and iterating.

### Water Deficit Cascade

1. Water intake falls below water loss → net water goes negative
2. Water stores begin draining (Stage 1)
3. When stores reach a low threshold, the plant enters water stress (Stage 2):
   - Stomata partially close automatically to reduce water loss
   - Reduced stomata → reduced CO2 → reduced photosynthesis
   - Leaves visibly wilt — reduced photosynthetic efficiency
   - Growth stalls
4. If water stores fully deplete (Stage 3):
   - Oldest leaves drop first (they lose the most water for the least production)
   - Each dropped leaf reduces water demand, which may restore balance
5. Recovery: if the plant's code responds by shedding leaves, closing stomata further, or redirecting resources, the cascade can stabilize. If the code has no drought logic, the plant continues declining until it either stabilizes through automatic shedding or dies

### Glucose Deficit Cascade

1. Glucose income falls below maintenance cost → net glucose goes negative
2. Glucose stores begin draining (Stage 1)
3. When stores reach a low threshold, the plant enters glucose stress (Stage 2):
   - Growth halts entirely — all glucose goes to maintenance
   - Organs operate at reduced efficiency (slower absorption, slower gas exchange)
   - Plant visibly stalls
4. If glucose stores fully deplete (Stage 3):
   - The plant sheds its most expensive organ first
   - Each shed organ reduces maintenance cost
   - The plant stabilizes at a smaller size, or if it can't shed enough, it dies
5. Recovery: if conditions improve externally (rain returns, light increases) or the plant's code adapts (sheds expensive organs, shifts priorities), the plant rebuilds stores before resuming growth

### Light Starvation

1. Available light drops below what leaves can use (shade, competition, time of day)
2. Photosynthesis drops proportionally
3. If glucose income falls below maintenance, the glucose deficit cascade begins
4. This is especially relevant in competitive multi-plant scenarios where taller neighbors shade shorter plants

### Structural Failure (Phase 2+)

When structural load is introduced, failure follows a simpler pattern: load exceeds capacity → heaviest organ above the failure point detaches or takes damage → plant loses the investment but remains alive.

### Recovery Should Be Fast Once Conditions Improve

When the underlying problem resolves — either because the environment changes (rain returns) or because the plant's reactive code adapts (closes stomata, sheds an expensive organ) — recovery from wilting should be fast. Wilted leaves should bounce back within 2-3 ticks once water or glucose balance is restored.

Slow recovery would punish the player's plant even after its code successfully handled the crisis. The real cost of failure is the lost production during wilting and any organs shed during Stage 3. Those are lasting consequences. Making the plant stay degraded after the crisis has passed adds no interesting gameplay — it just makes a successful adaptation feel unrewarding to watch.

Shed organs are the permanent cost. They must be regrown from scratch, which takes glucose and ticks. That's what makes prevention (good initial design + early reactive logic) more valuable than recovery (shedding and rebuilding). The player sees this in the run timeline: "my plant wilted at tick 35, shed two leaves at tick 42, recovered at tick 45, but then spent ticks 45-80 regrowing what it lost. Next iteration, I'll trigger `when Dry:` at a higher water threshold so it never reaches Stage 3."

### Shed Material as a Resource

Organ shedding should not be pure loss. In real biology, fallen leaves decompose into soil nutrients. Dead roots leave organic matter that improves soil structure. Plants that drop organs are recycling material, not destroying it.

In gameplay terms, shed organs should return something to the plant:

**Nutrient recovery from shed organs.** When a leaf drops or a root is shed, a fraction of its glucose investment should be recovered — either returned to glucose stores directly (reabsorption before shedding, which real plants do) or deposited into the soil as organic matter that roots can later reclaim.

Suggested recovery rate: 20-40% of the organ's original growth cost. Not enough to make shedding *profitable*, but enough that it doesn't feel like the player's investment was entirely wasted. The exact percentage is a tuning parameter.

This has several design benefits:

1. **Shedding becomes a strategic tool, not just a failure state.** A player might deliberately shed old, self-shaded leaves that are costing more maintenance than they earn, and reclaim some of that glucose for a better-placed leaf. This turns pruning into a skill.

2. **It softens the failure cascade.** If shedding returns some glucose, the plant's stores get a small bump with each shed organ. This slows the cascade and gives the plant's reactive code more ticks to stabilize. The cascade is still dangerous — the plant is shrinking — but it's not a pure death spiral.

3. **It connects to future composting/soil mechanics.** In later phases, shed material becoming soil nutrients opens up whole gameplay loops: companion planting, nutrient cycling, ecosystem design. Building the "shed material has value" concept into Phase 1 means these later mechanics feel like natural extensions rather than bolted-on systems.

4. **It's biologically real.** Plants reabsorb nitrogen and phosphorus from leaves before dropping them (autumn senescence). Deciduous trees are not wasting their leaves — they're recycling them. This makes the mechanic feel grounded rather than gamey.

**Deliberate shedding as a programmed action.** Beyond automatic shedding during failure cascades, the player should be able to write shedding logic into their code:

```growl
when Plant.Leaves.Canopy.Count > 4 and Metrics.Canopy.ShadingLoss > High:
    Plant.Leaves.Canopy.ShedOldest(1)

when Dry:
    Plant.Leaves.Canopy.ShedOldest(2)
```

This would remove organs, recover a fraction of their cost, and reduce maintenance. It's programmatic pruning. A player who knows their canopy will self-shade past 4 leaves can write code that proactively sheds underperforming leaves and reinvests the recovered glucose. Deciduous shedding before a drought — dropping leaves to reduce water demand before stores run out — becomes a viable coded strategy.

Pruning should feel like an optimization tool, not a punishment. The player who writes intelligent shedding logic is playing the architecture game at a higher level than the player who just adds organs.

## Constraints That Risk Being Unfun

Not every realistic constraint makes for good gameplay. Some constraints that are biologically accurate could make the game tedious or frustrating if implemented naively. In a compile-and-run model, the frustration risks are different from a real-time game — the player can't be annoyed by moment-to-moment micromanagement, but they CAN be frustrated by opaque failures that don't teach them how to improve their code.

### Maintenance Cost Must Not Feel Like a Treadmill

The maintenance floor creates the core economic tension — good. But if most of the player's early runs end with "your plant starved because maintenance exceeded income," the game feels punishing before it feels creative.

Guideline: a reasonably built plant in a stable environment should comfortably exceed its maintenance cost with meaningful surplus. The player's first few programs should succeed, producing visible growth and glucose. Maintenance becomes the interesting constraint when the player starts pushing their design — adding more organs, tackling harder commissions, facing variable environments. The progression should be: easy success → ambitious failure → smarter design → harder success.

### The Observe Phase Must Teach

Since the player can't intervene mid-run, the post-run observation is where all learning happens. If the player watches their plant die and can't tell why, the iteration loop breaks. They don't know what to change in their code.

The game needs excellent run playback:

- A timeline showing key events: when stores started draining, when wilting began, when organs shed, when reactive code fired
- Clear metric graphs the player can scrub through: glucose over time, water over time, income vs. maintenance
- Cause annotations: "Leaf shed at tick 42. Reason: glucose stores depleted. Maintenance (4.2/tick) exceeded income (2.1/tick) for 15 ticks."
- The limiting factor at each tick, so the player can see when and why their bottleneck shifted

The observation tools are not a nice-to-have. They are as important as the simulation itself. Without them, the player is guessing at what to change, which makes iteration feel random rather than skillful.

### Self-Shading Should Be Gentle at Low Organ Counts

Self-shading creates a great architecture tradeoff at scale. But for a beginning player with 2-3 leaves, the shading penalty should be minimal. If the player's second leaf is noticeably less productive than their first, their early programs will underperform for reasons they can't yet understand. The shading curve should be gentle for the first few leaves and steepen as the canopy grows. The player discovers self-shading as a mid-game optimization problem, not a beginner trap.

### Failure Must Feel Preventable in Hindsight

In a compile-and-run game, the player watches their plant fail without being able to help. This is only satisfying if the player can look at the run afterward and see exactly what they should have coded differently. "My plant died because I didn't handle drought" is a learnable lesson. "My plant died and I don't know why" is a reason to quit.

Rules for legible failure:

- Shedding never happens without at least several ticks of visible wilting first, so the player can spot the crisis point in the timeline
- The run log must clearly show *why* the plant is shedding (water deficit, glucose deficit) with specific numbers
- The player should be able to identify which `when` handler they should have written — or which existing handler fired too late — by reviewing the timeline
- Shedding the minimum viable organs (the last leaf, last root, or only stem) should be a visually dramatic event, not a quiet metric change. This is the plant's death scene. It should be memorable enough that the player is motivated to prevent it next time

### Static Programs Should Work for Simple Commissions

Reactive code (`when`) is the player's most powerful tool. But if every commission requires reactive code to survive, beginners will feel overwhelmed. Simple commissions in stable environments should be completable with fully static programs — just morphology and fixed stomata settings. This lets the player learn organism design first and reactive coding second.

The progression: static programs work for easy commissions → environmental variability makes static programs fail → the player learns reactive code to handle variability → reactive code becomes the main skill expression for harder commissions. Each step should feel like a natural escalation, not a wall.

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

5. **Failure should teach.** When the player reviews a failed run, the timeline and metrics (`limiting_factor`, `glucose per tick`, `water efficiency`) should tell them exactly what went wrong, when it went wrong, and what code they should write to prevent it next time.

6. **The constraints should create the beginner organisms, not the other way around.** If you hand someone the constraints and say "build a water-efficient plant," they should naturally converge on something that looks like the Water Saver preset. The presets should be discoveries, not inventions.

7. **Static programs should succeed at easy commissions.** The player should be able to deploy a simple program with no reactive code into a stable environment and watch it thrive. Reactive code becomes necessary when the environment is variable or the commission is demanding — not for basic survival.

8. **The iteration loop should feel like engineering, not guessing.** Each failed run should give the player enough information to make a specific, targeted change to their code. If the player is reduced to trial-and-error, the observation tools are insufficient.
