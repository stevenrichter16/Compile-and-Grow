# GROWL LANGUAGE SPECIFICATION v1.0
## Complete Syntax Reference for Parser & Compiler Implementation

---

## 1. FOUNDATIONAL DESIGN DECISIONS

### Philosophy
- **Python-inspired** surface syntax (indentation-scoped, readable, minimal ceremony)
- **Full OOP** — classes, inheritance, interfaces, traits, operator overloading
- **Dynamically typed** with optional type annotations (annotations enable compile-time energy cost analysis)
- **Biological metaphor woven into language primitives** — not bolted on as a library
- **Errors produce mutations, not crashes** — the organism interprets bad code as stress
- **Memory is a first-class cost** — storing state has explicit energy implications

### Encoding & Whitespace
- UTF-8 source encoding
- Indentation: 4 spaces per level (tabs rejected by parser with a friendly error)
- Line endings: LF or CRLF accepted, normalized to LF internally
- Maximum line length: none enforced, but terminal UI wraps at 80 chars
- Blank lines are ignored between statements

### Comments
```
# Single line comment

## Doc comment (attached to next declaration)
## These are extractable by the in-game help system.
## Players can document their organisms and the game
## will display these in the seed canister inspector.

#! Warning comment — displayed as amber in the terminal
#! These show up in the genome analysis panel

###
Block comment.
Can span multiple lines.
Useful for disabling large sections of code.
###
```

---

## 2. PRIMITIVE TYPES

### Numeric
```
# Integers
x = 42
y = -7
big = 1_000_000          # underscore separators allowed
hex_val = 0xFF
bin_val = 0b1010

# Floats
pi = 3.14159
small = 0.001
sci = 6.022e23
negative_exp = 1.5e-4

# Numeric literals can have unit suffixes (cosmetic, no runtime effect,
# but the IDE/terminal highlights them for readability)
dist = 5.0cm
temp = 22.5C
weight = 340g
time = 60s
pct = 85%                # syntactic sugar for 0.85
energy = 12kW
```

### Strings
```
name = "wheat"
desc = 'also valid with single quotes'

# String interpolation with ${}
greeting = "Organism ${org.name} is ${org.maturity * 100}% mature"

# Multi-line strings
journal = """
    This organism was designed to
    produce bioluminescent output
    in low-radiation environments.
"""

# Raw strings (no escape processing)
pattern = r"silo_\d+\.wheat"

# Escape sequences: \n \t \\ \" \' \$ \0
```

### Booleans
```
alive = true
dead = false

# Truthy/falsy rules:
# Falsy: false, 0, 0.0, "", none, empty collections
# Truthy: everything else
```

### None
```
result = none            # Growl uses 'none' not 'None' or 'null'
```

### Colors (built-in literal type)
```
# Hex color literals — first-class because organisms have visual properties
red = #FF0000
soft_green = #B4FFB4
warm_white = #FFFAF0

# RGB constructor
c = rgb(200, 255, 220)

# RGBA with alpha
c = rgba(200, 255, 220, 0.5)

# Named color constants
c = color.red
c = color.forest_green
c = color.amber
```

### Vectors (built-in literal type)
```
# 2D vector literal
dir = <3, 4>
normalized = <0.6, 0.8>

# 3D vector literal  
pos = <1.0, 2.5, -0.3>

# Vector operations are built-in
sum = <1, 2> + <3, 4>         # <4, 6>
scaled = <1, 2> * 3           # <3, 6>
dot = <1, 2> ** <3, 4>        # 11 (dot product)
cross = <1, 0, 0> ^^ <0, 1, 0>  # <0, 0, 1> (cross product, 3D only)
mag = |<3, 4>|                # 5.0 (magnitude)
unit = ^<3, 4>                # <0.6, 0.8> (normalize)
```

### Ranges
```
r = 0..10               # exclusive end: 0, 1, 2, ..., 9
r = 0..=10              # inclusive end: 0, 1, 2, ..., 10
r = 0..10 by 2          # stepped: 0, 2, 4, 6, 8
r = 10..0 by -1         # reverse: 10, 9, 8, ..., 1
r = 0.0..1.0 by 0.1     # float range: 0.0, 0.1, 0.2, ..., 0.9
```

---

## 3. COLLECTION TYPES

### Lists
```
items = [1, 2, 3, 4, 5]
mixed = [1, "two", 3.0, true]
nested = [[1, 2], [3, 4]]
empty = []

# Access
first = items[0]
last = items[-1]
slice = items[1..3]       # [2, 3]
slice = items[2..]        # [3, 4, 5]

# List comprehensions
squares = [x * x for x in 0..10]
filtered = [x for x in items if x > 2]
coords = [<x, y> for x in 0..5 for y in 0..5]

# Methods
items.push(6)
items.pop()
items.insert(0, 99)
items.remove(3)
items.contains(2)         # true
items.length              # property, not method
items.sort()
items.sort(by: fn(a, b) -> a > b)
items.reverse()
items.map(fn(x) -> x * 2)
items.filter(fn(x) -> x > 2)
items.reduce(0, fn(acc, x) -> acc + x)
items.each(fn(x) -> log(x))
items.any(fn(x) -> x > 4)       # true if any match
items.all(fn(x) -> x > 0)       # true if all match
items.find(fn(x) -> x > 3)      # first match or none
items.flatten()                   # flatten nested lists
items.zip(other_list)             # pairs: [(a1,b1), (a2,b2), ...]
items.unique()                    # remove duplicates
items.count(fn(x) -> x > 3)     # count matches
items.min()
items.max()
items.sum()
items.avg()
items.sample()                    # random element
items.sample(3)                   # 3 random elements
items.shuffle()
```

### Dictionaries
```
data = {
    "crop": "wheat",
    "amount": 45,
    "quality": 0.8
}

# Short-hand when key matches variable name
crop = "wheat"
amount = 45
data = { crop, amount }    # same as {"crop": crop, "amount": amount}

# Access
val = data["crop"]
val = data.crop             # dot access works for string keys that are valid identifiers
val = data.get("missing", default="none")

# Methods
data.keys()
data.values()
data.entries()              # list of (key, value) tuples
data.has("crop")            # true
data.remove("crop")
data.merge(other_dict)      # returns new dict with both
data.length

# Dict comprehension
doubled = {k: v * 2 for k, v in data.entries() if v is num}
```

### Tuples (immutable)
```
point = (3, 4)
rgb = (255, 128, 0)

# Destructuring
(x, y) = point
(r, g, b) = rgb

# Named tuples
point = (x: 3, y: 4)
val = point.x              # 3

# Tuples are immutable — you can't reassign elements
# point[0] = 5             # ERROR
```

### Sets
```
unique = {1, 2, 3, 4}      # note: {} alone is a dict, not a set
also = set([1, 2, 2, 3])   # {1, 2, 3}

unique.add(5)
unique.remove(3)
unique.contains(2)          # true

# Set operations
a = {1, 2, 3}
b = {2, 3, 4}
a | b                       # union: {1, 2, 3, 4}
a & b                       # intersection: {2, 3}
a - b                       # difference: {1}
a ^ b                       # symmetric difference: {1, 4}
```

---

## 4. VARIABLES & ASSIGNMENT

### Declaration
```
# Variables are declared by assignment (no keyword needed)
x = 10
name = "lumenvine"

# Multiple assignment
a, b, c = 1, 2, 3

# Swap
a, b = b, a

# Augmented assignment
x += 5
x -= 2
x *= 3
x /= 2
x //= 3                    # integer division
x %= 4                     # modulo
x **= 2                    # exponent
```

### Constants
```
const MAX_HEIGHT = 100
const GROWTH_RATE = 0.05
const SPECIES_NAME = "Lumenvine"

# Constants cannot be reassigned after declaration
# MAX_HEIGHT = 200          # ERROR: cannot reassign constant
```

### Type Annotations (Optional)
```
# Type annotations are optional but enable better energy analysis
# and better error messages from the terminal

x: int = 42
name: str = "wheat"
rate: float = 0.05
alive: bool = true
pos: vec2 = <3, 4>
pos3: vec3 = <1, 2, 3>
c: color = #FF0000
items: list[int] = [1, 2, 3]
data: dict[str, float] = {"nitrogen": 0.5}
point: (int, int) = (3, 4)
callback: fn(int) -> bool = fn(x) -> x > 5

# Union types
value: int | float = 3.14
result: str | none = none

# The 'any' type disables type checking for that variable
wild: any = "could be anything"
```

---

## 5. OPERATORS

