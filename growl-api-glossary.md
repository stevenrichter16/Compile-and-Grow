# Growl API Glossary — What Actually Works

## Functional API Calls

These calls have real gameplay effects — their outputs are read by other systems.

### Leaf Module
- **leaf.grow(size)** — Creates a leaf part. Leaf area feeds `photo.absorb_light()` energy calculation. Costs energy. Accumulates (new leaf each call).
- **leaf.open_stomata(amount)** — Sets stomata openness (0–1) on all leaves. Directly multiplies photosynthesis efficiency. Idempotent (no effect after first call with same value).
- **leaf.close_stomata()** — Sets stomata to 0. Shuts down photosynthesis.
- **leaf.set_color(r, g, b)** — Read by the visualizer for rendering.
- **leaf.absorb_moisture()** — Reads leaf area + world humidity, adds water to organism.
- **leaf.absorb_nutrients() / absorb_chemical(name)** — Reads leaf area, adds resources to organism state.
- **leaf.shed(name)** — Removes a leaf part, reducing leaf area.
- **leaf.regrow(name)** — Creates a new leaf part, restoring leaf area.

### Photo Module
- **photo.absorb_light()** — Core energy generator. Formula: `leafArea * 2.0 * efficiency`. Efficiency = `lightIntensity * stomata * waterLevel`, modified by pigment/chlorophyll/saturation. Consumes water (transpiration).
- **photo.set_pigment(type)** — Sets pigment on leaves. Read by `absorb_light()` to apply a multiplier (e.g., chlorophyll-b, carotenoid each have different curves).
- **photo.boost_chlorophyll(factor)** — Direct multiplier on photosynthesis efficiency. Read by `absorb_light()`.
- **photo.set_light_saturation(threshold)** — Diminishing returns cap. If light > threshold, efficiency drops. Read by `absorb_light()`.
- **photo.store_energy(amount, location)** — Removes energy from organism, stores it in a plant part (stem/root). Can be retrieved later.
- **photo.retrieve_energy(amount, location)** — Pulls stored energy from a part back into the organism's energy pool.
- **photo.chemosynthesis()** — Energy from world chemical concentration.
- **photo.thermosynthesis()** — Energy from world temperature.
- **photo.radiosynthesis()** — Energy from world radiation.
- **photo.decompose()** — Energy from world soil organic matter (depletes it).
- **photo.parasitic()** — Small fixed energy gain + stress. Placeholder.
- **photo.share_energy(amount)** — Deducts energy. Placeholder (no target organism yet).

### Root Module
- **root.grow_down(distance)** — Creates/extends root, increases root area. Root area feeds `root.absorb()`. Costs energy. Accumulates.
- **root.grow_wide(distance)** — Same as grow_down but lateral spread. Costs energy. Accumulates.
- **root.branch(count)** — Creates new root parts, increasing total root area. Costs energy.
- **root.absorb(resource)** — Pulls resources (water, nitrogen, etc.) from world soil into organism. Amount = `rootArea * baseRate * worldValue`. Core resource intake.
- **root.absorb_all() / absorb_filtered(list)** — Variants of absorb for multiple resources.
- **root.deposit(resource, amount)** — Adds value to world resource grid.
- **root.exude(chemical, amount)** — Adds value to world resource grid.
- **root.sense_depth() / sense_moisture() / sense_obstacle() / sense_neighbors()** — Read-only queries, no cost.

### Stem Module
- **stem.grow_up(distance)** — Creates/extends main stem. Increases size and height (read by visualizer). Costs `distance * 0.6 * materialMultiplier` energy. Accumulates.
- **stem.grow_thick(amount)** — Increases thickness (read by visualizer, also read by `stem.store_water()` for capacity). Costs energy. Accumulates.
- **stem.branch(count)** — Creates branch parts (read by visualizer for positioning). Costs energy.
- **stem.grow_segment(length, angle)** — Creates segment parts with angles (read by visualizer). Costs energy.
- **stem.split(count)** — Creates co-dominant stem tips. Costs energy.
- **stem.set_material(type)** — Sets material (fibrous, woody, hollow, etc.). Read by growth functions to multiply energy costs (woody = 2x, hollow = 0.4x).
- **stem.set_rigidity(value)** — Stored in morphology state. Exposed in snapshots.
- **stem.set_color(r, g, b)** — Read by the visualizer for rendering.
- **stem.store_water(amount)** — Deducts water from organism, stores in stem. Reads stem thickness for capacity.
- **stem.store_energy(amount)** — Deducts energy, stores in stem part. Read by `photo.retrieve_energy()`.
- **stem.shed(name)** — Removes a part.
- **stem.heal(name, strength)** — Restores part health.

