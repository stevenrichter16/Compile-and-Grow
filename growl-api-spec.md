# GROWL LANGUAGE — COMPLETE API SPECIFICATION

## Language Overview

Growl is an interpreted, dynamically-typed language for programming universal biocore seed units. It governs all biological systems in the game. Growl programs execute once per growth tick (configurable speed, default ~5 seconds real-time). Every seed runs its own Growl program independently.

Growl is forgiving by design. Errors don't crash the organism — they produce mutations, stunted growth, or unexpected behavior. The organism tries to survive even if the code is bad. This is a deliberate design choice: bad code is more interesting than a stack trace.

---

## Program Structure

A Growl program is a collection of **gene functions** assigned to genome slots. Every organism has a genome with a fixed number of slots and a fixed energy budget determined by the canister tier.

```
CANISTER TIERS
━━━━━━━━━━━━━━
Tier 1 (common):     12 slots, 100 energy
Tier 2 (advanced):   20 slots, 200 energy
Tier 3 (prototype):  32 slots, 400 energy
Tier 4 (experimental): 48 slots, 800 energy   ← very rare, late-game
```

### Minimal Valid Program

Four role slots must be filled. Everything else is optional.

```python
@role("intake")
def intake(org, env):
    root.grow_down(2)
    root.absorb("water")

@role("structure")
def structure(org, env):
    stem.grow_up(3)

@role("energy")
def energy(org, env):
    photo.absorb_light()

@role("output")
def output(org, env):
    product = synthesize(base="carbohydrate")
    produce(product)
```

Energy cost: ~22. Produces a minimal carbohydrate-generating plant.

### Gene Decorator

Every gene function uses the `@gene` or `@role` decorator to assign it to a genome slot.

```python
@role("intake")       # Assigns to the INTAKE role slot (required)
@role("structure")    # Assigns to the STRUCTURE role slot (required)
@role("energy")       # Assigns to the ENERGY role slot (required)
@role("output")       # Assigns to the OUTPUT role slot (required)

@gene("defense")      # Assigns to a named optional slot
@gene("adaptation")   # Assigns to a named optional slot
@gene("reproduction") # etc.
@gene("signal")       # etc.

# Slot names are freeform strings — the player names their own genes.
# The game tracks them by slot number internally.
```

### Gene Function Signature

Every gene function receives two arguments:

```python
@gene("any_name")
def my_gene(org, env):
    # org — the organism's current state (read/write)
    # env — the environment around the organism (read-only)
    pass
```

---

## The `org` Object — Organism State

The organism's internal state. Updated every tick. All genes in the same organism share this state, which is how genes cooperate.

### Core Properties

```python
org.age                  # int — ticks since germination
org.maturity             # float 0.0-1.0 — lifecycle progress
org.alive                # bool — if false, no genes execute
org.energy               # float — current stored energy (not budget)
org.energy_income        # float — energy gained last tick
org.energy_expense       # float — energy spent last tick
org.mass                 # float — total biomass in grams
org.water                # float 0.0-1.0 — hydration level
org.health               # float 0.0-1.0 — overall structural integrity
org.stress               # float 0.0-1.0 — accumulated stress
org.temperature          # float — internal temp in celsius
org.genome               # GenomeInfo — read-only info about the genome
org.name                 # string — player-assigned name (or "unnamed")
```

### Nutrient Store

```python
org.nutrients                    # dict-like access to all nutrients
org.nutrients["nitrogen"]        # float — current nitrogen level
org.nutrients["phosphorus"]      # float
org.nutrients["potassium"]       # float
org.nutrients["iron"]            # float
org.nutrients["calcium"]         # float
org.nutrients["sulfur"]          # float
org.nutrients["zinc"]            # float
org.nutrients["copper"]          # float
org.nutrients["magnesium"]       # float
org.nutrients["carbon"]          # float
org.nutrients.total()            # float — sum of all nutrients
org.nutrients.deficiencies()     # list of nutrients below threshold
org.nutrients.surplus()          # list of nutrients above threshold
```

### Morphology — The Organism's Physical Shape

```python
org.morphology                   # the organism's body plan
org.morphology.height            # float — total height in cm
org.morphology.width             # float — canopy/spread width in cm
org.morphology.depth             # float — root/anchor depth in cm
org.morphology.volume            # float — total volume
org.morphology.surface_area      # float — total exposed surface
org.morphology.center_of_mass    # (x, y, z) — affects stability
org.morphology.symmetry          # "radial", "bilateral", "asymmetric"
org.morphology.color             # (r, g, b) — visual appearance
org.morphology.opacity           # float 0.0-1.0
org.morphology.texture           # "smooth", "rough", "spiny", "fuzzy", etc.
org.morphology.rigidity          # float 0.0-1.0 — how stiff vs flexible
```

### Body Parts Registry

Organisms don't have predefined body parts. Parts are created dynamically by gene code and registered in a parts list. Any gene can read any part; only the gene that created a part can modify it.

```python
org.parts                        # list of all body parts
org.parts.find("root_main")      # find part by name
org.parts.find_type("root")      # find all parts of a type
org.parts.find_type("leaf")      # find all leaf-type parts
org.parts.count("branch")        # how many branches exist

# A body part object:
part = org.parts.find("root_main")
part.name                        # string
part.type                        # string — "root", "stem", "leaf", "fruit", etc.
part.size                        # float
part.health                      # float 0.0-1.0
part.age                         # int — ticks since creation
part.energy_cost                 # float — maintenance cost per tick
part.properties                  # dict — arbitrary key-value data
part.children                    # list — sub-parts attached to this part
part.parent                      # part or None — what this is attached to
```

### Chemical Store

Organisms can produce and store chemicals. These are different from nutrients — chemicals are synthesized compounds used for defense, signaling, or output.

```python
org.chemicals                        # dict-like
org.chemicals["alkaloid"]            # float — amount stored
org.chemicals["nectar"]              # float
org.chemicals["resin"]               # float
org.chemicals.produce("alkaloid", amount)    # synthesize and store
org.chemicals.release("alkaloid", amount)    # emit into environment
org.chemicals.capacity               # float — total chemical storage space
org.chemicals.list()                  # list all stored chemicals
```

### Memory — Persistent State Across Ticks

Genes execute every tick but local variables are lost. `org.memory` persists across ticks, allowing organisms to learn and remember.

```python
org.memory                       # dict-like persistent storage
org.memory["last_water_level"] = org.water
org.memory["times_attacked"] = org.memory.get("times_attacked", 0) + 1
org.memory["growth_direction"] = "north"

# Memory costs energy to maintain — 0.1 energy per key per tick
# This prevents infinite memory. Players must be selective about
# what their organisms remember.

org.memory.keys()                # list all stored keys
org.memory.clear()               # wipe all memory
org.memory.cost()                # current energy cost of stored memory
```

### Signal Bus — Inter-Organism Communication

Organisms can send and receive chemical signals to/from nearby organisms. This is the Growl-to-Growl communication layer (distinct from the Depot, which is cross-language).

```python
org.signals.emit("distress", intensity=0.8, radius=5)
org.signals.emit("pollen", data={"type": "cross_pollinate"}, radius=3)
org.signals.emit("food_here", intensity=0.3, radius=10)

# Receiving signals (returns list of signals detected this tick)
incoming = org.signals.receive()
for signal in incoming:
    signal.type          # string — "distress", "pollen", etc.
    signal.intensity     # float — how strong
    signal.distance      # float — how far away the sender is
    signal.direction     # (x, y) — direction toward sender
    signal.data          # dict — arbitrary payload
    signal.sender        # OrgRef — reference to the sender organism

# Filtering received signals
threats = org.signals.receive(type="distress")
pollen = org.signals.receive(type="pollen", max_distance=3)
```

---