### Arithmetic
```
a + b           # addition
a - b           # subtraction
a * b           # multiplication
a / b           # float division (always returns float)
a // b          # integer division (floor)
a % b           # modulo
a ** b          # exponentiation
-a              # unary negation
```

### Comparison
```
a == b          # equality
a != b          # inequality
a < b           # less than
a > b           # greater than
a <= b          # less or equal
a >= b          # greater or equal

# Chained comparisons (like Python)
0 < x < 10     # true if x is between 0 and 10 exclusive
a <= b <= c     # true if b is between a and c inclusive
```

### Logical
```
a and b         # logical AND (short-circuit)
a or b          # logical OR (short-circuit)
not a           # logical NOT

# Nullish coalescing
value = data.get("key") ?? "default"    # use right side if left is none
```

### Bitwise (available but rarely needed in biological programming)
```
a & b           # bitwise AND (also set intersection — context-dependent)
a | b           # bitwise OR (also set union)
a ^ b           # bitwise XOR (also set symmetric difference)
~a              # bitwise NOT
a << n          # left shift
a >> n          # right shift
```

### Identity & Membership
```
a is b          # identity check (same object)
a is not b      # negated identity
a is int        # type check
a is not none   # none check

x in list       # membership test
x not in list   # negated membership
"key" in dict   # key existence
```

### Pipeline Operator
```
# Pass a value through a chain of function calls
# Left-hand value becomes the first argument of the right-hand function

result = soil.scan("water") |> grow_toward(3) |> clamp(0, 10)

# Equivalent to:
result = clamp(grow_toward(soil.scan("water"), 3), 0, 10)

# With lambdas in the pipeline
nutrients = org.nutrients.values()
    |> filter(fn(v) -> v < 0.3)
    |> map(fn(v) -> v * 2)
    |> sort()

# Pipeline is purely syntactic sugar but makes data transformation
# chains much more readable, which matters when players are writing
# complex organism logic
```

### Spread Operator
```
list1 = [1, 2, 3]
list2 = [0, ...list1, 4]    # [0, 1, 2, 3, 4]

dict1 = {"a": 1, "b": 2}
dict2 = {...dict1, "c": 3}  # {"a": 1, "b": 2, "c": 3}

# In function calls
args = [1, 2, 3]
some_function(...args)
```

---

## 6. CONTROL FLOW

### Conditionals
```
if moisture < 0.3:
    irrigate()

if temp > 40:
    leaf.close_stomata()
elif temp > 30:
    leaf.open_stomata(0.5)
elif temp > 20:
    leaf.open_stomata(0.8)
else:
    leaf.open_stomata(1.0)

# Inline conditional expression
action = "close" if moisture < 0.2 else "open"

# Guard clauses (return/skip early)
if not org.alive: return
if org.energy < 5: return
```

### Loops
```
# For loops iterate over any iterable
for item in items:
    process(item)

# With index
for i, item in items.enumerate():
    log("${i}: ${item}")

# Range-based
for i in 0..10:
    grow_segment(i)

for i in 0..100 by 5:
    scan_at_depth(i)

# While loops
while org.water < 0.5:
    root.absorb("water")

# Loop control
for item in items:
    if item == none: continue      # skip this iteration
    if item.toxic: break           # exit loop entirely

# Loop-else (runs if loop completes without break)
for pest in env.threats.pests:
    if pest.weakness == "alkaloid":
        defense.produce_toxin("alkaloid")
        break
else:
    # No pest was weak to alkaloids — try physical defense
    defense.grow_thorns()

# Infinite loop (for organisms that need persistent behavior)
loop:
    scan_environment()
    wait(1)                         # wait 1 tick
    if shutdown_requested: break
```

### Match (Pattern Matching)
```
match env.soil.type:
    case "loam":
        root.grow_wide(4)
    case "clay":
        root.grow_down(6)
        root.grow_thick(2)
    case "sand":
        root.grow_deep(8)
        root.absorb_filtered("water")  # sand drains fast
    case "calciumite" | "ite" | "ite_ite":
        root.grow_down(2)              # alien soil types — careful
        root.exude("acid", 0.1)        # dissolve minerals
    case _:
        root.grow_down(3)              # default fallback

# Match with destructuring
match scan_result:
    case {type: "water", distance: d} if d < 5:
        root.grow_toward("water", d)
    case {type: "water", distance: d}:
        log("Water detected but too far: ${d}")
    case {type: "toxin", name: n}:
        warn("Toxin detected: ${n}")
    case none:
        warn("Scan returned nothing")

# Match with type checking
match product:
    case p is Food:
        produce(p, location="tips")
    case p is Chemical:
        emit(p)
    case p is Material:
        produce(p, location="stem")

# Match on tuples
match (org.water, org.energy):
    case (w, e) if w < 0.2 and e < 10:
        enter_dormancy()
    case (w, _) if w < 0.2:
        root.absorb("water")
    case (_, e) if e < 10:
        photo.retrieve_energy(5)
    case _:
        grow_normally()
```

### Try / Recover (Error Handling)
```
# Growl doesn't crash on errors — it MUTATES.
# But you can catch errors to handle them gracefully.

try:
    root.grow_toward(water_dir, 5)
recover err:
    # err contains info about what went wrong
    match err:
        case GrowthBlocked(obstacle):
            root.grow_wide(2)       # go around
            log("Obstacle hit: ${obstacle}")
        case ResourceDepleted(resource):
            warn("Out of ${resource}")
        case EnergyInsufficient(needed, available):
            photo.set_metabolism(0.5)  # slow down to save energy
        case _:
            warn("Unknown error: ${err}")

# 'recover' is used instead of 'catch' because the organism
# is recovering from stress, not catching an exception.
# This framing reinforces the biological metaphor.

# If no recover block: the organism takes stress damage proportional
# to the error severity. Bad code = stressed organism = visible wilting.

# Finally equivalent
try:
    risky_operation()
recover err:
    handle(err)
always:
    cleanup()
```

---

## 7. FUNCTIONS

### Basic Functions
```
fn grow_toward_water(depth):
    water_dir = env.soil.scan("water")
    root.grow_toward(water_dir, depth)
    return root.sense_moisture()

# With type annotations
fn grow_toward_water(depth: float) -> float:
    water_dir = env.soil.scan("water")
    root.grow_toward(water_dir, depth)
    return root.sense_moisture()

# Default parameters
fn grow_root(depth: float = 3, resource: str = "water") -> float:
    dir = env.soil.scan(resource)
    root.grow_toward(dir, depth)
    return root.sense_moisture(dir)

# Named arguments at call site
grow_root(depth: 5, resource: "nitrogen")
grow_root(resource: "iron")              # depth uses default

# Variadic arguments
fn log_all(*messages):
    for msg in messages:
        log(msg)

log_all("first", "second", "third")

# Keyword arguments
fn configure(**options):
    for key, val in options.entries():
        org.memory[key] = val

configure(mode="defensive", threshold=0.3)
```

### Lambda / Anonymous Functions
```
# Short lambda syntax
double = fn(x) -> x * 2
is_ripe = fn(fruit) -> fruit.maturity > 0.9

# Multi-line lambda
process = fn(item):
    cleaned = clean(item)
    validated = validate(cleaned)
    return validated

# Used inline
threats = env.threats.pests.filter(fn(p) -> p.size > 0.5)
nearest = neighbors.sort(by: fn(a, b) -> a.distance < b.distance)

# Closures capture surrounding scope
fn make_grower(rate):
    return fn():
        stem.grow_up(rate)    # 'rate' captured from outer scope

fast_grower = make_grower(5)
slow_grower = make_grower(1)
fast_grower()                 # grows 5 units
slow_grower()                 # grows 1 unit
```

### Generators
```
# Yield-based generators for lazy sequences
fn fibonacci():
    a, b = 0, 1
    loop:
        yield a
        a, b = b, a + b

# Usage
fib = fibonacci()
first_ten = fib.take(10)     # [0, 1, 1, 2, 3, 5, 8, 13, 21, 34]

# Practical example: scan soil in expanding circles
fn spiral_scan(center, max_radius):
    for r in 1..=max_radius:
        for angle in 0..360 by (360 // (r * 6)):
            x = center.x + r * cos(radians(angle))
            y = center.y + r * sin(radians(angle))
            yield env.soil.scan_at(<x, y>)

for result in spiral_scan(<0, 0>, 10):
    if result.type == "water":
        root.grow_toward(result.direction, result.distance)
        break
```

