# The Facility: Tutorial Sequence

## Design Philosophy

No tutorial popups. No floating arrows. No "Press X to interact." The facility itself is the tutorial — its layout, its locked doors, its half-working systems, and the notes left behind by dead engineers. The player learns by exploring a space that was designed (in-fiction) to be operated by competent people. You're figuring out what those people knew.

Every lesson is gated by a **physical problem** the player can see and understand before they understand the code solution. "The lights are off" is obvious. "Write a conditional statement" is not. The game always shows you the problem in the world first, then lets you discover that code is how you fix it.

---

## The Facility Layout

```
                    ┌─────────────────┐
                    │   SURFACE       │
                    │   (buried exit) │
                    └────────┬────────┘
                             │
                    ┌────────┴────────┐
         LEVEL 0   │   ATRIUM        │  ← You wake up here
                    │   (hub room)    │
                    └──┬─────┬─────┬──┘
                       │     │     │
              ┌────────┘     │     └────────┐
              │              │              │
     ┌────────┴───┐  ┌──────┴──────┐  ┌────┴────────┐
L1   │  GROW WING │  │ POWER WING  │  │ LOGISTICS   │
     │  (farm)    │  │ (core)      │  │ WING        │
     │            │  │             │  │ (locked)    │
     └────────────┘  └─────────────┘  └─────────────┘
```

The player explores roughly left-to-right, but can wander. Doors unlock based on problems solved, not arbitrary gates.

---

## BEAT 0: Waking Up (2 minutes)

### What the player sees

Dark. Emergency strips on the floor, dim red. You're in a cot in a small room off the main atrium. The room has:

- A dead tablet on the nightstand (no power)
- A locker with a jumpsuit
- A window looking into the atrium (dark, but you can see shapes)

### What the player does