## The `env` Object — Environment State

Everything outside the organism. Read-only — the organism can sense the environment but can only change it through its own growth and chemical emissions.

### Soil

```python
env.soil                         # None if organism isn't soil-based
env.soil.moisture                # float 0.0-1.0
env.soil.temperature             # float — celsius
env.soil.ph                      # float 0-14
env.soil.depth                   # float — total depth of soil in cm
env.soil.density                 # float — compaction level
env.soil.type                    # "loam", "clay", "sand", "calciumite", "shale", etc.
env.soil.organic_matter          # float 0.0-1.0 — decomposed material richness
env.soil.toxins                  # list of ToxinInfo objects
env.soil.fungi_present           # bool
env.soil.fungi_network           # FungiNetwork or None

# Directional scanning — returns direction vector toward resource
env.soil.scan("water")           # (dx, dy) toward nearest water concentration
env.soil.scan("nitrogen")        # (dx, dy) toward nearest nitrogen
env.soil.scan("iron")            # etc.
env.soil.scan_radius(resource, radius)  # all sources within radius
```

### Atmosphere

```python
env.air                          # always available
env.air.temperature              # float — celsius
env.air.humidity                 # float 0.0-1.0
env.air.co2                      # float — CO2 concentration (ppm)
env.air.oxygen                   # float — O2 concentration (ppm)
env.air.wind_speed               # float — m/s
env.air.wind_direction           # (dx, dy) — normalized vector
env.air.pressure                 # float — atmospheric pressure
env.air.toxins                   # list of airborne toxins
env.air.spores                   # list of SporeInfo — airborne biological material
env.air.chemicals                # list of ChemicalInfo — released by other organisms
```

### Light

```python
env.light                        # always available
env.light.intensity              # float 0.0-1.0 (0 = total dark, 1 = full sun)
env.light.direction              # (dx, dy, dz) — primary light source direction
env.light.spectrum               # "natural", "uv_artificial", "bioluminescent", "mixed"
env.light.day_phase              # "dawn", "morning", "noon", "afternoon", "dusk", "night"
env.light.day_length             # float — hours of light per cycle
env.light.uv_index               # float — UV radiation level (high on surface)
```

### Weather (Surface Only — Indoor Is Stable)

```python
env.weather                      # None if indoors
env.weather.current              # "clear", "cloudy", "rain", "storm", "dust_storm", "snow"
env.weather.intensity            # float 0.0-1.0
env.weather.duration_remaining   # int — ticks until weather changes
env.weather.forecast(ticks)      # list of predicted weather states
env.weather.precipitation        # float — water falling per tick
env.weather.lightning_risk       # float 0.0-1.0
```

### Neighbors

```python
env.neighbors                            # access to nearby organisms
env.neighbors.nearby(radius=3)           # list of OrgRef within radius
env.neighbors.count(radius=5)            # int — how many organisms nearby
env.neighbors.nearest()                  # OrgRef — closest organism
env.neighbors.nearest(filter="food")     # closest organism classified as food

# OrgRef — a limited read-only view of another organism
neighbor = env.neighbors.nearest()
neighbor.distance                # float
neighbor.direction               # (dx, dy)
neighbor.type                    # auto-classified: "plant", "fungal", "colonial", etc.
neighbor.output_type             # what it produces: "carbohydrate", "protein", etc.
neighbor.height                  # float
neighbor.health                  # float — rough estimate (not exact)
neighbor.age                     # int
neighbor.is_friendly             # bool — based on signal history
neighbor.signals                 # list of signals this neighbor is currently emitting
neighbor.chemical_emissions      # list of chemicals being released
```

### Threats

```python
env.threats                          # active threats this tick
env.threats.pests                    # list of PestInfo
env.threats.disease                  # list of DiseaseInfo
env.threats.herbivores               # list of HerbivoreInfo
env.threats.environmental            # list — frost, heat, drought, flood, etc.

# PestInfo
pest = env.threats.pests[0]
pest.type                    # "aphid", "beetle", "worm", "mite", etc.
pest.count                   # int — swarm size
pest.location                # "roots", "stem", "leaves", "fruit"
pest.damage_rate             # float — damage per tick if unchecked
pest.weakness                # list — "alkaloid", "thorn", "heat", etc.
pest.size                    # float — affects what defenses work
pest.is_flying               # bool

# DiseaseInfo
disease = env.threats.disease[0]
disease.type                 # "fungal", "bacterial", "viral"
disease.severity             # float 0.0-1.0
disease.spread_rate          # float — how fast it's progressing
disease.affected_parts       # list of part names currently infected
disease.weakness             # list — "copper", "heat", "isolation", etc.
```

### Terrain (Surface Only)

```python
env.terrain                      # None if indoors
env.terrain.elevation            # float — meters above sea level
env.terrain.slope                # float — steepness
env.terrain.slope_direction      # (dx, dy) — which way the slope faces
env.terrain.water_table          # float — depth to groundwater
env.terrain.bedrock_depth        # float — depth to impassable rock
env.terrain.surface_type         # "dirt", "rock", "sand", "gravel", "water"
env.terrain.nearby_water         # WaterSourceInfo or None
```

---

## Module: `root` — Subterranean Growth

Controls underground or anchoring structures. Not limited to roots — could be rhizomes, tendrils, anchors, or absorption networks.

```python
# === GROWTH ===

root.grow_down(distance)
    # Extend downward. Costs energy proportional to distance and soil density.
    # Returns: bool — success/failure (fails if hitting bedrock)

root.grow_up(distance)
    # Roots can grow upward (aerial roots, mangroves).
    # Less efficient at absorption but allows surface access.

root.grow_wide(distance)
    # Lateral spread. Good for shallow soil, stability.

root.grow_toward(direction, distance)
    # Directed growth. Use with env.soil.scan() for resource seeking.
    # direction: (dx, dy) vector or resource name string
    # If given a string like "water", auto-scans and grows toward it.

root.branch(count, from_part=None)
    # Create sub-roots branching from main root or specified part.
    # More branches = more absorption surface but more energy cost.

root.thicken(part_name, amount)
    # Increase thickness of a root segment. Improves durability
    # and storage capacity but costs energy.

# === ABSORPTION ===

root.absorb(resource)
    # Pull a resource from soil into org.nutrients.
    # resource: "water", "nitrogen", "phosphorus", etc.
    # Absorption rate depends on root surface area and soil concentration.
    # Returns: float — amount absorbed

root.absorb_all()
    # Pull all available resources at reduced efficiency.
    # Lazy but wasteful. Good for beginners.

root.absorb_filtered(*resources)
    # Pull only specific resources at full efficiency.
    # root.absorb_filtered("water", "nitrogen", "iron")

root.set_absorption_rate(resource, rate)
    # Fine-tune how aggressively to pull a specific resource.
    # rate: float 0.0-1.0
    # High rate depletes soil faster but gathers more.

# === INTERACTION ===

root.connect_fungi(network)
    # Connect to a mycorrhizal fungi network.
    # Enables resource sharing with other connected organisms.
    # Requires env.soil.fungi_present == True.
    # Returns: FungiConnection

root.deposit(resource, amount)
    # Push a resource INTO the soil. Used for:
    # - Nitrogen fixation (legume-like behavior)
    # - Detoxification (absorb toxin, break it down, deposit safe byproduct)
    # - Terraforming (improving soil for future plantings)

root.exude(chemical, amount)
    # Release a chemical from roots into soil.
    # Can repel pests, attract beneficial organisms, or poison competitors.

root.anchor(strength)
    # Increase anchoring force. Resists uprooting from wind, floods,
    # or herbivores. Does not gather resources.

# === SENSING ===

root.sense_depth()
    # Returns: float — current maximum root depth

root.sense_moisture(direction=None)
    # Returns: float — moisture level at root tips
    # If direction given, returns moisture in that direction.

root.sense_obstacle(direction)
    # Returns: ObstacleInfo or None
    # Detects rocks, other roots, bedrock, pipes, etc.

root.sense_neighbors()
    # Detect other organisms' root systems nearby.
    # Returns: list of RootContact objects
    # RootContact has: organism (OrgRef), distance, direction, 
    # is_competing (bool — drawing same resources?)
```