### Decorators
```
# Decorators modify function behavior
# Several are built into Growl for the genome system

@role("intake")
fn intake(org, env):
    root.absorb("water")

@gene("defense")
fn defense(org, env):
    defense.grow_thorns()

# Custom decorators
fn memoize(func):
    cache = {}
    return fn(*args):
        key = str(args)
        if key not in cache:
            cache[key] = func(...args)
        return cache[key]

@memoize
fn expensive_calculation(x, y):
    return complex_math(x, y)

# Energy tracking decorator (built-in)
@energy_cost(15)             # manually declare cost for analysis
fn heavy_operation():
    # ...complex logic...
    pass

# Conditional execution decorator (built-in)  
@only_when(fn() -> org.maturity > 0.5)
@gene("late_growth")
fn late_growth(org, env):
    # This gene only executes when maturity > 0.5
    # Energy cost is 0 when inactive
    reproduce.generate_seeds(4)

# Frequency decorator (built-in)
@every(10)                   # only runs every 10 ticks
@gene("periodic_scan")
fn periodic_scan(org, env):
    depot.set("scan_result", env.soil.scan("water"))

# Priority decorator (built-in) — controls gene execution order
@priority(1)                 # lower number = runs first
@role("intake")
fn intake(org, env):
    root.absorb("water")

@priority(2)
@role("energy")
fn energy(org, env):
    photo.absorb_light()
```

---

## 8. CLASSES & OBJECT-ORIENTED PROGRAMMING

### Basic Classes
```
class GrowthStrategy:
    ## A strategy for how this organism grows under different conditions.
    
    # Constructor
    fn new(rate: float, direction: vec2):
        self.rate = rate
        self.direction = direction
        self.active = true
        self.ticks_active = 0
    
    # Methods
    fn execute(soil_data):
        if not self.active: return
        root.grow_toward(self.direction, self.rate)
        self.ticks_active += 1
    
    fn pause():
        self.active = false
    
    fn resume():
        self.active = true
    
    # Computed property
    fn efficiency -> float:
        return self.rate / (self.ticks_active + 1)
    
    # String representation
    fn to_str() -> str:
        return "GrowthStrategy(rate=${self.rate}, dir=${self.direction})"

# Usage
strategy = GrowthStrategy(rate: 3.0, direction: <0, -1>)
strategy.execute(env.soil)
log(strategy.efficiency)
log(strategy)                # calls to_str() automatically
```

### Inheritance
```
class Organism:
    fn new(name: str):
        self.name = name
        self.age = 0
    
    fn tick():
        self.age += 1
    
    fn describe() -> str:
        return self.name

class FoodProducer extends Organism:
    fn new(name: str, crop_type: str):
        super.new(name)        # call parent constructor
        self.crop_type = crop_type
        self.yield_total = 0.0
    
    # Override parent method
    fn tick():
        super.tick()           # call parent tick
        self.grow()
    
    fn grow():
        # Subclass-specific behavior
        self.yield_total += 0.1
    
    fn describe() -> str:
        return "${super.describe()} producing ${self.crop_type}"

class GrainProducer extends FoodProducer:
    fn new(name: str):
        super.new(name, "grain")
        self.grain_size = 1.0
    
    fn grow():
        super.grow()
        self.grain_size += 0.05

# Usage
wheat = GrainProducer("wheat_alpha")
wheat.tick()
log(wheat.describe())        # "wheat_alpha producing grain"
log(wheat is Organism)       # true
log(wheat is FoodProducer)   # true
log(wheat is GrainProducer)  # true
```

### Interfaces (Traits)
```
# Interfaces define contracts that classes must fulfill.
# Called "traits" in Growl because organisms have traits.

trait Photosynthetic:
    ## Any organism that can convert light to energy.
    fn absorb_light() -> float
    fn optimal_wavelength() -> str

trait Defensive:
    ## Any organism that can defend itself.
    fn on_threat(threat) -> str     # returns defense action taken
    fn defense_cost() -> float

trait Reproductive:
    ## Any organism that can reproduce.
    fn produce_offspring(count: int) -> list
    fn viable() -> bool

# A class implements traits
class SolarPlant implements Photosynthetic, Defensive:
    fn new():
        self.chlorophyll = 1.0
        self.thorns = 0
    
    # Must implement all trait methods
    fn absorb_light() -> float:
        return env.light.intensity * self.chlorophyll
    
    fn optimal_wavelength() -> str:
        return "red"
    
    fn on_threat(threat) -> str:
        if self.thorns > 0:
            return "thorns"
        return "none"
    
    fn defense_cost() -> float:
        return self.thorns * 2.0

# Trait type checking
fn process_photosynthetic(p: Photosynthetic):
    energy = p.absorb_light()
    log("Absorbed ${energy} energy at ${p.optimal_wavelength()}")

plant = SolarPlant()
process_photosynthetic(plant)    # works — SolarPlant implements Photosynthetic

# Traits can have default implementations
trait Loggable:
    fn log_status():
        log("${self.to_str()} at tick ${TICK}")
    
    # This method must be implemented by the class
    fn to_str() -> str
```

### Abstract Classes
```
abstract class EnergySource:
    ## Base class for all energy production strategies.
    
    fn new(efficiency: float):
        self.efficiency = efficiency
        self.total_produced = 0.0
    
    # Abstract method — must be implemented by subclasses
    abstract fn produce(env) -> float
    
    # Concrete method — inherited as-is
    fn produce_and_track(env) -> float:
        amount = self.produce(env)
        self.total_produced += amount
        return amount

class Photosynthesis extends EnergySource:
    fn new(pigment: str = "chlorophyll_a"):
        super.new(efficiency: 0.7)
        self.pigment = pigment
    
    fn produce(env) -> float:
        return env.light.intensity * self.efficiency

class Chemosynthesis extends EnergySource:
    fn new(source: str):
        super.new(efficiency: 0.5)
        self.source = source
    
    fn produce(env) -> float:
        concentration = env.soil.scan(self.source)
        return |concentration| * self.efficiency     # magnitude of scan result

# Cannot instantiate abstract class directly
# source = EnergySource(0.5)    # ERROR: cannot instantiate abstract class
```

### Operator Overloading
```
class NutrientMix:
    fn new(nitrogen: float = 0, phosphorus: float = 0, potassium: float = 0):
        self.n = nitrogen
        self.p = phosphorus
        self.k = potassium
    
    # Addition — combine two mixes
    fn __add__(other: NutrientMix) -> NutrientMix:
        return NutrientMix(
            self.n + other.n,
            self.p + other.p,
            self.k + other.k
        )
    
    # Scalar multiplication
    fn __mul__(scalar: float) -> NutrientMix:
        return NutrientMix(self.n * scalar, self.p * scalar, self.k * scalar)
    
    # Comparison
    fn __eq__(other: NutrientMix) -> bool:
        return self.n == other.n and self.p == other.p and self.k == other.k
    
    fn __lt__(other: NutrientMix) -> bool:
        return self.total() < other.total()
    
    # String representation
    fn __str__() -> str:
        return "NPK(${self.n}, ${self.p}, ${self.k})"
    
    # Subscript access
    fn __getitem__(key: str) -> float:
        match key:
            case "n" | "nitrogen": return self.n
            case "p" | "phosphorus": return self.p
            case "k" | "potassium": return self.k
            case _: error("Unknown nutrient: ${key}")
    
    # Iteration
    fn __iter__():
        yield ("nitrogen", self.n)
        yield ("phosphorus", self.p)
        yield ("potassium", self.k)
    
    # Length
    fn __len__() -> int:
        return 3
    
    # Boolean truthiness
    fn __bool__() -> bool:
        return self.total() > 0
    
    # Helper
    fn total() -> float:
        return self.n + self.p + self.k

# Usage
mix_a = NutrientMix(nitrogen: 0.5, phosphorus: 0.3)
mix_b = NutrientMix(nitrogen: 0.2, potassium: 0.4)
combined = mix_a + mix_b
doubled = combined * 2
log(combined["nitrogen"])    # 0.7
for name, val in combined:
    log("${name}: ${val}")

# All overloadable operators:
# __add__, __sub__, __mul__, __div__, __mod__, __pow__
# __eq__, __ne__, __lt__, __gt__, __le__, __ge__
# __str__, __bool__, __len__, __iter__
# __getitem__, __setitem__, __contains__
# __call__ (make instances callable)
# __hash__ (for use as dict keys / set members)
```