Gets up. Puts on the jumpsuit (or doesn't — no forced tutorials). Walks into the atrium.

### What the player learns

Movement. Looking around. The vibe: this place has been abandoned for a long time, but it isn't destroyed. It's dormant.

---

## BEAT 1: The Atrium — Orientation (5 minutes)

### What the player sees

A large circular hub. Three corridors branch off, each marked with faded signage:

- **BIOCULTURE LAB** (left) — door slightly ajar, green emergency light
- **POWER SYSTEMS** (center) — door sealed, amber emergency light, a low hum behind it
- **SUPPLY & LOGISTICS** (right) — door sealed, red emergency light, completely dark beyond

A central terminal sits in the middle of the atrium. It's ON — barely. The screen flickers. It's running on whatever trickle of power the core is still producing.

### What the player does

Approaches the terminal. It displays:

```
FACILITY STATUS — AUTO-REPORT
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Core output:     43% capacity
Bioculture:      OFFLINE (no active programs)
Power routing:   DEFAULT (unoptimized)  
Logistics:       OFFLINE (insufficient power)
Life support:    MINIMAL
Last login:      1,847 days ago

Three new alerts. View? _
```

If the player views alerts:

```
ALERT 1 [1847 days old]: Bioculture crop failure. 
  All active seed programs terminated. Grow beds empty.
  Recommend: Reprogram seeds and restart grow cycle.

ALERT 2 [1847 days old]: Core fuel at 43%. 
  Estimated remaining life at current draw: ~8 years.
  Recommend: Reduce power consumption or source fuel.

ALERT 3 [1847 days old]: Logistics wing shut down.
  Insufficient power for full facility operation.
  Reroute power from non-essential systems to restore.
```

### What the player learns

The big picture. Three wings, three problems, one underlying constraint (power). They can see all three corridors from here and choose where to go. The game subtly pulls them left — the Bioculture door is already open, the green light is inviting, and the alert about food makes it feel urgent.

But if they try the other doors first:

- **Power Systems**: Door is sealed with a keypad. A note taped to it reads: *"Override code is in Dr. Vasquez's terminal — Bioculture Lab, office 2."* This sends them to the grow wing anyway, but now they have a secondary goal there.
- **Logistics**: Door doesn't respond at all. A wall panel reads: `INSUFFICIENT POWER — LOGISTICS WING REQUIRES 120kW — CURRENT AVAILABLE: 80kW`. They literally can't open this door until they optimize power elsewhere.

---

## BEAT 2: The Grow Wing — First Contact with Growl (15 minutes)

### Room 1: The Lobby

A hallway with windows into the grow rooms. Through the glass, the player sees rows of empty grow beds under dark UV lights. Dead vines clinging to trellises. Nutrient paste dried in the irrigation tubes.

On the wall: a framed poster. 

```
BIOCULTURE QUICK REFERENCE

Every seed needs a program to grow. 
Use any GROWL TERMINAL to write or modify seed programs.
Minimum viable genome: ROOT, STEM, LEAF, FRUIT.
Flash your program to a seed canister, plant it, and wait.

"If it's not programmed, it's not growing." 
  — Dr. Vasquez, Facility Bioculture Lead
```

This is the only explicit "tutorial" text in the game, and it's a poster on a wall that the player might walk right past. It's flavor. It's worldbuilding. It also tells you everything you need to know.

### Room 2: The Seed Vault

A cold room. Racks of cylindrical canisters with colored caps. A manifest on a clipboard:

```
SEED INVENTORY — LAST AUDIT
━━━━━━━━━━━━━━━━━━━━━━━━━━━
Wheat (base strain)     x24   ← simple, forgiving
Potato (base strain)    x12
Soybean (base strain)   x8
Tomato (hybrid-A)       x4    ← needs more complex genome
Cotton (fiber strain)   x6    ← non-food, but useful later
Lumenbloom (exp.)       x2    ← mysterious, high slot count

Note: All canisters unprogrammed. Previous programs 
wiped during emergency shutdown. Sorry about that.  — R.
```

The player picks up a wheat canister. It goes into their inventory.

### Room 3: The First Terminal

A small desk with a Growl terminal. This is the moment. The player plugs in the wheat canister and sees:

```
GROWL TERMINAL v3.1
━━━━━━━━━━━━━━━━━━
Connected: WHEAT_01 (base strain)
Genome slots: 12 (0 filled)
Energy budget: 100

Slot 1 — ROOT:  [empty]
Slot 2 — STEM:  [empty]
Slot 3 — LEAF:  [empty]
Slot 4 — FRUIT: [empty]

Type HELP for reference. Type LOAD TEMPLATE for examples.
> _
```

### The First Program

Here's where the game has to be very careful. The player might:

**A) Type HELP** — Gets a concise reference card:

```
ROOT FUNCTIONS:
  grow_down(distance)    — extend roots downward
  grow_wide(distance)    — extend roots outward
  absorb("resource")     — pull resource from soil

STEM FUNCTIONS:
  grow_up(distance)      — extend stem upward
  branch(count)          — create branches

LEAF FUNCTIONS:
  spread(area)           — increase leaf surface
  orient(direction)      — angle leaves

FRUIT FUNCTIONS:
  set_fruit("type")      — grain, tuber, fiber
  grow_fruit(energy)     — allocate energy to fruit
  ripen(speed)           — maturation rate
```

**B) Type LOAD TEMPLATE** — Gets a working starter program:

```
Loading template: WHEAT_BASIC

# ROOT
def root(plant, soil):
    grow_down(2)
    absorb("water")

# STEM  
def stem(plant):
    grow_up(3)

# LEAF
def leaf(plant, light):
    spread(4)

# FRUIT
def fruit(plant):
    set_fruit("grain")
    grow_fruit(10)
    ripen(1.0)

Energy cost: 28/100
This template will produce minimal viable wheat.
Flash to canister? (yes/no) > _
```

**C) Just start typing** — The terminal has inline error messages:

```
> def root(plant, soil):
>     grow(5)
ERROR: Unknown function 'grow'. Did you mean 'grow_down' or 'grow_wide'?
```

The game meets the player where they are. If they load the template and flash it, that's fine — they'll get wheat. If they write from scratch, they learn more. Both paths lead to a planted seed.

### Room 4: The First Grow Bed

Adjacent to the terminal room. One grow bed with soil, a single UV light overhead (dim — only getting 75% power), and an irrigation nozzle (functional).