---

## Module: `stem` — Structural Growth

Controls above-ground or above-surface structural elements. Stems, stalks, trunks, membranes, shells, or any load-bearing structure.

```python
# === GROWTH ===

stem.grow_up(distance)
    # Extend upward. Height improves light access but increases
    # wind exposure and energy cost.

stem.grow_horizontal(distance, direction=None)
    # Lateral structural growth. Vines, runners, creeping stems.
    # If no direction, grows toward strongest light.

stem.grow_thick(amount)
    # Increase stem diameter. More thickness = more structural strength,
    # more water transport capacity, and more internal storage.
    # Also increases mass significantly.

stem.branch(count, height=None, angle=None)
    # Create branches. Each branch can support leaves and fruit.
    # height: where on the stem to branch (default: top)
    # angle: radians from vertical (default: 45°)
    # Returns: list of Part objects for the new branches

stem.grow_segment(length, angle, from_part=None)
    # Add a specific stem segment with precise angle control.
    # Allows complex structures: zigzags, spirals, lattices.
    # from_part: which existing part to grow from (default: tip)

stem.split(count)
    # Fork the current growing tip into multiple tips.
    # Unlike branch, split creates equal co-dominant stems.
    # Useful for bushy, shrub-like morphologies.

# === PROPERTIES ===

stem.set_rigidity(value)
    # float 0.0-1.0
    # 0.0 = completely flexible (vine, tendril — needs support)
    # 0.5 = semi-rigid (grass, young tree)
    # 1.0 = fully rigid (mature trunk, woody)
    # Rigid stems resist wind but snap under extreme force.
    # Flexible stems sway but can tangle.

stem.set_material(type)
    # "herbaceous" — soft, fast-growing, cheap, weak
    # "woody"      — slow-growing, expensive, very strong
    # "fibrous"    — medium, flexible, good tensile strength
    # "hollow"     — very cheap, light, fragile but tall quickly
    # "inflatable" — gas-filled, enables floating structures
    # "crystalline" — hard, brittle, reflects light (rare, expensive)

stem.store_water(amount)
    # Use stem as water reservoir (cactus-like).
    # Only works if stem is thick enough.
    # Returns: float — amount actually stored

stem.store_energy(amount)
    # Use stem as energy reservoir (starch storage like a potato tuber).
    # Retrievable later when energy income is low.

# === SUPPORT ===

stem.attach_to(target)
    # Attach to a nearby structure — wall, pole, another organism.
    # Enables climbing vine behavior. Requires flexible rigidity.
    # target: direction vector, Part, or OrgRef

stem.support_weight(part_name)
    # Explicitly reinforce support for a heavy part (large fruit, etc.)
    # Prevents branches from breaking under load.

stem.shed(part_name)
    # Deliberately drop a branch or segment.
    # Useful for: removing diseased parts, reducing wind load,
    # or redirecting energy to other parts.

stem.heal(part_name, rate)
    # Repair damage to a specific segment.
    # rate: energy allocated to healing per tick.

# === MORPHOLOGY CONTROL ===

stem.set_color(r, g, b)
    # Set the stem's color. Cosmetic but also functional —
    # dark colors absorb more heat, light colors reflect.

stem.set_texture(type)
    # "smooth"  — less surface area, less water loss, harder for pests to grip
    # "rough"   — more surface area, can host symbiotes
    # "waxy"    — waterproof, reduces moisture loss significantly  
    # "hairy"   — insulating, traps air layer for temperature regulation
    # "spiny"   — sharp protrusions, discourages herbivores
    # "sticky"  — traps small insects (carnivorous plant potential)

stem.produce_bark(thickness)
    # Create an outer protective layer. Expensive but powerful.
    # Insulates against temperature, fire resistance, pest barrier.
    # Only works with "woody" material.

stem.produce_wax(thickness)
    # Waterproof coating. Prevents water loss.
    # Works with any material type.
```

---

## Module: `leaf` — Surface Organs

Controls light-catching, gas-exchange, and sensory surfaces. Not necessarily leaves — could be solar panels, gill-like structures, antennae, or membranes.

```python
# === GROWTH ===

leaf.grow(area, from_part=None)
    # Create or expand leaf surface on a branch or stem segment.
    # area: float — surface area in cm²
    # from_part: which branch/segment (default: all available tips)
    # Returns: Part — the leaf part created

leaf.grow_count(number, size_each, from_part=None)
    # Grow multiple smaller leaves instead of one large one.
    # More individual leaves = more redundancy (lose one, keep the rest)
    # but higher total energy cost.

leaf.reshape(part_name, shape)
    # Change leaf shape. Affects airflow, light capture, and water runoff.
    # "broad"   — maximum light capture, high water loss
    # "narrow"  — less light, much less water loss, wind-resistant
    # "needle"  — minimal light, minimal water loss, cold-resistant
    # "cup"     — catches water, can funnel to stem
    # "tube"    — rolled, creates internal microclimate
    # "fractal" — complex edge, maximum surface area per unit mass
    # "flat"    — default, balanced
    # "dome"    — deflects rain and debris

# === ORIENTATION ===

leaf.orient(direction)
    # Point leaves in a specific direction.
    # direction: (dx, dy, dz) vector, or use shortcuts:
    # leaf.orient(env.light.direction)        — face the light
    # leaf.orient("up")                       — flat horizontal
    # leaf.orient("vertical")                 — edge-on (reduce heat/light)

leaf.track_light(enabled=True)
    # Auto-orient toward light source every tick.
    # Costs extra energy but optimizes light capture.
    # Heliotropism — like a sunflower.

leaf.set_angle_range(min_angle, max_angle)
    # Constrain tracking to an angular range.
    # Prevents leaves from orienting into positions that cause
    # structural stress.

# === GAS EXCHANGE ===

leaf.open_stomata(amount)
    # float 0.0-1.0 — how open the gas exchange pores are.
    # Higher = more CO2 in (faster photosynthesis) + more water out
    # Lower = conserve water but slower growth
    # This is the core water/growth tradeoff.

leaf.close_stomata()
    # Fully close. No gas exchange. No water loss. No photosynthesis.
    # Emergency drought measure.

leaf.set_stomata_schedule(schedule)
    # Automate stomata based on conditions.
    # schedule: dict mapping conditions to openness
    # leaf.set_stomata_schedule({
    #     "dawn": 0.3,
    #     "morning": 0.8,
    #     "noon": 0.5,     # reduce at peak heat
    #     "afternoon": 0.8,
    #     "dusk": 0.3,
    #     "night": 0.0
    # })

leaf.filter_gas(gas, action)
    # Control which gases pass through stomata.
    # gas: "co2", "oxygen", "nitrogen", "methane", "toxin_*"
    # action: "absorb", "block", "emit"
    # Enables air-filtering organisms.

# === PROPERTIES ===

leaf.set_color(r, g, b)
    # Affects light absorption spectrum.
    # Dark green = standard photosynthesis
    # Red/purple = different pigments, possibly more UV-resistant
    # White/silver = reflective, reduces heat absorption
    # Black = maximum heat and light absorption

leaf.set_coating(type)
    # "none"       — default
    # "waxy"       — water-repellent, reduces moisture loss
    # "hairy"      — insulating, traps moisture in dry air
    # "reflective" — reflects excess light (high-UV environments)
    # "sticky"     — traps insects and particulates
    # "absorbent"  — draws moisture from air (fog harvesting)

leaf.set_lifespan(ticks)
    # How long each leaf lives before it's shed and regrown.
    # Short lifespan = constantly fresh, more nutrient recycling, higher cost
    # Long lifespan = cheaper but accumulates damage and pests
    # None (default) = leaves persist until manually shed or destroyed

leaf.shed(part_name=None)
    # Drop leaves. If no part specified, shed all.
    # Recovers a portion of the energy invested.
    # Dropped leaves become organic matter in soil.
    # Useful for: drought survival, pest removal, seasonal cycles.

leaf.regrow(part_name)
    # Regrow a previously shed leaf from its branch point.
    # Faster and cheaper than growing new if the branch still exists.

# === ABSORPTION (non-photosynthetic) ===

leaf.absorb_moisture()
    # Pull water directly from humid air through leaf surface.
    # Efficiency depends on humidity and leaf coating.
    # Alternative to root-based water intake.

leaf.absorb_nutrients(resource)
    # Foliar feeding — absorb nutrients from air or rain.
    # Less efficient than roots but works without soil.
    # Enables aerial/floating organisms.

leaf.absorb_chemical(chemical)
    # Absorb specific airborne chemicals.
    # Can be used to harvest chemicals released by other organisms,
    # or to filter toxins from the air.
```