### Static Members and Class Methods
```
class SoilAnalyzer:
    # Static constant
    static PH_NEUTRAL = 7.0
    
    # Static variable
    static scan_count = 0
    
    fn new(location: vec2):
        self.location = location
        SoilAnalyzer.scan_count += 1
    
    # Static method — no access to self
    static fn is_acidic(ph: float) -> bool:
        return ph < SoilAnalyzer.PH_NEUTRAL
    
    # Class method — receives the class itself
    class fn from_current_position() -> SoilAnalyzer:
        pos = org.morphology.center_of_mass
        return cls(location: <pos.x, pos.y>)

# Usage
analyzer = SoilAnalyzer.from_current_position()
log(SoilAnalyzer.is_acidic(5.5))     # true
log(SoilAnalyzer.scan_count)          # 1
```

### Enums
```
enum Season:
    SPRING
    SUMMER  
    AUTUMN
    WINTER

current = Season.SUMMER

match current:
    case Season.SPRING: grow_fast()
    case Season.SUMMER: produce_fruit()
    case Season.AUTUMN: store_energy()
    case Season.WINTER: go_dormant()

# Enums with values
enum SoilType(str):
    LOAM = "loam"
    CLAY = "clay"
    SAND = "sand"
    CALCIUMITE = "calciumite"
    SHALE = "shale"

# Enums with methods
enum GrowthPhase(float, float):
    SEEDLING = (0.0, 0.2)
    VEGETATIVE = (0.2, 0.6)
    REPRODUCTIVE = (0.6, 0.9)
    SENESCENCE = (0.9, 1.0)
    
    fn contains(maturity: float) -> bool:
        return self.value[0] <= maturity < self.value[1]
    
    static fn from_maturity(m: float) -> GrowthPhase:
        for phase in GrowthPhase:
            if phase.contains(m):
                return phase
        return GrowthPhase.SENESCENCE

phase = GrowthPhase.from_maturity(org.maturity)
```

### Structs (Lightweight Value Types)
```
# Structs are like classes but:
# - No inheritance
# - Passed by value (copied) not reference
# - Auto-generated constructor, equality, and to_str
# - Ideal for small data bundles

struct ScanResult:
    direction: vec2
    distance: float
    resource: str
    concentration: float = 0.0    # default value

# Auto-generated constructor
result = ScanResult(
    direction: <0.3, -0.7>,
    distance: 4.2,
    resource: "water"
)

# Auto-generated equality
result_a == result_b     # compares all fields

# Structs can have methods
struct NutrientProfile:
    carbs: float
    protein: float
    fat: float
    vitamins: float
    
    fn total_calories() -> float:
        return self.carbs * 4 + self.protein * 4 + self.fat * 9
    
    fn is_balanced() -> bool:
        vals = [self.carbs, self.protein, self.fat, self.vitamins]
        return vals.min() > 0.1   # nothing critically low

# Struct spread/copy
modified = ScanResult(...result, distance: 5.0)
# copies all fields from result but overrides distance
```

### Mixins (Multiple Behavior Composition)
```
# Mixins are reusable bundles of behavior that can be mixed into classes.
# Unlike traits, mixins can carry state and full implementations.
# Unlike inheritance, you can mix in multiple.

mixin Trackable:
    fn init_tracking():
        self._track_history = []
    
    fn track(event: str, data: any = none):
        self._track_history.push({
            "tick": TICK,
            "event": event,
            "data": data
        })
    
    fn history(last: int = 10) -> list:
        return self._track_history[-last..]

mixin Cacheable:
    fn init_cache():
        self._cache = {}
        self._cache_ttl = {}
    
    fn cache_set(key: str, value: any, ttl: int = 50):
        self._cache[key] = value
        self._cache_ttl[key] = TICK + ttl
    
    fn cache_get(key: str) -> any:
        if key in self._cache and TICK < self._cache_ttl[key]:
            return self._cache[key]
        return none

class SmartRoot with Trackable, Cacheable:
    fn new():
        self.init_tracking()
        self.init_cache()
        self.depth = 0
    
    fn grow(direction: vec2, distance: float):
        # Check cache for recent scans
        cached = self.cache_get("soil_scan")
        if cached is none:
            cached = env.soil.scan("water")
            self.cache_set("soil_scan", cached, ttl: 20)
        
        root.grow_toward(cached, distance)
        self.depth += distance
        self.track("grew", {"direction": direction, "distance": distance})
```

---

## 9. BIOLOGICAL LANGUAGE CONSTRUCTS

These are Growl-specific constructs that don't exist in general-purpose languages. They embody the biological programming metaphor.

### Gene Declarations
```
# Roles (required — the four core slots)
@role("intake")
fn intake(org, env):
    root.absorb("water")

@role("structure")
fn structure(org, env):
    stem.grow_up(3)

@role("energy")
fn energy(org, env):
    photo.absorb_light()

@role("output")
fn output(org, env):
    product = synthesize(base: "carbohydrate")
    produce(product)

# Genes (optional — fill remaining genome slots)
@gene("drought_response")
fn drought_handler(org, env):
    if org.water < 0.2:
        leaf.close_stomata()

# Genes can be any name — the name becomes the slot label
@gene("my_custom_behavior")
fn whatever_i_want(org, env):
    pass
```

### Phase Blocks
```
# Execute code only during a lifecycle phase.
# The two floats are maturity range [start, end).

phase "seedling" (0.0, 0.2):
    root.grow_down(4)
    root.grow_wide(2)
    stem.grow_up(1)

phase "vegetative" (0.2, 0.6):
    stem.grow_up(3)
    stem.branch(2)
    leaf.grow(8)

phase "reproductive" (0.6, 0.9):
    output_product()
    reproduce.generate_seeds(4)

phase "senescence" (0.9, 1.0):
    leaf.shed()
    reproduce.generate_seeds(10, energy_per_seed: 2)

# Custom phases (not tied to maturity — any condition)
phase "drought" when org.water < 0.15:
    leaf.close_stomata()
    root.grow_toward("water", 5)
    stem.store_water(org.water * 0.5)

phase "under_attack" when env.threats.pests.length > 0:
    defense.produce_toxin("alkaloid", potency: 0.7)
    org.signals.emit("distress", intensity: 0.9, radius: 5)

# Phases are mutually exclusive within a gene — only one runs per tick.
# Priority: conditional phases > lifecycle phases
# If "drought" is active, the lifecycle phase is paused.
```

### When Blocks (Edge-Triggered)
```
# 'when' runs ONCE when a condition BECOMES true.
# It does NOT run again until the condition becomes false and true again.
# This is edge-detection, not level-detection.

when org.water < 0.2:
    warn("Water critical!")
    root.grow_toward("water", 5)
    org.signals.emit("drought_stress", intensity: 0.9)

when env.threats.pests.length > 0:
    defense.produce_toxin("alkaloid", potency: 0.5)
    log("Pest detected — toxins deployed")

when org.maturity > 0.5:
    name_self("Mature ${org.name}")

# 'when' with 'then' for paired triggers (edge up / edge down)
when org.water < 0.2:
    leaf.close_stomata()
    warn("Drought mode activated")
then when org.water >= 0.5:
    leaf.open_stomata(0.8)
    log("Drought mode deactivated")
```

### Respond Blocks (Event-Reactive)
```
# 'respond to' registers an event handler within a gene.
# Events come from the organism itself, neighbors, or the Depot.

respond to "damage" as dmg:
    match dmg.source:
        case "pest":
            defense.produce_toxin("capsaicin", potency: dmg.severity)
        case "wind":
            stem.grow_thick(1)
            root.anchor(dmg.severity)
        case "frost":
            defense.fever(2)
        case "harvest":
            photo.set_metabolism(1.5)

respond to "neighbor_signal" as signal:
    if signal.type == "distress" and signal.distance < 3:
        defense.produce_repellent("volatile_oil", radius: 3)
    elif signal.type == "pollen" and org.maturity > 0.6:
        reproduce.crossbreed(signal.sender)

respond to "depot:power.fluctuation" as data:
    if data.severity > 0.5:
        photo.store_energy(org.energy * 0.3)
        leaf.close_stomata()

respond to "depot:irrigation.activated":
    org.memory["last_irrigated"] = org.age
```

