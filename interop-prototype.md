# Cross-Language Interop: Mechanical Prototype

## The Core Problem

Three languages run three separate runtimes. A player writes Growl to grow wheat, Struct to build a silo, and Signal to move wheat into the silo. How do these programs actually talk to each other?

---

## The Depot (Shared Memory)

The **Depot** is the in-game interop layer. It's a shared key-value store that all three languages can read from and write to. Physically, it's represented in-game as a glowing conduit network connecting terminals.

Every piece of data that crosses a language boundary passes through the Depot.

```
┌──────────┐       ┌─────────┐       ┌──────────┐
│  GROWL   │──────▶│  DEPOT  │◀──────│  STRUCT  │
│ (fields) │◀──────│ (shared │──────▶│ (builds) │
└──────────┘       │  state) │       └──────────┘
                   │         │
                   │         │◀──────┌──────────┐
                   │         │──────▶│  SIGNAL  │
                   └─────────┘       │ (routes) │
                                     └──────────┘
```

### What lives in the Depot

The Depot holds **Resources**, **Statuses**, and **Events**.

```
DEPOT STATE (what the player sees in a debug panel):
─────────────────────────────────────────────────
RESOURCES
  silo_01.wheat        = 340 units
  silo_01.capacity     = 500 units
  warehouse.flour      = 80 units
  vault.coins          = 2300

STATUSES  
  field_north.state    = "growing"
  field_north.crop     = "wheat"
  field_north.progress = 0.72
  mill_01.state        = "idle"
  mill_01.health       = 0.95
  truck_03.state       = "delivering"
  truck_03.location    = [34, 78]

EVENTS (recent)
  12:04:03  field_north.harvested     { crop: "wheat", amount: 45 }
  12:04:03  silo_01.received          { item: "wheat", amount: 45 }
  12:04:05  silo_01.threshold_reached { resource: "wheat", pct: 0.90 }
  12:04:06  truck_03.dispatched       { from: "silo_01", to: "mill_01" }
```

The Depot is always visible to the player as a real-time dashboard. This is how they debug cross-language issues — they watch data flow through it.

---

## How Each Language Talks to the Depot

### Growl (Python-like) — Simple and Direct

Growl treats the Depot like a dictionary. No ceremony.

```python
# WRITING to the Depot after a harvest
harvest_amount = harvest(field_north)
depot.put("silo_01.wheat", harvest_amount, add=True)
depot.set("field_north.state", "fallow")

# READING from the Depot to make decisions  
moisture = depot.get("field_north.moisture")
if moisture < 0.3:
    irrigate(field_north)

# EMITTING an event (other languages can hear this)
depot.emit("field_north.harvested", {
    "crop": "wheat",
    "amount": harvest_amount,
    "quality": scan(field_north).quality
})
```

Growl's Depot access is **untyped**. You can put anything in, read anything out. Fast and loose. This means Growl can accidentally put garbage into the Depot that breaks Struct.

### Struct (C#-like) — Typed and Contractual

Struct interacts with the Depot through **typed ports**. You must define what shape of data you expect.

```csharp
// Define a contract for what this building accepts
[DepotPort("silo_01.received")]
public struct IncomingCrop {
    public string CropType;
    public int Amount;
    public float Quality;
}

// The silo class reads from the Depot with type safety
class Silo : Storage {
    Port<IncomingCrop> intake = Depot.Port<IncomingCrop>("silo_01.received");
    
    public override void Update() {
        if (intake.HasData()) {
            IncomingCrop delivery = intake.Read();
            
            // This is type-checked at compile time
            // If Growl emits something missing "Quality", 
            // this fails gracefully with a default value
            Store(delivery.CropType, delivery.Amount);
        }
    }
    
    // Writing back to the Depot
    public void CheckCapacity() {
        float pct = (float)Count / Capacity;
        Depot.Set("silo_01.capacity_pct", pct);
        
        if (pct > 0.9f) {
            Depot.Emit("silo_01.threshold_reached", new {
                Resource = StoredType,
                Percent = pct
            });
        }
    }
}
```

The **Port** system is the key Struct concept. A Port is a typed window into the Depot. If the incoming data doesn't match the type, Struct either fills in defaults or rejects it and logs a **type fault** — which the player sees as a warning light on the building.

### Signal (JS-like) — Event Listeners on the Depot

Signal doesn't read the Depot directly. It **subscribes** to Depot events and reacts.