---

## Module: `photo` — Photosynthesis and Energy Systems

Controls energy production. Photosynthesis is the default but not the only option.

```python
# === PHOTOSYNTHESIS ===

photo.absorb_light(efficiency=None)
    # Standard photosynthesis. Converts light + CO2 + water into energy.
    # efficiency: float 0.0-1.0 override (default calculated from leaf area,
    #   light intensity, CO2 levels, and water availability)
    # Returns: float — energy produced this tick

photo.set_pigment(type)
    # Change the photosynthetic pigment. Affects which light wavelengths
    # are usable.
    # "chlorophyll_a" — standard, green, broad spectrum (default)
    # "chlorophyll_b" — blue/red optimized, slightly more efficient
    # "carotenoid"    — orange/yellow, UV-resistant, lower efficiency
    # "phycocyanin"   — blue, works in very low light (deep water/caves)
    # "bacterio"      — infrared, works in complete visible darkness
    #                   (requires infrared light source — rare)

photo.boost_chlorophyll(factor)
    # Increase chlorophyll density. More pigment per leaf area.
    # Effective in low light but wastes energy in bright light.
    # factor: float, e.g. 1.5 = 50% more chlorophyll

photo.set_light_saturation(threshold)
    # At what light intensity the plant is "full" and can't use more.
    # Low threshold = efficient in dim light, wastes bright light.
    # High threshold = uses more bright light, poor in dim light.
    # float 0.0-1.0

# === ALTERNATIVE ENERGY SOURCES ===

photo.chemosynthesis(source)
    # Generate energy from chemical reactions instead of light.
    # Completely independent of light. Requires chemical source in environment.
    # source: "sulfur", "iron", "methane", "hydrogen", "ammonia"
    # Efficiency depends on concentration of source in soil/air.
    # Returns: float — energy produced

photo.thermosynthesis(source)
    # Generate energy from temperature differentials.
    # source: "geothermal" (soil heat), "solar_thermal" (surface heat),
    #         "gradient" (difference between root and leaf temperatures)
    # Low energy output but extremely reliable. Works day and night.
    # Returns: float — energy produced

photo.radiosynthesis()
    # Generate energy from ambient radiation.
    # Only works near radioactive sources (like the nuclear core).
    # Very niche but effectively unlimited in the right environment.
    # The organism turns black from melanin (real science — Chernobyl fungi do this).
    # Returns: float — energy produced

photo.parasitic(target)
    # Drain energy from another organism.
    # Requires physical contact (root.connect or stem.attach_to).
    # target: OrgRef — the organism to drain
    # Morally neutral in-game but the victim organism will stress.
    # Returns: float — energy drained

photo.decompose(organic_matter)
    # Break down dead organic material for energy.
    # Enables fungal/decomposer organisms.
    # organic_matter: accessed via env.soil.organic_matter
    # Returns: float — energy produced

# === ENERGY MANAGEMENT ===

photo.set_metabolism(rate)
    # Overall metabolic rate multiplier.
    # High = fast growth, high energy consumption, short lifespan
    # Low = slow growth, efficient, long lifespan
    # float, default 1.0

photo.store_energy(amount, location="stem")
    # Store excess energy as chemical potential (starch, fat, sugar).
    # location: which body part stores it ("stem", "root", "fruit", or part_name)
    # Stored energy can be retrieved later.

photo.retrieve_energy(amount, location="stem")
    # Pull stored energy back into active use.
    # Used during low-income periods (night, winter, drought).
    # Returns: float — amount actually retrieved (may be less than requested)

photo.share_energy(target, amount)
    # Send energy to another organism through root network or direct contact.
    # Requires fungi connection or physical attachment.
    # Enables cooperative colony behavior.
```

---

## Module: `morph` — Dynamic Morphology

Controls the organism's physical form beyond what root/stem/leaf cover. This is the creative playground for building organisms that don't fit into plant categories.

```python
# === BODY PLAN ===

morph.set_symmetry(type)
    # "radial"    — symmetric around a center axis (flowers, jellyfish)
    # "bilateral" — symmetric left-right (most animals, some leaves)
    # "asymmetric" — no symmetry (free-form, coral-like)
    # Affects how grow commands distribute mass.

morph.set_growth_pattern(type)
    # "apical"     — grows from tips (default, plant-like)
    # "diffuse"    — grows everywhere simultaneously (grass-like)
    # "meristematic" — grows from specific zones (bamboo-like, segmented)
    # "accretive"  — layers build on top of each other (coral, shell)
    # "inflating"  — existing structures expand (balloon, gas bag)
    # Affects how the organism looks and behaves as it grows.

# === CUSTOM STRUCTURES ===

morph.create_part(name, type, properties)
    # Create an entirely new body part that doesn't fit existing categories.
    # name: string identifier
    # type: string category (freeform — "tendril", "bladder", "antenna", etc.)
    # properties: dict of initial properties
    # Returns: Part
    #
    # Example — create a gas bladder:
    # bladder = morph.create_part("float_bladder", "bladder", {
    #     "volume": 10,
    #     "gas": "hydrogen",
    #     "wall_thickness": 0.5
    # })

morph.attach(part, to_part, position="tip")
    # Attach a created part to an existing part.
    # position: "tip", "base", "middle", or float 0.0-1.0 along the part

morph.grow_part(part_name, property, amount)
    # Increase a property of a custom part.
    # morph.grow_part("float_bladder", "volume", 5)  — inflate bladder

morph.shrink_part(part_name, property, amount)
    # Decrease a property.

morph.remove_part(part_name)
    # Destroy a body part. Recovers some energy.

# === MOVEMENT (limited) ===

morph.orient_toward(direction)
    # Slowly orient the entire organism toward a direction.
    # Very slow — organisms are sessile, not mobile.
    # But over many ticks, they can lean, rotate, or reposition.

morph.contract(part_name, amount)
    # Contract a body part. Requires non-rigid material.
    # Can create slow movement, pumping, or gripping.
    # Think: Venus flytrap closing, tendril coiling.

morph.expand(part_name, amount)
    # Expand a body part. Opposite of contract.

morph.pulse(part_name, frequency, amplitude)
    # Rhythmic contraction/expansion cycle.
    # Enables pumping behavior (circulating fluids),
    # or rhythmic spore/seed dispersal.

# === SURFACE PROPERTIES ===

morph.set_surface(part_name, properties)
    # Fine-grained surface control on any part.
    # properties dict can include:
    #   "color": (r, g, b)
    #   "reflectance": float 0.0-1.0
    #   "bioluminescence": float 0.0-1.0 (glow intensity)
    #   "biolum_color": (r, g, b) 
    #   "absorption": float 0.0-1.0 (how much light/heat it soaks up)
    #   "hydrophobic": bool (water-repellent surface)
    #   "conductivity": float (thermal or electrical)

morph.emit_light(intensity, color, part_name=None)
    # Bioluminescence. The organism glows.
    # Costs energy proportional to intensity.
    # Can be continuous or pulsed (use in combination with pulse()).
    # If no part specified, whole organism glows.
```