### Adapt Blocks (Continuous Tuning)
```
# 'adapt' continuously adjusts a value based on a condition.
# Smoother than if/else — creates gradual biological responses.

adapt leaf.stomata_openness:
    toward 1.0 when env.light.intensity > 0.5 and org.water > 0.4
    toward 0.3 when org.water < 0.3
    toward 0.0 when env.light.intensity < 0.1
    rate 0.1  # change by 0.1 per tick maximum (smooth transition)

adapt stem.rigidity:
    toward 1.0 when env.air.wind_speed > 10
    toward 0.5 when env.air.wind_speed > 5
    toward 0.3 otherwise
    rate 0.05

# 'adapt' prevents jarring jumps in behavior.
# The organism smoothly transitions between states
# rather than snapping instantly.
# This looks better visually and is more biologically realistic.
```

### Cycle Blocks (Periodic Behavior)
```
# 'cycle' defines behavior that repeats on a rhythm.
# Biological organisms are full of rhythmic processes.

cycle "circadian" period 24h:
    at 0%:     # dawn
        leaf.open_stomata(0.3)
        photo.set_metabolism(0.8)
    at 25%:    # midday  
        leaf.open_stomata(1.0)
        photo.set_metabolism(1.2)
    at 50%:    # afternoon
        leaf.open_stomata(0.7)
    at 75%:    # night
        leaf.close_stomata()
        photo.set_metabolism(0.3)
        root.absorb("water")    # roots work at night

cycle "growth_pulse" period 5 ticks:
    at 0%:
        stem.grow_up(0.5)
    at 50%:
        root.grow_down(0.3)
    # alternating stem/root growth mimics real plant behavior

# Cycles can be started, stopped, and shifted
cycle "seasonal" period 100 ticks:
    at 0%:   phase_spring()
    at 25%:  phase_summer()
    at 50%:  phase_autumn()
    at 75%:  phase_winter()

# Cycle control (within gene code)
cycles.pause("circadian")
cycles.resume("circadian")
cycles.shift("circadian", offset: 25%)   # jet lag
cycles.reset("circadian")
```

---

## 10. TYPE SYSTEM DETAILS

### Custom Type Aliases
```
type Energy = float
type ResourceName = str
type Direction = vec2
type Callback = fn(any) -> none
type NutrientMap = dict[str, float]
type ScanResults = list[ScanResult]
type MaybeFloat = float | none
```

### Generic Types
```
class Buffer[T]:
    fn new(capacity: int):
        self.items: list[T] = []
        self.capacity = capacity
    
    fn push(item: T):
        if self.items.length >= self.capacity:
            self.items.pop(0)     # remove oldest
        self.items.push(item)
    
    fn peek() -> T | none:
        if self.items.length > 0:
            return self.items[-1]
        return none
    
    fn average() -> T where T: numeric:
        return self.items.sum() / self.items.length

# Usage
water_history = Buffer[float](capacity: 100)
water_history.push(org.water)
avg = water_history.average()

scan_buffer = Buffer[ScanResult](capacity: 20)
```

### Constraint Types
```
# Constrained types add runtime validation
type Percentage = float where 0.0 <= self <= 1.0
type PositiveInt = int where self > 0
type CropName = str where self.length > 0 and self.length <= 32
type ValidPH = float where 0.0 <= self <= 14.0

fn set_stomata(openness: Percentage):
    leaf.open_stomata(openness)

set_stomata(0.5)    # fine
set_stomata(1.5)    # runtime error (but organism mutates rather than crashes)
                    # the stomata opens to 1.0 instead and the organism takes stress
```

### Union Types & Narrowing
```
type ScanHit = ScanResult | none

fn find_water() -> ScanHit:
    result = env.soil.scan("water")
    if |result| > 0:
        return ScanResult(direction: result, distance: |result|, resource: "water")
    return none

hit = find_water()

# Type narrowing through conditionals
if hit is ScanResult:
    root.grow_toward(hit.direction, hit.distance)
    # compiler knows 'hit' is ScanResult here, not none
elif hit is none:
    warn("No water found")
```

---

## 11. MODULES & IMPORTS

### Module Definition
```
# File: strategies/drought.gwl

## Drought survival strategies for arid environments.

module drought

const CRITICAL_WATER = 0.15
const LOW_WATER = 0.3

class DroughtStrategy:
    fn new(severity: float = 0.5):
        self.severity = severity
    
    fn apply(org, env):
        if org.water < CRITICAL_WATER:
            self.emergency(org)
        elif org.water < LOW_WATER:
            self.conserve(org)
    
    fn emergency(org):
        leaf.close_stomata()
        leaf.shed()
        root.grow_toward("water", 8)
    
    fn conserve(org):
        leaf.open_stomata(0.2)
        stem.store_water(org.water * 0.3)

fn quick_drought_check(org) -> bool:
    return org.water < LOW_WATER
```

### Importing
```
# Import entire module
import drought
strategy = drought.DroughtStrategy(severity: 0.8)
strategy.apply(org, env)

# Import specific items
from drought import DroughtStrategy, CRITICAL_WATER
strategy = DroughtStrategy()

# Import with alias
import drought as dry
from drought import DroughtStrategy as DS

# Import all (not recommended but available)
from drought import *

# Relative imports (within the same organism's program)
from .helpers import scan_pattern
from ..shared import common_strategies
```

### Standard Library Modules
```
# These modules are always available — no import needed.
# They represent the biological API.

root          # subterranean growth
stem          # structural growth
leaf          # surface organs
photo         # energy systems
morph         # dynamic morphology
defense       # protection systems
reproduce     # reproduction and propagation
depot         # cross-language communication

# These require import:
import math               # extended math functions
import patterns            # common growth patterns library
import analysis            # soil/environment analysis tools
import genetics            # genome manipulation utilities
import schedule            # advanced timing/scheduling

# Player-created modules can be saved to their library
# and imported into future organisms
import my_library.smart_roots
import my_library.efficient_photosynthesis
```

### Module: `math` (Extended)
```
import math

math.sin(angle)
math.cos(angle)
math.tan(angle)
math.asin(value)
math.acos(value)
math.atan2(y, x)
math.radians(degrees)
math.degrees(radians)
math.sqrt(value)
math.abs(value)          # also available as built-in |value|
math.floor(value)
math.ceil(value)
math.round(value, places: 2)
math.log(value, base: math.E)
math.log2(value)
math.log10(value)
math.pow(base, exp)      # also available as base ** exp
math.PI
math.E
math.TAU                 # 2π
math.INF                 # positive infinity
math.sigmoid(x)          # logistic curve, useful for smooth transitions
math.smoothstep(edge0, edge1, x)   # smooth interpolation
math.map_range(value, in_min, in_max, out_min, out_max)  # remap
```

### Module: `patterns` (Growth Pattern Library)
```
import patterns

# Pre-built growth patterns players can use or learn from

patterns.spiral(turns: 3, radius: 5)
    # Returns a list of (direction, distance) pairs for spiral growth.
    # Useful for roots seeking resources in a spiral scan.

patterns.fibonacci_branch(levels: 5)
    # Returns branching angles based on golden ratio.
    # Produces naturally beautiful tree structures.

patterns.fractal_root(depth: 4, scale: 0.7)
    # Returns a recursive branching pattern.
    # Each level is 70% the size of the previous.

patterns.hexgrid(radius: 3, spacing: 2.0)
    # Returns positions for hexagonal packing.
    # Optimal for covering area (like leaf arrangement).

patterns.phyllotaxis(count: 20, divergence: 137.5)
    # Returns positions following phyllotactic pattern.
    # The golden angle arrangement seen in sunflower heads.
```

---

## 12. CONCURRENCY & TIMING

### Wait & Defer
```
# Wait suspends the current gene for N ticks
wait(5)                  # pause 5 ticks, then continue
wait(1)                  # common: skip a tick

# Defer runs code at a later time without blocking
defer 10 ticks:
    log("This runs 10 ticks from now")
    check_growth()

defer until org.maturity > 0.5:
    start_fruiting()

# Cancel a deferred action
handle = defer 100 ticks:
    reproduce.generate_seeds(10)

if crisis_detected:
    handle.cancel()
```

### Async Gene Execution
```
# By default, genes run sequentially each tick in priority order.
# Mark a gene as async to run it in parallel with other genes.
# Useful for independent monitoring tasks.

@gene("monitor")
@async                    # runs in parallel, doesn't block other genes
fn environment_monitor(org, env):
    loop:
        snapshot = {
            "water": org.water,
            "energy": org.energy,
            "threats": env.threats.pests.length,
            "light": env.light.intensity
        }
        org.memory["env_snapshot"] = snapshot
        depot.set("${org.name}.status", snapshot)
        wait(5)

# Other genes can read the snapshot from memory without
# needing to do their own scanning
```