```javascript
// Listen for silo threshold events
on("silo_01.threshold_reached", async (event) => {
    // Find a truck
    let truck = await request_unit("truck", near(event.source))
    
    if (!truck) {
        depot.emit("logistics.warning", { 
            msg: "no trucks available",
            priority: "high" 
        })
        return
    }
    
    // Find best destination for this resource
    let dest = await find_demand(event.resource)
    
    // Create a delivery job
    let job = await truck.deliver({
        pickup: event.source,
        dropoff: dest,
        resource: event.resource
    })
    
    // When delivery completes, update the Depot
    job.then((result) => {
        depot.put(dest + "." + event.resource, result.amount, add=true)
        depot.emit("delivery.complete", result)
    })
    
    // If delivery fails, retry
    job.catch((err) => {
        depot.emit("delivery.failed", { reason: err, job: job })
        retry(job, { delay: 30, max_attempts: 3 })
    })
})
```

Signal never polls. It only wakes up when events fire. This is efficient but means if an event is missed (e.g. the crystal network has a gap), Signal code simply doesn't run — and the player has to figure out why their trucks stopped moving.

---

## A Complete Pipeline: Wheat to Money

Here's a full end-to-end example of all three languages cooperating.

### Step 1: Growl Grows and Harvests

```python
# Growl program: "field_manager.gwl"
# Runs every game tick on assigned fields

for field in my_fields:
    status = scan(field)
    
    if status.state == "empty":
        # Pick best crop for this soil
        crop = best_crop(status.soil_type, season())
        plant(field, crop)
        depot.set(field.id + ".state", "growing")
        depot.set(field.id + ".crop", crop.name)
    
    elif status.state == "ready":
        result = harvest(field)
        
        # Push to Depot — this is where Growl's job ends
        # It doesn't know or care what happens to the wheat next
        depot.emit("harvest.complete", {
            "source": field.id,
            "crop": result.crop,
            "amount": result.amount,
            "quality": result.quality
        })
        depot.put(field.id + ".state", "empty")
    
    elif status.state == "growing":
        # Maintain the field
        if status.moisture < 0.3:
            irrigate(field)
        if status.pests > 0:
            treat(field, "pesticide")
        depot.put(field.id + ".progress", status.progress)
```

### Step 2: Signal Hears the Harvest and Routes It

```javascript
// Signal program: "harvest_router.sig"
// Listens for harvests and moves crops to storage

on("harvest.complete", async (event) => {
    // Find a silo that has room for this crop type
    let silos = depot.query("silo_*.capacity_pct")
        .filter(s => s.value < 0.95)
        .sort((a, b) => distance(a.source, event.source))
    
    if (silos.length === 0) {
        depot.emit("logistics.crisis", { 
            type: "no_storage",
            crop: event.crop 
        })
        return  // Crops will rot! Player needs to build more silos.
    }
    
    let target_silo = silos[0]
    
    // Dispatch a harvester drone to carry the crops
    let drone = await request_unit("harvester", near(event.source))
    
    await drone.pickup(event.source, event.crop, event.amount)
    await drone.deliver(target_silo.id)
    
    // Drone delivery triggers the Struct silo's intake port
    depot.emit(target_silo.id + ".received", {
        crop: event.crop,
        amount: event.amount,
        quality: event.quality
    })
})
```

### Step 3: Struct Processes the Stored Crops

```csharp
// Struct program: "mill_processor.str"  
// Compiled and installed on the mill building

[DepotPort("silo_01.threshold_reached")]
struct ThresholdAlert {
    public string Resource;
    public float Percent;
}

class GristMill : Processor {
    Port<ThresholdAlert> alert = Depot.Port<ThresholdAlert>("silo_01.threshold_reached");
    
    // Define the recipe
    Recipe flour_recipe = new Recipe {
        Input  = new ItemStack("wheat", 10),
        Output = new ItemStack("flour", 4),
        Time   = 30  // game seconds
    };
    
    public override void Update() {
        if (State == MachineState.Idle && alert.HasData()) {
            ThresholdAlert a = alert.Read();
            
            if (a.Resource == "wheat") {
                StartProcessing(flour_recipe);
            }
        }
    }
    
    protected override void OnProcessingComplete(ItemStack output) {
        // Put the flour into our own storage
        InternalStore.Add(output);
        Depot.Set(Id + ".flour", InternalStore.Count("flour"));
        
        // Tell the world we made flour
        Depot.Emit(Id + ".produced", new {
            Item = output.Name,
            Amount = output.Count
        });
    }
}
```