The player plants their programmed wheat canister. The game does NOT timeskip. They watch the seed do what they told it to do:

- Roots extend downward (they can see a cross-section view of the soil)
- Stem pushes up
- Leaves spread
- Grain heads form slowly

If they used the basic template, the wheat grows. It's slow. The yield is small. But something grew. **This is the first dopamine hit: code made a living thing.**

The terminal nearby shows real-time telemetry:

```
PLANT STATUS — WHEAT_01
━━━━━━━━━━━━━━━━━━━━━━
Height:     12 cm (growing)
Water:      0.6 (adequate)
Energy:     0.4 (low — dim light)  ← HINT
Roots:      shallow
Yield est:  ~3 grain units

⚠ Low energy production. Consider improving LEAF gene 
  or increasing light availability.
```

That warning is the first nudge toward the core loop: your code works, but it could be better, and the environment is constraining you.

---

## BEAT 3: The Iteration Loop (20 minutes)

### The Second Planting

The player has 23 more wheat canisters. They go back to the terminal. Now they know the basics. The question becomes: can I do better?

Maybe they try bigger leaves:

```python
def leaf(plant, light):
    spread(8)              # Double the area
    orient(light.direction) # Track the light
```

This costs more energy but produces more. They plant it, watch it grow, compare the yield. Better.

Maybe they notice the soil is dry and improve roots:

```python
def root(plant, soil):
    water_dir = soil.scan("water")
    grow_toward(water_dir, 3)
    grow_wide(2)
    absorb("water")
    absorb("nitrogen")
```

More water, more nutrients. Plant grows faster.

### The Multi-Bed Discovery

The grow wing has **six beds**, but only one UV light is on. A panel on the wall:

```
GROW ROOM — LIGHTING CONTROL
━━━━━━━━━━━━━━━━━━━━━━━━━━━
Bed 1: UV ON  (50W)
Bed 2: UV OFF (not enough power)
Bed 3: UV OFF
Bed 4: UV OFF
Bed 5: UV OFF
Bed 6: UV OFF

Total allocated: 50W / 200W capacity
Available from grid: 80W spare

[REROUTE POWER] button
```

The player can turn on a second light (bringing spare power down to 30W). Maybe a third. But each light they turn on is power they can't use elsewhere.

OR — and this is the key moment — they realize they can write seed programs that work in dimmer light and stretch one UV across two beds. The game presents the tradeoff physically and lets the player decide: brute force (more power) or clever code (better seeds)?

### Dr. Vasquez's Office

Off the grow wing lobby. The player finds it while exploring. Inside:

- **The power wing keycode** on a sticky note on the monitor (pays off the earlier hook)
- **Dr. Vasquez's research journal** — a series of terminal entries:

```
JOURNAL — DR. VASQUEZ
━━━━━━━━━━━━━━━━━━━━

Entry 14:
Breakthrough with the companion gene today. Planted soy 
next to wheat and wrote a simple neighbor-detection function.
The wheat's nitrogen uptake doubled. Two crops, cooperating 
through code. This is what the genome system was designed for.

Entry 22:
The experimental strain — Lumenbloom — is extraordinary.
32 genome slots. Energy budget of 400. I've been writing 
programs for it that I never imagined possible. It doesn't
just grow — it computes. More later.

Entry 31:
Core fuel is dropping faster than projected. I've been 
optimizing seed programs to use less light but we need a 
real solution. Talked to Okonkwo in Power Systems about 
writing Struct code to improve core efficiency. She says
the routing tables haven't been updated in years. Could
probably squeeze another 15% out of what we have.

Entry 38 (final):
Evacuation ordered. I've wiped the seed programs as 
protocol demands — can't risk uncontrolled growth if
the facility is breached. But I've left the canisters
and the reference docs. If anyone finds this place,
the seeds are good. You just have to teach them 
how to grow again.

Good luck.
```

This journal does several things:

- Foreshadows companion planting (an advanced technique)
- Teases the Lumenbloom as an endgame seed
- Explicitly points the player toward the Power Wing and Struct
- Establishes that the world has history and people you'll never meet
- Frames the player's actions as continuing someone's work