### Tickers
```
# Lightweight periodic tasks, cheaper than full genes.
# No org/env access — just run a callback on schedule.

ticker "heartbeat" every 10 ticks:
    depot.emit("${org.name}.alive", {tick: TICK})

ticker "water_check" every 3 ticks:
    if org.water < 0.2:
        org.signals.emit("thirsty", intensity: 1.0 - org.water)

# Tickers cost less energy than full genes.
# Energy cost: 0.5 per ticker per activation.
```

---

## 13. ERROR & MUTATION SYSTEM

### How Errors Work in Growl

Growl does not crash. Ever. Instead, errors cause **mutations** — unintended biological side effects. This is a core design principle: bad code makes weird organisms, not dead terminals.

```
ERROR → MUTATION MAPPING
━━━━━━━━━━━━━━━━━━━━━━━
TypeError           → Random property drift (color changes, size wobble)
IndexOutOfBounds    → Stunted growth in a random body part
DivisionByZero      → Energy spike then crash (organism flares brightly then wilts)
NoneAccess          → Phantom growth (grows a vestigial part that does nothing)
InfiniteLoop        → Cancerous growth (one part grows uncontrollably for 10 ticks)
StackOverflow       → Organism splits in two (emergency cell division)
EnergyOverBudget    → Part shedding (drops expensive parts to survive)
TypeMismatch        → Chemical imbalance (wrong nutrients, weird coloring)
KeyNotFound         → Sensory confusion (organism reacts to phantom stimuli)
```

### The Mutation Log
```
# Players can inspect mutations in the terminal

inspect_mutations()
# Returns:
# MUTATION LOG — organism "wheat_alpha_01"
# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
# Tick 142:  TypeError in gene "drought_response" line 7
#            → Color drift: stem shifted from green to yellow-green
#            → Cause: compared string to float (soil.type < 0.3)
#
# Tick 289:  IndexOutOfBounds in gene "companion" line 12  
#            → Stunted leaf growth: leaf_03 capped at 60% normal size
#            → Cause: accessed neighbors[3] but only 2 neighbors exist
```

### Explicit Mutation (Intentional)
```
# Players can INTENTIONALLY introduce mutations for creative effects

mutate org.morphology.color by noise(TICK * 0.1) * 0.2
    # Color slowly shifts over time using perlin noise
    # Creates shimmering/iridescent effect

mutate stem.rigidity by random(-0.1, 0.1) every 5 ticks
    # Stem rigidity wobbles slightly
    # Makes the organism sway naturally

# Mutation propagation — mutations can be inherited
if reproduce.mutate_enabled:
    # Children inherit accumulated mutations
    # Over generations, organisms drift and adapt
    pass
```

---

## 14. COMPLETE ORGANISM EXAMPLE

Putting it all together — a full, complex organism using every language feature.

```
###
ORGANISM: Skythread
A floating air-purifying organism that anchors to ceilings,
produces bioluminescent light, and filters toxins.
Designed for facility corridor illumination.
Canister: Tier 2 (20 slots, 200 energy)
###

module skythread
import math
import patterns

# ─── CUSTOM TYPES ───

struct LightConfig:
    base_color: color
    max_brightness: float
    pulse_rate: float = 0.0

    fn with_pulse(rate: float) -> LightConfig:
        return LightConfig(...self, pulse_rate: rate)

enum Mode:
    IDLE
    ACTIVE
    ALERT
    DORMANT

trait Glowing:
    fn glow(brightness: float, color: color)
    fn dim()

trait AirProcessing:
    fn filter_cycle() -> float    # returns volume processed

# ─── MAIN STATE CLASS ───

class SkythreadState implements Glowing, AirProcessing:
    fn new():
        self.mode = Mode.IDLE
        self.brightness = 0.3
        self.light = LightConfig(
            base_color: #B4FFD0,
            max_brightness: 0.8
        )
        self.toxins_filtered = 0.0
        self.energy_buffer = Buffer[float](capacity: 50)

    fn glow(brightness: float, col: color):
        self.brightness = clamp(brightness, 0.05, self.light.max_brightness)
        morph.emit_light(intensity: self.brightness, color: col)

    fn dim():
        self.brightness = 0.05
        morph.emit_light(intensity: 0.05, color: self.light.base_color)

    fn filter_cycle() -> float:
        volume = 0.0
        for toxin in env.air.toxins:
            leaf.absorb_chemical(toxin.name)
            volume += toxin.concentration
        self.toxins_filtered += volume
        return volume

    fn update_mode():
        nearby_count = env.neighbors.count(radius: 5)
        co2_spike = env.air.co2 > 800

        match (nearby_count > 0, co2_spike, org.energy < 10):
            case (_, _, true):      self.mode = Mode.DORMANT
            case (_, true, _):      self.mode = Mode.ALERT
            case (true, _, _):      self.mode = Mode.ACTIVE
            case _:                 self.mode = Mode.IDLE

# ─── INSTANTIATE STATE ───

const STATE = SkythreadState()

name_self("Skythread")
classify_self("utility_bioluminescent_filter")

# ─── ROLE: INTAKE ───

@role("intake")
@priority(1)
fn intake(org, env):
    ## Absorbs moisture and CO2 from air. No soil contact.
    leaf.absorb_moisture()
    leaf.absorb_chemical("co2")

    adapt leaf.absorption_rate:
        toward 1.0 when env.air.humidity > 0.7
        toward 0.5 when env.air.humidity > 0.3
        toward 0.2 otherwise
        rate 0.05

# ─── ROLE: STRUCTURE ───

@role("structure")
@priority(2)
fn structure(org, env):
    ## Hanging vine structure that spreads along ceiling.
    phase "establishing" (0.0, 0.2):
        stem.set_material("fibrous")
        stem.set_rigidity(0.15)
        stem.attach_to("ceiling")
        stem.grow_horizontal(2)

        # Grow dangling tendrils using fibonacci spacing
        for angle in patterns.phyllotaxis(count: 5):
            stem.grow_segment(
                length: random(1.0, 3.0),
                angle: -90 + random(-15, 15),   # mostly downward, slight variation
            )

    phase "spreading" (0.2, 0.7):
        if org.morphology.width < 50:
            stem.branch(1)
            stem.grow_horizontal(1)

        # Occasionally grow a new tendril
        if chance(0.1):
            stem.grow_segment(length: random(2, 5), angle: -90)

    phase "mature" (0.7, 1.0):
        stem.heal("*", rate: 0.5)   # self-maintenance

    stem.set_texture("hairy")        # insulating fuzz
    morph.set_surface("*", {
        "bioluminescence": STATE.brightness * 0.3,
        "biolum_color": STATE.light.base_color
    })

# ─── ROLE: ENERGY ───

@role("energy")
@priority(3)
fn energy_production(org, env):
    ## Radiosynthesis primary, photosynthesis backup.
    income = photo.radiosynthesis()

    if income < 3:
        photo.set_pigment("phycocyanin")
        income += photo.absorb_light()

    STATE.energy_buffer.push(income)

    # Store excess during high-production periods
    avg = STATE.energy_buffer.average() ?? 0
    if income > avg * 1.2:
        photo.store_energy(income * 0.2, location: "stem")

# ─── ROLE: OUTPUT ───

@role("output")
@priority(4)
fn output(org, env):
    ## Bioluminescent light + oxygen emission.

    STATE.update_mode()

    match STATE.mode:
        case Mode.DORMANT:
            STATE.dim()
        case Mode.IDLE:
            STATE.glow(0.3, STATE.light.base_color)
        case Mode.ACTIVE:
            STATE.glow(0.7, #FFFAF0)        # warm white when people nearby
        case Mode.ALERT:
            STATE.glow(0.5, #FFB4B4)        # pinkish when air quality is bad

    # Oxygen output
    o2 = synthesize(base: "chemical", type: "nutrient_rich", potency: 0.8)
    emit(o2, rate: org.energy_income * 0.1)

# ─── GENE: AIR FILTRATION ───

@gene("air_filter")
@priority(5)
fn air_filtration(org, env):
    ## Absorb and neutralize airborne toxins.
    volume = STATE.filter_cycle()

    if volume > 0:
        depot.emit("air.filtered", {
            "source": org.name,
            "volume": volume,
            "remaining_toxins": env.air.toxins.length
        })

    when env.air.toxins.length > 5:
        warn("Heavy toxin load — increasing filtration")
        leaf.grow(area: 4)           # grow more leaf surface for more filtering
        STATE.light = STATE.light.with_pulse(rate: 2.0)
        # pulsing light = visual warning to player that air quality is bad

# ─── GENE: PRESENCE DETECTION ───

@gene("presence_response")
fn presence(org, env):
    ## Brighten when living things are nearby.
    nearby = env.neighbors.count(radius: 5)
    co2_level = env.air.co2

    # CO2 spike means something is breathing nearby
    when co2_level > 600:
        org.memory["bright_until"] = TICK + 30

    if TICK < org.memory.get("bright_until", 0):
        STATE.glow(0.9, #FFFAF0)     # bright warm white

# ─── GENE: SELF-PROPAGATION ───

@gene("spread")
@only_when(fn() -> org.maturity > 0.6)
fn self_propagation(org, env):
    ## Slowly clone along ceiling.
    cycle "clone_cycle" period 100 ticks:
        at 0%:
            if org.energy > 30:
                reproduce.clone(direction: "horizontal")
                depot.emit("skythread.cloned", {
                    "parent": org.name,
                    "location": org.morphology.center_of_mass
                })

# ─── GENE: DEPOT REPORTING ───

@gene("telemetry")
@every(10)
fn depot_report(org, env):
    ## Report status to the shared Depot for Signal/Struct coordination.
    id = "skythread_${SELF.id}"
    depot.set("${id}.brightness", STATE.brightness)
    depot.set("${id}.mode", STATE.mode.name)
    depot.set("${id}.air_quality", 1.0 - env.air.toxins.length * 0.1)
    depot.set("${id}.energy", org.energy)
    depot.set("${id}.toxins_filtered_total", STATE.toxins_filtered)

    respond to "depot:lighting.override" as cmd:
        match cmd.action:
            case "bright": STATE.glow(0.9, cmd.color ?? #FFFFFF)
            case "dim":    STATE.dim()
            case "pulse":  STATE.light = STATE.light.with_pulse(rate: cmd.rate ?? 1.0)
            case "reset":  STATE.mode = Mode.IDLE
```