### Step 4: Signal Sells the Flour

```javascript
// Signal program: "trade_manager.sig"
// Listens for produced goods and sells them

on("mill_*.produced", async (event) => {
    // Check market prices (markets are NPC-run, prices fluctuate)
    let markets = await depot.query("market_*.prices." + event.item)
    let best = markets.sort((a, b) => b.value - a.value)[0]
    
    if (best.value < min_acceptable_price(event.item)) {
        // Price too low, stockpile and wait
        depot.emit("trade.deferred", { item: event.item, reason: "low_price" })
        return
    }
    
    let truck = await request_unit("truck", near(event.source))
    
    let sale = await truck.deliver_and_sell({
        pickup: event.source,
        market: best.id,
        item: event.item,
        amount: event.amount
    })
    
    sale.then((result) => {
        depot.put("vault.coins", result.revenue, add=true)
        depot.emit("trade.complete", {
            item: event.item,
            revenue: result.revenue,
            market: best.id
        })
    })
})
```

### The Full Flow Visualized

```
GROWL                    SIGNAL                   STRUCT
─────                    ──────                   ──────
field scans soil
field plants wheat
  ...growing...
  ...growing...
field harvests wheat
  │
  ├─emit("harvest.complete")
  │                      │
  │                      ├─hears event
  │                      ├─finds nearest silo
  │                      ├─dispatches drone
  │                      ├─emit("silo_01.received")
  │                      │                        │
  │                      │                        ├─Port reads delivery
  │                      │                        ├─stores wheat
  │                      │                        ├─checks capacity: 90%!
  │                      │                        ├─emit("silo_01.threshold_reached")
  │                      │                        │
  │                      ├─hears threshold        │
  │                      │  (different listener)   │
  │                      │                        ├─mill sees threshold
  │                      │                        ├─pulls wheat from silo
  │                      │                        ├─processing...
  │                      │                        ├─emit("mill_01.produced")
  │                      │                        │
  │                      ├─hears production       │
  │                      ├─checks market prices   │
  │                      ├─dispatches truck       │
  │                      ├─sells flour            │
  │                      ├─coins += revenue       │
  │                      │                        │
  ▼                      ▼                        ▼
NEXT CYCLE           WAITING FOR              WAITING FOR
                     NEXT EVENT               NEXT INPUT
```

---

## Cross-Language Bugs (The Fun Part)

### Bug Type 1: Schema Mismatch

Growl emits a harvest event with `"qty"` instead of `"amount"`:

```python
# Growl (the bug)
depot.emit("harvest.complete", {
    "crop": "wheat",
    "qty": 45,          # ← typo, should be "amount"
    "quality": 0.8
})
```

**What happens:**
- Signal's listener picks it up — Signal is loosely typed, so `event.amount` is just `undefined`
- Signal dispatches a drone to pick up `undefined` units of wheat
- The drone arrives, picks up nothing, delivers nothing
- Struct's silo receives a delivery of 0 wheat
- The silo never hits threshold, the mill never runs
- The player sees: crops vanishing from fields, empty silos, idle mills

**How the player debugs:**
The Depot dashboard shows the event log:

```
12:04:03  harvest.complete  { crop: "wheat", qty: 45, quality: 0.8 }
                                              ^^^
                            ⚠ FIELD MISMATCH: Signal listener "harvest_router" 
                              expected field "amount", got "qty"
```

The game highlights the mismatch in amber. The player goes to their Growl script, fixes `qty` → `amount`.

### Bug Type 2: Timing / Race Condition

Signal dispatches a truck to the mill, but Struct hasn't finished compiling the mill yet:

```javascript
// Signal (runs immediately)
on("silo_01.threshold_reached", async (event) => {
    let truck = await request_unit("truck", near("silo_01"))
    await truck.deliver({ pickup: "silo_01", dropoff: "mill_01" })
    //                                                 ^^^^^^
    //                              mill_01 hasn't compiled yet!
})
```

**What happens:**
- The truck drives to where mill_01 should be
- There's nothing there (or a half-built foundation flickering in and out)
- The truck throws a delivery error
- The wheat is stuck on the truck
- Meanwhile the silo fills up, more threshold events fire, more trucks dispatch to nowhere

**How the player debugs:**
They see trucks circling an empty lot. The Depot shows:

```
12:05:00  delivery.failed  { reason: "destination_not_found: mill_01", 
                             truck: "truck_03" }
12:05:00  delivery.failed  { reason: "destination_not_found: mill_01", 
                             truck: "truck_07" }
  ⚠ STRUCT STATUS: mill_01 compilation at 67%... ETA 45 seconds
```

**The fix — Signal needs to wait for Struct:**

```javascript
on("silo_01.threshold_reached", async (event) => {
    // Wait until the mill actually exists
    await depot.when("mill_01.state", state => state !== "compiling")
    
    let truck = await request_unit("truck", near("silo_01"))
    await truck.deliver({ pickup: "silo_01", dropoff: "mill_01" })
})
```

### Bug Type 3: Feedback Loop / Cascade

The most fun bug. Struct's silo emits "threshold_reached" at 90%. Signal dispatches a truck that takes wheat out. This drops it below 90%. Struct stops emitting. Signal stops dispatching. Wheat accumulates. Hits 90% again. Repeat forever.

```
silo hits 90% → Signal dispatches truck → 
truck removes wheat → silo drops to 85% → 
mill never gets enough to start processing →
wheat accumulates → silo hits 90% again →
Signal dispatches another truck → ...
```

The player sees: a single truck endlessly shuffling small amounts of wheat back and forth, the mill perpetually almost-but-never-starting.

**The fix — multiple valid approaches:**

Growl fix (overproduce so the silo stays above threshold):
```python
# Plant more fields, maintain surplus
if depot.get("silo_01.capacity_pct") < 0.5:
    expand_fields("wheat", 2)
```

Struct fix (change the threshold or add hysteresis):
```csharp
// Only emit threshold when going UP past 90%, not on every tick
if (pct > 0.9f && previousPct <= 0.9f) {
    Depot.Emit("silo_01.threshold_reached", ...);
}
```

Signal fix (batch deliveries instead of reacting to every event):
```javascript
on("silo_01.threshold_reached", async (event) => {
    // Debounce — wait 60 seconds collecting before dispatching
    await delay(60)
    let current = depot.get("silo_01.wheat")
    if (current > 200) {  // Only move in bulk
        let truck = await request_unit("truck", near("silo_01"))
        await truck.deliver({ ... })
    }
})
```

The game doesn't tell the player which fix is "right." All three work. The player's choice reflects their thinking style.

---

## The Interop Complexity Curve

### Early Game (Hours 1-3)
One language at a time. Depot is just a scoreboard.
```
Growl writes → Player reads Depot → Player manually triggers next step
```

### Mid Game (Hours 3-8)  
Two languages. First real interop. The player connects Growl output to Struct input.
```
Growl emits → Struct port reads → Things happen automatically
(Player feels powerful)
```

### Late Game (Hours 8-15)
All three languages. Complex pipelines. The player is debugging across language boundaries.
```
Growl emits → Signal routes → Struct processes → Signal sells → Depot updates → Growl reacts
(Player feels like a systems architect)
```

### Endgame (Hours 15+)
The player is optimizing. They're not just making things work — they're making them work efficiently. They start caring about:
- Event batching (reducing Depot traffic)
- Type contracts (preventing schema mismatches at design time)
- Async orchestration patterns (sagas, circuit breakers)
- Monitoring and alerting (writing code that watches other code)

The interop itself becomes the game.

---

## Implementation Notes for Actual Development

### Under the Hood

All three languages compile/interpret down to the same internal representation — a simple instruction set that the game's runtime executes. The Depot is just an in-memory dictionary with a pub/sub event bus bolted on.

```
INTERNAL RUNTIME:
├── Depot (shared state + event bus)
├── Growl VM (interpreted, runs every tick)
├── Struct Compiler → Struct VM (compiled, runs on update cycle)
├── Signal Reactor (event-driven, runs on event dispatch)
└── World Simulation (physics, time, entities)
```

The key constraint: **no language can directly call another language's functions.** They can ONLY communicate through the Depot. This keeps the mental model clean and makes debugging possible — every cross-language interaction leaves a trace in the Depot log.

### The Depot Query Language

For convenience, all three languages share a tiny query syntax for reading Depot state:

```
"silo_01.wheat"                    → single value
"silo_*.wheat"                     → all silos' wheat counts  
"silo_01.*"                        → everything about silo_01
"market_*.prices.flour"            → flour price at every market
"field_*.state == 'ready'"         → all fields ready to harvest
```

This is the one shared syntax across all three languages. It's deliberately simple — just dot paths and wildcards with optional filters. Players learn it once and use it everywhere.