---

## BEAT 4: The Power Wing — Struct Introduction (25 minutes)

### Unlocking the Door

The player uses the keycode from Vasquez's office. The power wing door opens. The hum gets louder. Emergency amber lighting. It's warmer in here.

### Room 1: The Core Observation Deck

A window looking into the reactor chamber. The core glows — visibly dimmer on one side. Readouts everywhere:

```
NUCLEAR CORE — STATUS
━━━━━━━━━━━━━━━━━━━━
Fuel remaining:    43%
Output:           430 kW
Theoretical max:  1000 kW
Efficiency:       71% (routing losses)
Core temp:        stable

POWER ROUTING TABLE
━━━━━━━━━━━━━━━━━━
Life support:     100 kW  (fixed — cannot reduce)
Grow wing:         50 kW  (1 of 6 lights active)
Terminals:         20 kW
Spare:             80 kW  (unallocated)
Logistics wing:    OFFLINE (needs 120 kW)

Total draw:       170 kW of 430 kW available
                  260 kW WASTED due to routing inefficiency
```

The critical number: **260 kW wasted.** The core produces enough power. The problem is the routing — it's running on ancient default settings. Power goes through a chain of transformers and switches, and each one is losing energy because it's misconfigured.

But the routing system is written in **Struct**, not Growl. There's a Struct terminal here — different from the Growl terminals. Heavier. More industrial. A compiler status light on top (currently dark).

### Room 2: The Struct Terminal

The player sits down:

```
STRUCT COMPILER v2.0
━━━━━━━━━━━━━━━━━━━━
WARNING: Power routing firmware is outdated.
Current efficiency: 71%
Estimated recoverable waste: 180+ kW

Loaded module: PowerRouter (readonly — compile new version to overwrite)

Type VIEW to inspect current code.
Type HELP for Struct reference.
> _
```

The player types VIEW:

```csharp
// PowerRouter — FACTORY DEFAULT (installed day 1, never updated)
class PowerRouter : SystemModule {
    
    // All power goes through a single bus
    public override void Route(PowerGrid grid) {
        Bus main = grid.MainBus;
        
        foreach (Wing wing in grid.Wings) {
            main.Send(wing, wing.DefaultAllocation);
            // DefaultAllocation is a fixed number set at install
            // It doesn't account for actual demand
        }
    }
}
// EFFICIENCY NOTE: Single bus = single bottleneck.
// Every transfer through MainBus loses 12% to heat.
// Multiple direct routes would eliminate this.
```

The player can SEE the problem in the code. All power is routed through one bus, and that bus has a 12% loss per transfer. The code is simple but wasteful.

### The First Struct Program

Here's where Struct reveals itself as a different way of thinking. In Growl, you just call functions and things happen immediately. In Struct, you have to define a structure, then compile it, then it runs.

The simplest improvement:

```csharp
class PowerRouter : SystemModule {
    
    public override void Route(PowerGrid grid) {
        // Direct routes — skip the main bus entirely
        foreach (Wing wing in grid.Wings) {
            if (wing.IsOnline) {
                DirectLine line = grid.CreateDirect(wing);
                line.Send(wing.CurrentDemand);
                // Direct lines only lose 3% instead of 12%
            }
        }
    }
}
```

The player hits COMPILE. Unlike Growl, which runs instantly, Struct compiles:

```
Compiling PowerRouter...
[████████░░░░░░░░] 52%
```

It takes real time. Maybe 20 seconds. The player waits. The compiler light on the terminal blinks amber.

```
Compilation complete. 0 errors. 1 warning.
WARNING: Wing "logistics" demand exceeds allocation. 
         Will be powered if sufficient capacity.

Installing PowerRouter...
Done. Rerouting power.

NEW EFFICIENCY: 89% (+18%)
RECOVERED: 184 kW

UPDATED POWER ROUTING TABLE
━━━━━━━━━━━━━━━━━━━━━━━━━━━
Life support:     100 kW
Grow wing:         50 kW  (can now support more)
Terminals:         20 kW
Spare:            260 kW  ← was 80!
Logistics wing:   STILL OFFLINE (120 kW available now, 
                  but door mechanism needs manual reset)

Total draw:       170 kW of 430 kW
Waste:             76 kW (down from 260)
```