---

## 15. GRAMMAR (FORMAL — EBNF-STYLE)

This section defines the grammar precisely enough to build a parser.

```ebnf
(* ── Top-level ── *)
program          = { statement NEWLINE } ;
statement        = declaration | expression_stmt | assignment | control_flow
                 | gene_decl | phase_block | when_block | respond_block
                 | adapt_block | cycle_block | ticker_decl
                 | import_stmt | module_decl ;

(* ── Declarations ── *)
declaration      = const_decl | class_decl | struct_decl | enum_decl
                 | trait_decl | mixin_decl | type_alias | fn_decl ;

const_decl       = "const" IDENTIFIER [ ":" type ] "=" expression ;
type_alias       = "type" IDENTIFIER [ generics ] "=" type [ where_clause ] ;

fn_decl          = { decorator } "fn" IDENTIFIER [ generics ] "(" [ param_list ] ")" 
                   [ "->" type ] ":" NEWLINE INDENT block DEDENT ;
param_list       = param { "," param } ;
param            = [ "*" | "**" ] IDENTIFIER [ ":" type ] [ "=" expression ] ;

class_decl       = { decorator } [ "abstract" ] "class" IDENTIFIER [ generics ]
                   [ "extends" type ] [ "implements" type_list ]
                   [ "with" type_list ] ":" NEWLINE INDENT class_body DEDENT ;
class_body       = { class_member NEWLINE } ;
class_member     = fn_decl | static_decl | class_method | field_decl 
                 | abstract_method ;
static_decl      = "static" ( fn_decl | field_decl ) ;
class_method     = "class" fn_decl ;
abstract_method  = "abstract" "fn" IDENTIFIER "(" [ param_list ] ")" [ "->" type ] ;
field_decl       = IDENTIFIER ":" type [ "=" expression ] ;

struct_decl      = "struct" IDENTIFIER [ generics ] ":"
                   NEWLINE INDENT { field_decl NEWLINE } { fn_decl NEWLINE } DEDENT ;

enum_decl        = "enum" IDENTIFIER [ "(" type_list ")" ] ":"
                   NEWLINE INDENT { enum_member NEWLINE } { fn_decl NEWLINE } DEDENT ;
enum_member      = IDENTIFIER [ "=" expression ] ;

trait_decl       = "trait" IDENTIFIER [ generics ] ":"
                   NEWLINE INDENT { trait_member NEWLINE } DEDENT ;
trait_member      = fn_decl | abstract_method ;

mixin_decl       = "mixin" IDENTIFIER ":"
                   NEWLINE INDENT { fn_decl NEWLINE } DEDENT ;

(* ── Gene / Role Declarations ── *)
gene_decl        = { decorator } ( "@role" | "@gene" ) "(" STRING ")" NEWLINE
                   fn_decl ;
decorator        = "@" IDENTIFIER [ "(" [ arg_list ] ")" ] NEWLINE ;

(* ── Import / Module ── *)
module_decl      = "module" IDENTIFIER ;
import_stmt      = "import" dotted_name [ "as" IDENTIFIER ]
                 | "from" dotted_name "import" import_names ;
import_names     = "*" | IDENTIFIER { "," IDENTIFIER } ;
dotted_name      = IDENTIFIER { "." IDENTIFIER } ;

(* ── Control Flow ── *)
control_flow     = if_stmt | for_stmt | while_stmt | loop_stmt
                 | match_stmt | try_stmt ;

if_stmt          = "if" expression ":" NEWLINE INDENT block DEDENT
                   { "elif" expression ":" NEWLINE INDENT block DEDENT }
                   [ "else" ":" NEWLINE INDENT block DEDENT ] ;

for_stmt         = "for" target_list "in" expression ":" NEWLINE INDENT block DEDENT
                   [ "else" ":" NEWLINE INDENT block DEDENT ] ;
target_list      = IDENTIFIER { "," IDENTIFIER } ;

while_stmt       = "while" expression ":" NEWLINE INDENT block DEDENT ;
loop_stmt        = "loop" ":" NEWLINE INDENT block DEDENT ;

match_stmt       = "match" expression ":" NEWLINE INDENT { case_clause } DEDENT ;
case_clause      = "case" pattern [ "if" expression ] ":" NEWLINE INDENT block DEDENT ;
pattern          = "_" | literal | IDENTIFIER | tuple_pattern | dict_pattern
                 | type_pattern | or_pattern ;
tuple_pattern    = "(" pattern { "," pattern } ")" ;
dict_pattern     = "{" IDENTIFIER ":" pattern { "," IDENTIFIER ":" pattern } "}" ;
type_pattern     = IDENTIFIER "is" type ;
or_pattern       = pattern "|" pattern ;

try_stmt         = "try" ":" NEWLINE INDENT block DEDENT
                   "recover" IDENTIFIER ":" NEWLINE INDENT block DEDENT
                   [ "always" ":" NEWLINE INDENT block DEDENT ] ;

(* ── Biological Constructs ── *)
phase_block      = "phase" STRING "(" expression "," expression ")" ":"
                   NEWLINE INDENT block DEDENT
                 | "phase" STRING "when" expression ":"
                   NEWLINE INDENT block DEDENT ;

when_block       = "when" expression ":" NEWLINE INDENT block DEDENT
                   [ "then" when_block ] ;

respond_block    = "respond" "to" STRING [ "as" IDENTIFIER ] ":"
                   NEWLINE INDENT block DEDENT ;

adapt_block      = "adapt" dotted_name ":" NEWLINE INDENT
                   { adapt_rule NEWLINE } "rate" expression NEWLINE DEDENT ;
adapt_rule       = "toward" expression ( "when" expression | "otherwise" ) ;

cycle_block      = "cycle" STRING "period" expression ":"
                   NEWLINE INDENT { cycle_point NEWLINE } DEDENT ;
cycle_point      = "at" expression ":" NEWLINE INDENT block DEDENT ;

ticker_decl      = "ticker" STRING "every" expression ":"
                   NEWLINE INDENT block DEDENT ;

(* ── Expressions ── *)
expression       = ternary ;
ternary          = or_expr [ "if" expression "else" expression ] ;
or_expr          = and_expr { "or" and_expr } ;
and_expr         = not_expr { "and" not_expr } ;
not_expr         = "not" not_expr | comparison ;
comparison       = pipe_expr { comp_op pipe_expr } ;
comp_op          = "==" | "!=" | "<" | ">" | "<=" | ">=" 
                 | "is" [ "not" ] | [ "not" ] "in" ;
pipe_expr        = nullish_expr { "|>" call_expr } ;
nullish_expr     = bitwise_expr { "??" bitwise_expr } ;
bitwise_expr     = arith_expr { ( "&" | "|" | "^" | "<<" | ">>" ) arith_expr } ;
arith_expr       = term { ( "+" | "-" ) term } ;
term             = factor { ( "*" | "/" | "//" | "%" ) factor } ;
factor           = ( "+" | "-" | "~" | "^" ) factor | power ;
power            = unary [ "**" factor ] ;
unary            = "|" expression "|"          (* magnitude *)
                 | "^" expression              (* normalize *)
                 | call_expr ;
call_expr        = primary { trailer } ;
trailer          = "(" [ arg_list ] ")"
                 | "[" subscript "]"
                 | "." IDENTIFIER ;
arg_list         = arg { "," arg } ;
arg              = [ IDENTIFIER ":" ] expression
                 | "..." expression ;
subscript        = expression [ ".." [ "=" ] expression [ "by" expression ] ] ;

primary          = literal | IDENTIFIER | grouped | list_expr | dict_expr
                 | set_expr | tuple_expr | lambda_expr | vector_literal ;

(* ── Literals ── *)
literal          = INTEGER | FLOAT | STRING | "true" | "false" | "none"
                 | color_literal | range_literal | unit_literal ;
color_literal    = "#" HEX_DIGITS ;
unit_literal     = ( INTEGER | FLOAT ) UNIT_SUFFIX ;
vector_literal   = "<" expression "," expression [ "," expression ] ">" ;
range_literal    = expression ".." [ "=" ] expression [ "by" expression ] ;

grouped          = "(" expression ")" ;
list_expr        = "[" [ expression { "," expression } ] "]"
                 | "[" expression "for" target_list "in" expression 
                       { "for" target_list "in" expression }
                       [ "if" expression ] "]" ;
dict_expr        = "{" [ dict_entry { "," dict_entry } ] "}" 
                 | "{" expression ":" expression "for" target_list "in" expression
                       [ "if" expression ] "}" ;
dict_entry       = expression ":" expression | IDENTIFIER | "..." expression ;
set_expr         = "{" expression { "," expression } "}" ;     (* 2+ items *)
tuple_expr       = "(" expression "," [ expression { "," expression } ] ")" ;
lambda_expr      = "fn" "(" [ param_list ] ")" ( "->" expression | ":" NEWLINE INDENT block DEDENT ) ;

(* ── Types ── *)
type             = type_union ;
type_union       = type_primary { "|" type_primary } ;
type_primary     = IDENTIFIER [ generics_usage ] | fn_type | tuple_type ;
fn_type          = "fn" "(" [ type_list ] ")" "->" type ;
tuple_type       = "(" type { "," type } ")" ;
generics         = "[" IDENTIFIER { "," IDENTIFIER } "]" ;
generics_usage   = "[" type { "," type } "]" ;
type_list        = type { "," type } ;
where_clause     = "where" expression ;

(* ── Assignment ── *)
assignment       = target "=" expression
                 | target augmented_op expression ;
augmented_op     = "+=" | "-=" | "*=" | "/=" | "//=" | "%=" | "**=" ;
target           = IDENTIFIER | call_expr "." IDENTIFIER | call_expr "[" subscript "]" ;

expression_stmt  = expression ;
block            = { statement NEWLINE } ;

(* ── Tokens ── *)
IDENTIFIER       = LETTER { LETTER | DIGIT | "_" } ;
INTEGER          = DIGIT { DIGIT | "_" } | "0x" HEX_DIGIT { HEX_DIGIT }
                 | "0b" BIN_DIGIT { BIN_DIGIT } ;
FLOAT            = DIGIT { DIGIT } "." DIGIT { DIGIT } [ "e" [ "+" | "-" ] DIGIT { DIGIT } ] ;
STRING           = '"' { CHAR | ESCAPE | "${" expression "}" } '"'
                 | "'" { CHAR | ESCAPE } "'"
                 | '"""' { ANY } '"""'
                 | "r\"" { CHAR } '"' ;
UNIT_SUFFIX      = "cm" | "m" | "g" | "kg" | "s" | "C" | "kW" | "%" | "h" ;
NEWLINE          = "\n" ;
INDENT           = (* increase in indentation level *) ;
DEDENT           = (* decrease in indentation level *) ;
```