### Synthesize Module
- **synthesize(base, type, ...)** — Creates a product dictionary. Costs ~1.5+ energy scaled by base type. Returns `none` if not enough energy.
- **emit(product, rate)** — Emits product to world state as `emission_{type}`. Costs `0.3 * rate` energy.
- **produce(product, location)** — Creates a "product" PlantPart on the organism. Costs 0.5 energy.
- **product_enrich(product, nutrient, amount)** — Modifies product quality. Costs energy.

### Reproduce Module
- **reproduce.generate_seeds(count)** — Creates seeds, costs energy, registers with SeedInventory.
- **reproduce.mutate(variance)** — Sets global mutation variance, read by `generate_seeds()`.
- **reproduce.crossbreed(partner)** — Creates hybrid seed. Checks maturity. Costs energy.
- **reproduce.clone()** — Creates clone. Checks maturity. Costs energy.
- **reproduce.fragment(part_name)** — Creates fragment and removes the part from the plant.

### Defense Module
- **defense.resist_disease(type, strength)** — Actually heals the organism by `strength * 0.05`.
- **defense.grow_camouflage(type)** — Changes organism color via morphology (read by visualizer).
- **defense.fever(intensity)** — Can damage organism if intensity > 5, reduces stress.
- **defense.produce_repellent/attractant(type, intensity)** — Modifies world state values.

---

## Cosmetic-Only API Calls (33 total)

These store properties and may cost energy, but **nothing in the codebase reads the values they set**. They have no gameplay effect.

### Stem
- `stem.produce_bark()` — sets `bark_thickness`, never read
- `stem.produce_wax()` — sets `wax_thickness`, never read
- `stem.attach_to()` — sets `attached_to`, `climbing`, never read
- `stem.support_weight()` — sets `reinforced`, never read
- `stem.grow_horizontal()` — `horizontal_spread` and `grow_direction` never read (part.Size increment is real though)

### Root
- `root.set_absorption_rate(resource, rate)` — sets per-resource rate, but **`absorb()` completely ignores it** and uses only `rootArea * baseRate`
- `root.anchor(strength)` — sets `anchor_strength`, never read (no wind/uprooting system)
- `root.connect_fungi()` — sets `fungi_connected`, never read (no mycorrhizal network)
- `root.thicken(amount)` — sets `thickness` on roots, never read
- `root.grow_toward(target, distance)` — `grow_direction` never read (part.Size increment is real)
- `root.grow_up()` (aerial) — `aerial_height` never read

### Leaf
- `leaf.reshape(name, shape)` — sets `shape`, never read
- `leaf.orient(direction)` — sets `orientation` on parts, never read
- `leaf.track_light(enabled)` — sets `track_light`, never read
- `leaf.set_angle_range(min, max)` — sets `angle_min`/`angle_max`, never read
- `leaf.set_stomata_schedule(schedule)` — sets `stomata_schedule`, never read
- `leaf.filter_gas(gas)` — sets `gas_filter_{gas}`, never read
- `leaf.set_coating(type)` — sets `coating`, never read
- `leaf.set_lifespan(ticks)` — sets `lifespan`, never read (no leaf aging system)

### Photo
- `photo.set_metabolism(rate)` — sets `metabolism` on organism, never read by any energy calculation

### Defense
- `defense.grow_thorns(part, sharpness, density)` — properties never read, morphology calls silently fail
- `defense.grow_armor(part, thickness)` — never read, no damage reduction system
- `defense.produce_toxin(type, potency, location)` — never read
- `defense.sticky_trap(part, strength)` — never read
- `defense.quarantine_part(name)` — never read
- `defense.on_damage(callback)` — never read
- `defense.on_neighbor_distress(callback)` — never read

### Synthesize
- `product_fortify(product, property, value)` — fortifications dict never read
- `product_set_coating(product, type)` — coating on products never read
- `product_set_form(product, shape)` — form on products never read

### Reproduce
- `reproduce.set_dispersal(method, params)` — never read
- `reproduce.set_germination(conditions)` — never read
- `reproduce.set_lifecycle(type)` — never read
- `reproduce.set_maturity_age(age)` — never read (maturity advances at fixed +0.05/tick)
- `reproduce.mutate_gene(slot, variance)` — per-gene keys never read by `generate_seeds()`