---

## Module: `defense` — Protection Systems

```python
# === PHYSICAL DEFENSE ===

defense.grow_thorns(part_name=None, sharpness=0.5, density=0.5)
    # Grow sharp protrusions on a body part or whole organism.
    # sharpness: float 0.0-1.0 — how damaging
    # density: float 0.0-1.0 — how many per cm²
    # Effective against herbivores and physical pests.

defense.grow_armor(part_name, thickness)
    # Harden the outer layer of a body part.
    # Blocks physical damage but heavy and restricts growth.

defense.grow_camouflage(environment_type=None)
    # Match color and texture to surroundings.
    # If environment_type is None, auto-matches current environment.
    # Reduces detection by herbivores.
    # Options: "soil", "rock", "vegetation", "dark", "bright"

# === CHEMICAL DEFENSE ===

defense.produce_toxin(type, potency=0.5, location="all")
    # Synthesize a toxic compound.
    # type: "alkaloid", "tannin", "cyanide", "capsaicin", "oxalate", "custom"
    # potency: float 0.0-1.0 — strength of effect
    # location: which parts contain the toxin ("leaves", "fruit", "stem", etc.)
    # WARNING: toxins in "fruit" make the output potentially harmful to consumers.
    # The player must plan around this.

defense.produce_repellent(type, radius=2)
    # Emit a pest-repelling compound into the air.
    # type: "volatile_oil", "sulfur_compound", "phenol", "custom"
    # Creates a repellent zone around the organism.
    # Repels pests but may also repel pollinators.

defense.produce_attractant(type, target, radius=3)
    # Attract a specific organism type.
    # type: "nectar", "pheromone", "scent", "custom"
    # target: "pollinator", "predator_insect", "symbiote"
    # Attracting predator insects that eat your pests is a 
    # sophisticated defense strategy.

defense.sticky_trap(part_name, strength=0.5)
    # Make a surface sticky enough to trap small organisms.
    # Trapped organisms can be decomposed for nutrients (carnivorous plant).
    # Requires pairing with photo.decompose() to actually gain benefit.

# === IMMUNE SYSTEM ===

defense.resist_disease(type, strength=0.5)
    # Allocate energy to resisting a specific disease type.
    # type: "fungal", "bacterial", "viral", "all"
    # "all" is much more expensive than targeting a specific type.
    # Higher strength = faster recovery but higher energy cost.

defense.quarantine_part(part_name)
    # Isolate a diseased body part to prevent spread.
    # The part stops receiving nutrients and slowly dies,
    # but the disease doesn't spread to the rest of the organism.
    # Can regrow the part later with stem.regrow() or leaf.regrow().

defense.fever(amount)
    # Raise internal temperature to fight infection.
    # amount: degrees celsius to increase
    # Effective against many pathogens but stresses the organism.
    # Too much fever damages the organism itself.

# === REACTIVE DEFENSE ===

defense.on_damage(callback)
    # Register a function that runs when the organism takes damage.
    # The callback receives DamageInfo:
    # 
    # defense.on_damage(lambda dmg: 
    #     defense.produce_toxin("alkaloid", potency=dmg.severity)
    #     if dmg.source == "pest" else None
    # )
    # 
    # This allows organisms to REACT to attacks rather than 
    # always running defenses.

defense.on_neighbor_distress(callback)
    # Register a function that runs when a nearby organism 
    # emits a distress signal.
    # Enables cooperative defense: one organism gets attacked,
    # all neighbors ramp up their defenses.
    #
    # defense.on_neighbor_distress(lambda signal:
    #     defense.produce_toxin("alkaloid", potency=0.8)
    # )
```

---

## Module: `reproduce` — Reproduction and Propagation

```python
# === SEED PRODUCTION ===

reproduce.generate_seeds(count, energy_per_seed)
    # Produce seeds from the organism's own genome.
    # count: how many seeds to create
    # energy_per_seed: energy invested in each seed
    #   Higher investment = seedling starts with more stored energy
    #   = better chance of survival in harsh conditions.
    # Seeds inherit the parent's genome.
    # Returns: list of SeedInfo

reproduce.set_dispersal(method, params=None)
    # How seeds are spread.
    # "drop"     — falls directly below parent. Minimal spread.
    # "wind"     — carried by air. params: {"weight": float}
    #              Lighter seeds travel further.
    # "burst"    — explosive pod. params: {"force": float, "radius": float}
    # "animal"   — attractive fruit coating, animals carry seeds.
    #              Requires fruit output with appealing properties.
    # "water"    — floats and drifts. params: {"buoyancy": float}
    # "network"  — travels through fungi network to specific location.
    #              Requires fungi connection.

reproduce.set_germination(conditions)
    # Define what conditions the seed requires to germinate.
    # conditions: dict of minimum requirements
    # reproduce.set_germination({
    #     "moisture": 0.3,
    #     "temperature_min": 10,
    #     "temperature_max": 35,
    #     "light": 0.2,
    #     "soil_depth": 1
    # })
    # Seeds that land in unsuitable conditions remain dormant.

# === MUTATION ===

reproduce.mutate(variance=0.1)
    # Child seeds will have slight random variations in gene parameters.
    # variance: float — how much genes can drift (0.0 = clones, 1.0 = wild)
    # This is how organisms adapt to conditions over generations.
    # If the player plants mutating seeds and waits many generations,
    # the organisms will slowly optimize themselves to local conditions.

reproduce.mutate_gene(slot_name, variance=0.1)
    # Mutate only a specific gene, leaving others stable.
    # Useful for fine-tuning one aspect while keeping the rest reliable.

reproduce.crossbreed(other_org)
    # Combine genomes with a neighbor organism.
    # Requires both organisms to have active reproduce genes
    # and be within signal range.
    # other_org: OrgRef
    # Returns: SeedInfo with hybrid genome
    # The child gets some genes from each parent — which ones is semi-random.
    # Powerful but unpredictable.

# === VEGETATIVE REPRODUCTION ===

reproduce.clone(direction=None)
    # Grow a genetic copy directly from the parent organism.
    # Connected by a runner/stolon/rhizome.
    # The clone shares the parent's root network initially.
    # direction: where to clone, or None for nearest open space.
    # This is how organisms colonize an area without seeds.

reproduce.fragment(part_name)
    # Detach a body part that can independently grow into a new organism.
    # The fragment carries the full genome and some stored energy.
    # Like propagation from cuttings.

# === LIFECYCLE ===

reproduce.set_lifecycle(type)
    # "annual"    — organism completes full cycle in one season, then dies.
    #               All energy redirected to reproduction at end of life.
    #               High seed output, no persistence.
    # "perennial" — organism persists across seasons. Lower seed output
    #               but doesn't need replanting. Builds up over time.
    # "ephemeral" — very short lifecycle (days). Rapid generations.
    #               Best for mutation/evolution strategies.
    # "immortal"  — never naturally dies. Grows indefinitely.
    #               Very high maintenance energy but compounds over time.

reproduce.set_maturity_age(ticks)
    # How many ticks before the organism can reproduce.
    # Short = faster spread, but organism may reproduce before
    #   it's fully developed.
    # Long = organism is well-established before reproducing.
```

---