**The lights in the facility get brighter.** Physically, visibly. The amber emergency tone in the power wing warms up. The hum changes pitch. The player just improved the world by writing code, and they can feel it.

Now they have 260 kW of spare power. They can go back to the grow wing and turn on ALL the UV lights. Their wheat programs that were struggling in dim light now thrive. Or they can keep the lights at one and spend the power elsewhere.

### The Struct Lesson

The key difference the player just felt:

- **Growl**: Write it, run it, see it immediately, tweak it, repeat. Growl is a conversation with the system.
- **Struct**: Design it, compile it, wait, it either works or it doesn't. Struct is a blueprint. You think first, then commit. But the results are reliable and efficient in ways Growl can't match.

### Okonkwo's Notes

In a locker near the Struct terminal, engineer Okonkwo's notes:

```
OKONKWO — ENGINEERING NOTES
━━━━━━━━━━━━━━━━━━━━━━━━━━━

The power router is just the start. The whole grid
is running on factory defaults. Every subsystem —
the transformers, the capacitor banks, the backup
switches — they're all Struct modules that could
be rewritten.

I've been wanting to write a LoadBalancer class that
dynamically shifts power based on real-time demand.
Grow wing needs more power during UV cycles, less at
night. If we could shift that power to logistics 
during off-hours, we'd effectively have more total
capacity without touching the core.

Never got around to it. The templates are in 
my terminal if anyone wants to finish the job.

Also: Vasquez keeps asking me about compiling
Struct modules that her Growl programs can talk to.
She wants the irrigation pumps to respond to 
signals from her seed programs. That's cross-system
work — you'd need the Depot network for that. 
Logistics wing has a Depot terminal. If we ever
get that wing back online...
```

This does several things:

- Shows the player that Struct has depth beyond what they just did
- Introduces the concept of dynamic power management (advanced Struct)
- Name-drops the Depot and cross-language interop
- Points toward the Logistics wing as the next unlock
- Connects back to Vasquez's story — these people worked together

---

## BEAT 5: The Return to Growing (15 minutes)

### More Power, More Possibilities

The player goes back to the grow wing with their new power surplus. They can now:

- Turn on all six UV lights (costs 200 kW, leaving 60 kW spare)
- Or turn on three and save power for the logistics wing
- Or turn on two and write better dim-light seed programs

Each choice is valid and leads to different gameplay.

### The Second Crop

By now the player has harvested their first wheat. They've seen the full cycle. They have a stockpile of basic grain — but the game subtly tells them it's not enough:

```
FACILITY NUTRITION REPORT
━━━━━━━━━━━━━━━━━━━━━━━━
Current food supply: 14 days (wheat grain only)
Nutritional balance: POOR
  Carbohydrates: adequate
  Protein: critically low
  Vitamins: critically low
  
Recommendation: Diversify crops.
Available protein source: Soybean (in seed vault)
Available vitamin source: Tomato (in seed vault)
```

The player now has a reason to program a second seed type. Soybeans have different needs than wheat — different soil requirements, different light sensitivity, different fruiting behavior. The same four required genes need completely different implementations.

This is where the player starts to feel like a bioengineer rather than someone following a tutorial. They're making real decisions about what to grow, how to allocate genome slots, and how to manage the energy budget across multiple crops.

### The Companion Discovery

If the player read Vasquez's journal, they might try planting soy next to wheat and writing the companion gene. If they didn't read it, they might discover it accidentally — or through a hint on a poster in the grow wing:

```
BIOCULTURE BEST PRACTICES
Tip #4: Plants can sense their neighbors.
Use neighbors.nearby() in any gene to detect 
adjacent plants and adapt behavior accordingly.
```

The first time a player writes a companion gene and watches two crops help each other grow, it feels like a discovery. They weren't told to do it. They figured out that the system supports it and made it happen.

---

## BEAT 6: The Logistics Wing — Signal and the Depot (30 minutes)