---

## 16. RESERVED WORDS

```
# Keywords
if elif else for in while loop break continue return
fn class struct enum trait mixin abstract static
const type import from as module
match case
try recover always
and or not is
true false none
yield
self super cls

# Biological keywords
phase when then
respond to
adapt toward rate otherwise
cycle at period
ticker every
wait defer
mutate by

# Decorators (built-in)
@role @gene @priority @every @only_when @async @energy_cost @memoize

# Built-in module names (reserved as bare identifiers)
root stem leaf photo morph defense reproduce depot org env

# Soft keywords (only reserved in specific contexts)
# These can be used as variable names elsewhere:
where with implements extends
```

---

## 17. OPERATOR PRECEDENCE (highest to lowest)

```
1.  ()  []  .  fn()           Grouping, subscript, attribute, call
2.  **                        Exponentiation (right-associative)
3.  +x  -x  ~x  |x|  ^x     Unary plus/minus/bitwise-not/magnitude/normalize
4.  *  /  //  %              Multiplication, division, modulo
5.  +  -                     Addition, subtraction
6.  <<  >>                   Bitwise shifts
7.  &                        Bitwise AND / set intersection
8.  ^                        Bitwise XOR / set symmetric diff
9.  |                        Bitwise OR / set union
10. ??                       Nullish coalescing
11. |>                       Pipeline
12. ==  !=  <  >  <=  >=     Comparison
    is  is not  in  not in   Identity, membership
13. not                      Logical NOT
14. and                      Logical AND
15. or                       Logical OR
16. x if cond else y         Ternary conditional
17. =  +=  -=  *=  /=  etc.  Assignment (statement-level, not expression)
```

---

## 18. IMPLEMENTATION NOTES

### Compilation Pipeline

```
Source (.gwl file)
    │
    ▼
┌──────────┐
│  LEXER   │  → Token stream
│          │    Handles indentation → INDENT/DEDENT tokens
│          │    String interpolation → concatenation tokens
│          │    Unit suffixes → literal + unit metadata
└────┬─────┘
     │
     ▼
┌──────────┐
│  PARSER  │  → Abstract Syntax Tree (AST)
│          │    Handles precedence climbing for expressions
│          │    Biological constructs (phase, when, adapt) → specialized AST nodes
│          │    Decorators → attached to function/class AST nodes
└────┬─────┘
     │
     ▼
┌──────────────┐
│  ANALYZER    │  → Annotated AST
│              │    Type inference and checking (optional annotations)
│              │    Energy cost calculation per gene
│              │    Scope resolution (closures, class members)
│              │    Trait conformance validation
│              │    Genome slot assignment
│              │    Budget analysis (base case + worst case)
└────┬─────────┘
     │
     ▼
┌──────────────┐
│  CODEGEN     │  → Bytecode for Growl VM
│              │    Gene functions → callable bytecode blocks
│              │    Phase/when/respond → event-triggered bytecode
│              │    Cycle/adapt → scheduled bytecode
│              │    Depot calls → IPC instructions
│              │    Biological API calls → VM intrinsics
└────┬─────────┘
     │
     ▼
┌──────────────┐
│  GROWL VM    │  Executes bytecode each game tick
│              │  Per-organism isolated runtime
│              │  Energy metering (halt gene if over budget)
│              │  Error → mutation mapping
│              │  Depot bridge (shared memory bus)
│              │  Signal bridge (organism-to-organism events)
└──────────────┘
```

### VM Design Considerations

- **Tick-based execution**: Each organism's genes execute once per game tick.
  Gene priority determines execution order within a tick.
- **Isolated heaps**: Each organism has its own memory space. No direct
  access to another organism's state (only through signals and depot).
- **Energy metering**: The VM tracks energy consumed per instruction.
  If a gene exceeds its budget mid-execution, remaining instructions
  are skipped and the organism takes proportional stress.
- **Deterministic randomness**: `random()` is seeded per-organism per-tick.
  Same seed + same state = same result. This makes debugging reproducible
  and enables replay/rewind.
- **Instruction limit per tick**: Prevent infinite computation. If a gene
  exceeds ~10,000 instructions in a single tick, it's halted and the
  "InfiniteLoop" mutation triggers.
- **Hot reload**: The player can modify a gene on the terminal and flash it
  to a living organism. The VM hot-swaps the gene bytecode without
  killing the organism. State in org.memory persists across reloads.