## Module: `synthesize` — Product Creation

The heart of the output system. Creates novel materials from available nutrients and energy.

```python
synthesize(
    base,               # required — primary material category
    density=0.5,        # how compact the product is
    water_content=0.3,  # moisture level
    growth_rate=0.5,    # how fast the product forms
    **kwargs            # additional properties vary by base type
)
# Returns: Product object

# === BASE TYPES ===

# FOOD BASES
"carbohydrate"     # Sugars, starches, cellulose
    # kwargs: sweetness (float), complexity ("simple", "complex", "starch")
    # Simple = quick energy, poor nutrition. Complex = slow release, filling.

"protein"          # Amino acid chains
    # kwargs: completeness (float 0-1, how many essential amino acids),
    #         texture ("soft", "firm", "fibrous", "gelatinous")

"lipid"            # Fats and oils
    # kwargs: saturation ("saturated", "unsaturated", "polyunsaturated"),
    #         melting_point (float, celsius — solid fat vs liquid oil),
    #         flavor ("neutral", "nutty", "fruity", "pungent")

# MATERIAL BASES
"fiber"            # Structural strands
    # kwargs: tensile_strength (float), flexibility (float),
    #         weave ("loose", "tight", "mesh"),
    #         material ("cellulose", "silk", "polymer")

"resin"            # Hardening compounds
    # kwargs: cure_time (float, ticks until hard), 
    #         hardness (float), transparency (float),
    #         solvent_resistance (float)

"rubber"           # Elastic compounds
    # kwargs: elasticity (float), durability (float),
    #         temperature_range (min, max)

"ite"     # calcium/ite/calcium mineral deposits
    # kwargs: hardness (float), porosity (float),
    #         crystal_structure ("amorphous", "cubic", "hexagonal")

# CHEMICAL BASES
"chemical"         # Synthesized compounds
    # kwargs: type (see below), potency (float),
    #         stability (float — how long before it degrades),
    #         delivery ("liquid", "gas", "powder", "gel")
    #
    # Chemical types:
    #   "phosphorescent"  — glows (bioluminescent output)
    #   "pigment"         — color compound (dyes, paints)
    #   "adhesive"        — sticky binding agent
    #   "solvent"         — dissolves other materials
    #   "catalyst"        — speeds up reactions (useful for Struct processes)
    #   "fuel"            — combustible energy source
    #   "medicine"        — therapeutic compound (healing, antidote)
    #   "acid"            — corrosive
    #   "base"            — alkaline neutralizer
    #   "hormone"         — biological signaling molecule
    #   "antibiotic"      — kills bacteria
    #   "antifungal"      — kills fungi
    #   "nutrient_rich"   — concentrated fertilizer for other organisms

# EXOTIC BASES (late-game, high energy cost)
"bioelectric"      # Generates electrical charge
    # kwargs: voltage (float), amperage (float),
    #         discharge_pattern ("continuous", "pulse", "on_contact")

"magnetic"         # Produces magnetic field
    # kwargs: field_strength (float), polarity ("north", "south", "alternating")

"piezoelectric"    # Generates charge under mechanical stress
    # kwargs: sensitivity (float), output_per_stress (float)
    # Wind blowing on the organism generates electricity.


# === PRODUCT OBJECT ===

product = synthesize(base="carbohydrate", density=0.6)

product.enrich(nutrient, amount)
    # Add a specific nutrient to the product.
    # The organism must have that nutrient available in org.nutrients.
    # product.enrich("iron", 0.2)
    # product.enrich("vitamin_c", 0.5)  ← vitamins are synthesized, not absorbed
    # product.enrich("protein", 0.3)    ← cross-enrich with other base types

product.fortify(property, value)
    # Enhance a non-nutritional property.
    # product.fortify("shelf_life", 60)     — days before spoilage
    # product.fortify("flavor", "sweet")    — affects NPC preference
    # product.fortify("color", (255, 200, 0))  — visual appearance
    # product.fortify("aroma", "floral")    — affects pollinator/pest attraction

product.set_coating(type)
    # Outer coating on the product.
    # "none"    — default, perishable
    # "waxy"    — water-resistant, longer shelf life
    # "shell"   — hard outer casing, must be cracked to access (like a nut)
    # "husk"    — fibrous wrapper, easy to remove
    # "pulp"    — soft outer layer, edible, contains different nutrients than core

product.set_form(shape)
    # Physical shape of the product.
    # "grain"   — small, dry, stackable (wheat-like)
    # "berry"   — small, round, wet (fruit-like)
    # "pod"     — elongated, contains multiple units (pea-like)
    # "tuber"   — underground mass (potato-like)
    # "head"    — dense cluster at top of stem (cabbage/sunflower-like)
    # "sap"     — liquid, must be harvested by tapping stem
    # "sheet"   — flat membrane, harvestable by peeling
    # "dust"    — fine particles, collected by shaking or airflow
    # "gel"     — semisolid blob, scoopable
    # "crystal" — geometric solid, grows slowly, very dense

# === PRODUCE FUNCTION ===

produce(product, location="tips", rate=None)
    # Actually grow the product on the organism.
    # product: Product object from synthesize()
    # location: "tips" (branch ends), "roots" (underground), 
    #           "stem" (along the stem), "surface" (all over),
    #           "internal" (stored inside, must be harvested by cutting),
    #           or specific part_name
    # rate: float — override for how fast production happens.
    #       None = use product's growth_rate
    # 
    # Multiple produce() calls per tick are allowed.
    # An organism can produce different products from different parts.

emit(product, rate=None)
    # Instead of producing a harvestable item, release the product
    # into the environment. Used for:
    # - Oxygen emission (air purifier organism)
    # - Chemical release (pesticide cloud, fertilizer mist)
    # - Bioluminescent compound release (light effects)
    # - Spore dispersal
    # rate: float — units released per tick
```

---

## Module: `depot` — Cross-Language Communication

Growl's interface to the shared Depot system. This is how organisms talk to Struct buildings and Signal logistics.

```python
# === WRITING ===

depot.set(key, value)
    # Set a value in the Depot.
    # depot.set("field_01.status", "ready_to_harvest")

depot.put(key, value, add=False)
    # If add=True, adds to existing numeric value instead of overwriting.
    # depot.put("silo_01.wheat", 45, add=True)

depot.emit(event_name, data)
    # Fire an event that Signal listeners and Struct ports can hear.
    # depot.emit("harvest.complete", {
    #     "source": "field_01",
    #     "product_type": "grain",
    #     "amount": 45,
    #     "quality": 0.8,
    #     "nutrients": {"carbohydrate": 0.6, "protein": 0.2}
    # })

# === READING ===

depot.get(key)
    # Read a value from the Depot.
    # power = depot.get("core.output")

depot.query(pattern)
    # Search Depot with wildcards.
    # silos = depot.query("silo_*.capacity_pct")
    # Returns: list of {key, value} dicts

depot.exists(key)
    # Check if a key exists. Returns bool.

# === LISTENING ===

depot.on(event_name, callback)
    # Listen for Depot events from within Growl.
    # Unlike Signal (which is event-ONLY), Growl listeners run
    # alongside the normal tick-based gene execution.
    #
    # depot.on("irrigation.activated", lambda data:
    #     org.memory.set("last_irrigated", org.age)
    # )
    #
    # depot.on("power.fluctuation", lambda data:
    #     leaf.close_stomata() if data["severity"] > 0.5 else None
    # )
```

---

## Built-in Functions — Available Everywhere