### Opening the Door

The player now has enough power. They go to the logistics wing door and reroute power:

```
LOGISTICS WING — POWER REQUEST
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Required: 120 kW
Available: 260 kW
Allocate? (yes/no) > yes

Powering up logistics wing...
[████████████████] 100%

Lights activating.
Systems coming online.
Depot network initializing...
```

The door opens. White lights flicker on. This wing is cleaner, more modern-looking than the others. Screens everywhere.

### Room 1: The Depot Hub

A circular room with a central holographic display — the Depot visualization. When it boots up, it's mostly empty:

```
DEPOT — SHARED STATE NETWORK
━━━━━━━━━━━━━━━━━━━━━━━━━━━

RESOURCES
  (none tracked)

STATUSES
  core.output        = 430
  core.efficiency    = 0.89
  growroom.lights    = 3     (or however many the player turned on)

EVENTS
  (no recent events)

Connected systems: Power (Struct), Bioculture (Growl)
Signal programs: 0 active
```

The Depot exists. It's been quietly tracking a few things from the Struct power router. But nothing is really using it yet. It's an empty nervous system waiting for a brain.

### Room 2: The Signal Terminal

Different again from both Growl and Struct terminals. Multiple screens. A real-time event log scrolling (mostly empty). The aesthetic is mission control.

```
SIGNAL RUNTIME v1.4
━━━━━━━━━━━━━━━━━━
Active listeners: 0
Event queue: empty
Depot connection: live

No programs loaded. This system responds to events 
from other systems. It does not run on its own.

Type HELP for reference.
> _
```

The player types HELP:

```
SIGNAL REFERENCE
━━━━━━━━━━━━━━━━
Signal programs don't run in loops like Growl.
Signal programs don't compile like Struct.
Signal programs LISTEN and REACT.

Core syntax:
  on("event.name", async (data) => {
      // do something when event fires
  })

Depot access:
  depot.get("key")              // read a value
  depot.put("key", value)       // write a value
  depot.emit("event", data)     // fire an event
  depot.query("pattern")        // search values
  
Logistics functions:
  request_unit(type, location)  // get a drone or truck
  find_nearest(type, location)  // locate something
  dispatch(unit, task)          // send a unit somewhere
```

### The Bridge Moment

The player's grow room is producing wheat and soy. But the grain just sits in the grow beds. There are storage silos visible through a window in the logistics wing — but nothing moves the food there.

On a whiteboard near the Signal terminal:

```
TODO (never finished):
- Write harvest listener that auto-moves 
  crops from grow beds to storage
- Need Growl programs to emit "harvest.complete" events
- Signal program catches events, dispatches drones
- Chen said the drones are charged and ready in Bay 3

START SIMPLE. One listener. One drone. One route.
```

This is the interop tutorial. The player needs to:

1. **Go back to the Growl terminal** and add one line to their wheat program:

```python
# At the end of the harvest section:
depot.emit("harvest.complete", {
    "source": field.id,
    "crop": result.crop,
    "amount": result.amount
})
```

2. **Go to the Signal terminal** and write their first listener:

```javascript
on("harvest.complete", async (event) => {
    let drone = await request_unit("drone", near(event.source))
    await drone.pickup(event.source, event.crop, event.amount)
    await drone.deliver("silo_01")
})
```

3. **Watch it happen.** The wheat grows, the Growl program emits an event, the Depot catches it, Signal's listener fires, a drone launches from Bay 3, flies to the grow room, picks up the wheat, and delivers it to the silo.

The first time this chain executes — code in three different languages, written at three different terminals, cooperating through the Depot — the player understands the entire system. Not because anyone explained it, but because they built it with their hands.

### The Depot Comes Alive

Now the Depot display in the hub room is active:

```
DEPOT — SHARED STATE NETWORK
━━━━━━━━━━━━━━━━━━━━━━━━━━━

RESOURCES
  silo_01.wheat       = 34
  silo_01.soy         = 12

STATUSES
  core.output         = 430
  field_01.state      = "growing"
  field_02.state      = "ready"
  drone_01.state      = "returning"
  drone_01.location   = [14, 7]

EVENTS (last 60 seconds)
  14:22:01  harvest.complete     { crop: wheat, amount: 8 }
  14:22:03  drone_01.dispatched  { to: field_01 }
  14:22:18  drone_01.delivered   { to: silo_01, amount: 8 }
```