```python
# === MATH ===
min(a, b)
max(a, b)
clamp(value, low, high)     # constrain value between low and high
lerp(a, b, t)               # linear interpolation
remap(value, in_low, in_high, out_low, out_high)
distance(point_a, point_b)  # euclidean distance between coordinates
direction(from_point, to_point)  # unit vector from A toward B
random(low=0, high=1)       # random float in range
random_int(low, high)        # random integer in range
random_choice(list)          # random element from list
noise(x, y=0, seed=0)       # perlin noise at coordinates (for organic variation)

# === LOGIC ===
chance(probability)          # returns True with given probability (0.0-1.0)
                             # chance(0.3) = 30% chance of True
every(n)                     # returns True every N ticks
                             # useful for periodic behavior without memory
after(tick_count)            # returns True after N ticks have passed since germination
between(start, end)          # True if org.age is between start and end ticks
season()                     # returns "spring", "summer", "autumn", "winter"
                             # (indoor facilities have no seasons — always returns "stable")
time_of_day()                # returns "dawn", "morning", "noon", "afternoon", "dusk", "night"

# === UTILITIES ===
log(message)                 # Print to the organism's debug log (visible on terminal)
warn(message)                # Print a warning (shows as amber in debug log)
error(message)               # Print an error (shows as red, does NOT crash the organism)
name_self(string)            # Set the organism's display name
classify_self(string)        # Set the organism's classification tag
                             # Used by env.neighbors and depot for identification

# === DATA STRUCTURES ===
# Standard Python-like structures are available:
# Lists, dicts, tuples, sets, strings
# List comprehensions work
# Lambda functions work
# No imports — everything is built-in

# === SPECIAL CONSTANTS ===
TICK          # int — current tick number (global game time)
SELF          # OrgRef — reference to this organism (for neighbor interactions)
NONE          # null value
UP            # (0, 1) direction vector
DOWN          # (0, -1)
LEFT          # (-1, 0)
RIGHT         # (1, 0)
NORTH         # alias for UP (surface gameplay)
SOUTH         # alias for DOWN
EAST          # alias for RIGHT
WEST          # alias for LEFT
```

---

## Conditional Execution Patterns

Growl supports several patterns for organisms that adapt behavior based on conditions. These are syntactic sugar over standard conditionals, designed to make biological programming feel natural.

```python
# === WHEN BLOCKS ===
# Execute only when a condition is first met (edge-triggered, not level-triggered)
# Useful for one-time responses to changing conditions

when org.water < 0.2:
    # This block runs ONCE when water drops below 0.2.
    # It does NOT run again until water goes above 0.2 and drops again.
    leaf.close_stomata()
    root.grow_toward("water", 5)
    org.signals.emit("drought_stress", intensity=0.9, radius=5)

when env.threats.pests:
    # Runs once when pests first appear.
    defense.produce_toxin("alkaloid", potency=0.6)
    log("Pest detected — deploying alkaloids")

# === WHILE BLOCKS ===
# Execute every tick as long as condition holds (standard loop-like)

while org.water < 0.5:
    root.absorb("water")
    leaf.open_stomata(0.2)  # conserve water while low

# === PHASE BLOCKS ===
# Execute during a specific lifecycle phase.
# Cleaner than checking org.maturity manually.

phase "seedling" (0.0, 0.2):
    # First 20% of life — focus on roots and establishment
    root.grow_down(3)
    root.grow_wide(2)
    stem.grow_up(1)

phase "vegetative" (0.2, 0.6):
    # Middle of life — grow structure and leaves
    stem.grow_up(2)
    stem.branch(2)
    leaf.grow(6)

phase "reproductive" (0.6, 0.9):
    # Late life — focus all energy on output
    produce(product)
    reproduce.generate_seeds(4, energy_per_seed=5)

phase "senescence" (0.9, 1.0):
    # End of life — shut down gracefully
    leaf.shed()
    stem.store_energy(org.energy * 0.5)  # bank energy in stem for next generation
    reproduce.generate_seeds(10, energy_per_seed=2)  # final seed burst

# === RESPOND BLOCKS ===
# Syntactic sugar for defense.on_damage and similar reactive patterns.
# Makes event-driven behavior read more naturally.

respond to "damage":
    source = event.source
    severity = event.severity
    if source == "pest" and severity > 0.3:
        defense.produce_toxin("capsaicin", potency=severity)
    elif source == "wind":
        stem.grow_thick(1)
        root.anchor(severity)

respond to "neighbor_signal":
    if event.type == "distress":
        defense.produce_repellent("volatile_oil", radius=3)
    elif event.type == "pollen" and org.maturity > 0.6:
        reproduce.crossbreed(event.sender)

respond to "harvest":
    # The organism was just partially harvested by a drone or player.
    # It can react — regrow, redirect energy, signal distress.
    if event.amount > org.mass * 0.5:
        # More than half the organism was taken
        photo.set_metabolism(1.5)   # speed up regrowth
        root.absorb_all()           # pull everything available
    else:
        # Minor harvest — just regrow the taken parts
        leaf.regrow(event.parts_taken)
```

---

## Energy Budget System

Every function call in every gene has an energy cost. The total cost of all genes must fit within the canister's energy budget. This is calculated at flash time (when the program is written to the canister) and enforced at runtime.

```
ENERGY COST REFERENCE (approximate)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

ROOT FUNCTIONS
  grow_down(d)           1 + (d * 2)
  grow_wide(d)           1 + (d * 1.5)
  grow_toward(dir, d)    2 + (d * 2)      — sensing costs extra
  absorb(resource)       2 per resource type
  absorb_all()           5                 — convenience tax
  connect_fungi()        4
  anchor(s)              1 + s

STEM FUNCTIONS
  grow_up(d)             1 + (d * 2)
  grow_thick(a)          2 + (a * 3)      — thickening is expensive
  branch(c)              3 per branch
  set_material("woody")  +5 base cost     — premium materials cost more
  produce_bark(t)        3 + (t * 4)

LEAF FUNCTIONS
  grow(area)             1 + (area * 0.5)
  track_light()          3 per tick        — continuous energy drain
  open_stomata(a)        0.5              — cheap but indirect costs via water loss

PHOTO FUNCTIONS
  absorb_light()         0 (it PRODUCES energy)
  chemosynthesis()       3 base            — costs energy to maintain the pathway
  boost_chlorophyll(f)   2 * f
  radiosynthesis()       5 base            — exotic pathways are pricier

DEFENSE FUNCTIONS
  grow_thorns(s, d)      2 + (s * d * 3)
  produce_toxin(t, p)    4 + (p * 8)      — potent toxins are very expensive
  resist_disease(t, s)   3 + (s * 5)
  fever(a)               a * 2            — linear with temperature increase

REPRODUCE FUNCTIONS
  generate_seeds(c, e)   c * (e + 2)      — each seed costs its investment + overhead
  clone()                15               — expensive but guaranteed
  crossbreed()           10
  mutate()               1                — very cheap, just randomness

SYNTHESIZE
  base cost varies:
    "carbohydrate"       3
    "protein"            5                — protein is harder to make
    "lipid"              4
    "fiber"              3
    "chemical"           6                — synthesis is complex
    "bioelectric"        12               — exotic bases are costly
  enrich() per call      1
  fortify() per call     0.5

MEMORY
  per key stored         0.1 per tick     — ongoing cost

SIGNALS
  emit()                 1 + (radius * 0.5)
  receive()              0.5

MORPH
  create_part()          3 + complexity
  emit_light(i)          2 + (i * 4)      — bright glow is expensive

NOTE: These costs are per-tick when in conditional blocks that are active.
A defense gene that only activates on pest detection costs 0 when no pests 
are present and full cost when active. The energy budget must accommodate 
the WORST CASE — all conditionals firing simultaneously.
```

### Energy Budget Feedback

When the player writes a program and flashes it, the terminal shows a breakdown:

```
GENOME ENERGY ANALYSIS
━━━━━━━━━━━━━━━━━━━━━
Budget: 100

  @role intake:          14 energy (base) / 22 energy (worst case)
  @role structure:       11 energy (base) / 18 energy (worst case)
  @role energy:           3 energy (produces +12 net)
  @role output:           8 energy (base) / 8 energy (worst case)
  @gene drought:          0 energy (base) / 10 energy (worst case)
  @gene defense:          0 energy (base) / 16 energy (worst case)
  @gene companion:        4 energy (base) / 4 energy (worst case)
  memory (3 keys):        0.3 per tick

  Base total:            40.3 / 100  ✓
  Worst case total:      78.3 / 100  ✓
  Net energy income:    +12.0 (photosynthesis)
  Headroom:              21.7 (available for additional genes)

  ⚠ NOTE: If all defensive genes activate simultaneously during
    a drought with pest attack, energy draw will spike to 78.3.
    Ensure photosynthesis income can sustain this or organism
    will enter energy deficit and begin shedding parts.

Flash to canister? (yes/no) > _
```

If worst case exceeds budget:

```
  Worst case total:     114.7 / 100  ✗ OVER BUDGET

  The organism cannot sustain all genes simultaneously.
  Under peak stress, it will enter energy deficit and:
    1. Shed lowest-priority body parts
    2. Reduce growth rate to zero
    3. Begin consuming stored energy
    4. If stores depleted: die

  Options:
    - Reduce gene complexity (lower parameter values)
    - Remove an optional gene
    - Improve energy production (better photo gene)
    - Accept the risk (organism will survive mild conditions
      but may die under compound stress)

  Flash anyway? (yes/no) > _
```

The game lets you flash an over-budget program. The organism will work fine under normal conditions. It only dies if too many expensive genes activate at once. Some players will deliberately over-budget and gamble on mild conditions. That's a valid strategy — and a fun way to learn about risk management.

---

## Complete Example: A Self-Sustaining Light Organism

This organism doesn't produce food. It produces light. It powers itself from the nuclear core's ambient radiation, purifies air, and glows. The player invented it to save power on facility lighting.

```python
# ORGANISM: Lumenvine
# Purpose: Bioluminescent air purifier
# Canister: Tier 2 (20 slots, 200 energy)

name_self("Lumenvine")
classify_self("utility_bioluminescent")

# ─── INTAKE: Absorb from air, not soil ───
@role("intake")
def intake(org, env):
    # No roots needed — this thing hangs from the ceiling
    leaf.absorb_moisture()
    leaf.absorb_chemical("co2")
    
    # If mounted near a water pipe, absorb condensation
    if env.air.humidity > 0.6:
        leaf.absorb_moisture()  # double absorption in humid conditions

# ─── STRUCTURE: Vine that hangs and spreads ───
@role("structure")  
def structure(org, env):
    phase "young" (0.0, 0.3):
        stem.set_material("fibrous")
        stem.set_rigidity(0.2)        # very flexible — hangs like a vine
        stem.grow_horizontal(2)
        stem.grow_segment(1, angle=-90) # grow downward (hanging)
    
    phase "mature" (0.3, 1.0):
        if org.morphology.width < 30:  # keep spreading until 30cm wide
            stem.branch(1)
            stem.grow_horizontal(1)
    
    stem.attach_to("ceiling")  # anchors to nearest overhead surface
    
    morph.set_surface("main_stem", {
        "bioluminescence": 0.3,        # stem glows faintly too
        "biolum_color": (180, 255, 200) # soft green
    })

# ─── ENERGY: Radiosynthesis from nuclear core ───
@role("energy")
def energy(org, env):
    # Primary: harvest ambient radiation
    income = photo.radiosynthesis()
    
    # Backup: if moved away from core, fall back to photosynthesis
    if income < 2:
        photo.set_pigment("phycocyanin")  # works in very low light
        photo.absorb_light()
    
    # Store excess for nighttime glow
    if org.energy > org.energy_expense * 2:
        photo.store_energy(org.energy * 0.2, location="stem")

# ─── OUTPUT: Bioluminescent light + clean oxygen ───
@role("output")
def output(org, env):
    # Primary output: GLOW
    brightness = remap(org.energy, 0, 50, 0.1, 0.8)
    brightness = clamp(brightness, 0.1, 0.8)  # always some glow, never blinding
    
    morph.emit_light(
        intensity=brightness,
        color=(200, 255, 220)  # warm white-green
    )
    
    # Secondary output: clean oxygen
    oxygen = synthesize(
        base="chemical",
        type="nutrient_rich",    # technically it's just O2
        potency=0.8,
        stability=1.0
    )
    emit(oxygen, rate=org.energy_income * 0.1)

# ─── ADAPTIVE GLOW: responds to presence ───
@gene("presence_sense")
def presence(org, env):
    # Brighten when something is nearby (motion-sensor light)
    nearby = env.neighbors.count(radius=5)
    if nearby > 0 or env.air.chemicals:  # CO2 spike = someone breathing nearby
        org.memory["bright_mode"] = 20   # stay bright for 20 ticks
    
    if org.memory.get("bright_mode", 0) > 0:
        morph.emit_light(intensity=0.9, color=(255, 255, 240))  # bright warm white
        org.memory["bright_mode"] -= 1

# ─── AIR QUALITY: filter toxins ───
@gene("air_filter")  
def air_filter(org, env):
    for toxin in env.air.toxins:
        leaf.absorb_chemical(toxin.name)
        # Break down internally — costs energy but cleans the air
        org.chemicals.produce("neutralizer", amount=toxin.concentration * 0.5)

# ─── REPRODUCTION: slow spread along ceiling ───
@gene("ceiling_spread")
def ceiling_spread(org, env):
    if org.maturity > 0.7 and every(50):
        reproduce.clone(direction="horizontal")
        # New vine starts growing along the ceiling nearby
        # Over time, the ceiling becomes a glowing garden

# ─── DEPOT INTEGRATION ───
@gene("depot_report")
def depot_report(org, env):
    if every(10):  # report every 10 ticks
        depot.set("lumenvine_" + str(SELF.id) + ".brightness", org.energy_income)
        depot.set("lumenvine_" + str(SELF.id) + ".air_quality", 
                  1.0 - len(env.air.toxins) * 0.1)
    
    # Listen for power grid changes
    depot.on("power.fluctuation", lambda data:
        photo.store_energy(org.energy * 0.5)  # bank energy during fluctuations
    )
```

```
GENOME ENERGY ANALYSIS — LUMENVINE
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Budget: 200

  @role intake:          8 energy
  @role structure:       18 energy (base) / 24 energy (worst case)
  @role energy:          5 energy (produces +15 net via radiosynthesis)
  @role output:          14 energy (base) / 18 energy (worst case)
  @gene presence_sense:  1 energy (base) / 6 energy (active)
  @gene air_filter:      0 energy (base) / 12 energy (heavy toxins)
  @gene ceiling_spread:  0 energy (base) / 15 energy (cloning tick)
  @gene depot_report:    2 energy
  memory (2 keys):       0.2 per tick

  Base total:            48.2 / 200  ✓
  Worst case total:      85.2 / 200  ✓
  Net energy income:    +15.0 (radiosynthesis)
  Headroom:             114.8

  Classification: UTILITY — BIOLUMINESCENT
  This organism produces light, not food.
  Estimated illumination: ~60 lumens (equivalent to a dim bulb)
  Six organisms would light a standard corridor.

Flash to canister? (yes/no) > _
```

The player hangs six of these from the grow room ceiling. They glow. They clean the air. They spread slowly along the ceiling on their own. The UV grow lights can be dimmed because the lumenvines provide supplemental light.

Total power saved: ~30 kW. That's 30 kW the player can reroute to the logistics wing, or to more grow beds, or to charge drones faster.

And none of this was designed by the game developer. The API just provided the building blocks. The player invented the lumenvine.