The player can stand in the Depot hub and watch their entire colony operate. Growl events firing. Signal listeners reacting. Struct systems humming. Data flowing through the Depot like blood through veins.

---

## BEAT 7: The First Crisis (10 minutes)

### Something breaks.

The game waits until the player has all three systems running, then introduces the first unscripted problem. This isn't a triggered event — it emerges from the simulation.

Possible crises (whichever happens first naturally):

**Drought:** The irrigation system's water reservoir runs low. Plants start dying. The player's seed programs don't have drought genes. They either need to quickly reprogram seeds with drought tolerance, or find and fix the water recycler (a Struct problem), or reroute a drone to bring water from an external source (Signal).

**Pest outbreak:** Something got into the grow room. Bugs eating the soy. The player's seeds have no defense genes. Do they write a toxin gene? A thorn gene? Do they write a Signal program to dispatch a maintenance drone to spray pesticide? Do they accept the loss and replant?

**Power fluctuation:** The core hiccups. Power drops to 35% for a few minutes. Lights dim. Any systems the player left at razor-thin margins go offline. Their carefully optimized power router might not handle the transient gracefully. Do they add error handling to their Struct code? Do they write Growl seeds that can survive brief darkness? Do they write a Signal alert system that notifies them of power drops?

The crisis isn't scripted, but the facility is tuned so that SOMETHING will go wrong within the first hour if the player hasn't over-engineered their systems. This teaches the most important lesson: **your code isn't done when it works. It's done when it handles failure.**

---

## BEAT 8: The Outside (when the player is ready)

### The Surface Door

In the atrium, there's a staircase going up. It's been accessible the whole time but there's been no reason to go up — everything interesting was below. At some point the player goes up and finds:

A heavy door. A window in it. Through the window: the alien planet surface. Harsh light. Strange soil. Weather.

```
SURFACE ACCESS
━━━━━━━━━━━━━━
Atmospheric composition: breathable (barely)
Temperature: variable (range: -12°C to 47°C)
Soil type: calciumite /iteite shale (unknown fertility)
Weather: dust storms frequent
Flora: none detected
Fauna: unknown

⚠ Surface conditions are uncontrolled. 
  Indoor growing parameters will not apply.
  Seed programs must account for weather, 
  temperature, soil chemistry, and potential threats.

Open door? _
```

The player steps outside. The world opens up. Everything they learned in the facility still applies, but the constraints are completely different:

- No UV lights — real sunlight, too much of it sometimes, not enough other times
- No controlled soil — alien chemistry, unknown nutrients, possible toxins
- No irrigation — rain is unpredictable, droughts are real
- No walls — wind, temperature swings, unknown creatures

Every seed program they wrote for the facility needs to be rewritten or heavily modified. The genome slots they left empty now matter desperately — they need weather resistance, deep roots, toxin filtering.

And out there in the distance, visible from the facility entrance: other structures. Other facilities. Roads connecting them. A whole colony, dormant, waiting to be brought back online.

The tutorial is over. The game begins.

---

## Pacing Summary

```
Beat 0: Wake up                          2 min    No code
Beat 1: Atrium orientation               5 min    No code (reading)  
Beat 2: First seed program (Growl)       15 min    First Growl code
Beat 3: Iteration and experimentation    20 min    Growl mastery
Beat 4: Power wing (Struct)              25 min    First Struct code
Beat 5: Return to growing                15 min    Growl + Struct synergy
Beat 6: Logistics wing (Signal + Depot)  30 min    First Signal code, interop
Beat 7: First crisis                     10 min    Cross-language debugging
Beat 8: The surface                      Open      Game begins
                                        ─────
                              Total:    ~2 hours
```

The player goes from "what is this place" to "I am orchestrating a self-sustaining colony through three programming languages" in about two hours, and at no point did a floating tooltip tell them to press a button.
