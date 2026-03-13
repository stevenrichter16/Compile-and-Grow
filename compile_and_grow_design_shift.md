# Compile & Grow

## Design Shift Document

### From Factory Automation → Commission-Based Bio-Programming

------------------------------------------------------------------------

# 1. Overview

This document outlines a design shift for **Compile & Grow** from a
**Satisfactory-style factory automation game** toward a
**commission-based bio-programming game**.

**New core description**

> *Compile & Grow is a post-apocalyptic bio-programming game where you
> take commissions and write code for custom plants that survive, adapt,
> and solve human problems.*

Instead of focusing on industrial production chains and large-scale
automation, the game centers on **writing code for individual plants
that perform specialized behaviors** in response to environmental and
human needs.

This direction aligns closely with the strengths already emerging in the
current branch of development:

-   programmable plant behavior
-   biological runtime systems
-   example plant archetypes
-   a code-first gameplay loop
-   expressive biological mechanics

------------------------------------------------------------------------

# 2. Original Direction: Factory Automation

The earlier direction drew inspiration from games such as:

-   Satisfactory
-   Factorio
-   Dyson Sphere Program

These games emphasize:

## Core mechanics

-   resource extraction
-   production chains
-   conveyor logistics
-   spatial factory optimization
-   throughput scaling

## Gameplay fantasy

The player becomes an **industrial architect**, building ever larger
automated production systems.

## Typical gameplay loop

gather resources → build machines → connect logistics → optimize
throughput → scale factory

## Implications for Compile & Grow

Under this model, plants would effectively function as:

-   biological machines
-   production nodes
-   components in larger industrial systems

While possible, this direction risks making plants feel like **novel
factory assets rather than living systems**.

------------------------------------------------------------------------

# 3. Observed Strengths of the Current Systems

Through development and discussion, several systems have emerged that
are **much stronger when framed around programming organisms rather than
building factories**.

## Behavioral plant archetypes

Examples already implemented include:

-   BalancedPlant
-   StoragePlant
-   WideCanopy
-   WaterSaver
-   FastGrower

These represent **distinct behavioral strategies**, not production
modules.

They already imply a design space where plants are:

-   specialized
-   responsive to conditions
-   tuned for survival tradeoffs

## Biological programming constructs

The runtime already supports expressive constructs such as:

-   lifecycle phases
-   event responses
-   adaptive behaviors
-   cyclical routines
-   tick-based scheduling

These mechanics are far closer to **programming a living system** than
to building a factory.

## Biological simulation features

Additional systems reinforce the organism-centric design:

-   environment proxy
-   nutrient tracking
-   mutation responses to errors
-   modular biological abilities
-   adaptive runtime behavior

These systems create a strong foundation for **programming organisms
with complex behaviors**.

------------------------------------------------------------------------

# 4. The New Direction

## Core Concept

The player is a **plant programmer** in a post-apocalyptic world.

They run a small bioengineering workshop where survivors bring problems
that can be solved using custom-programmed plants.

Examples include:

-   food crops that survive toxic soil
-   vines that stabilize ruined structures
-   moss that glows without attracting raiders
-   roots that filter contaminated water
-   plants that signal caravans at night

The player writes **Growl code** that controls:

-   plant growth
-   environmental responses
-   energy strategies
-   behavior patterns
-   morphology changes

------------------------------------------------------------------------

# 5. Core Gameplay Loop

receive commission → analyze environment and constraints → write plant
code → simulate plant growth → debug behavior → deliver organism

### Step 1: Commission

A client provides:

-   environmental conditions
-   desired behavior
-   constraints
-   budget

### Step 2: Programming

The player writes or modifies Growl code controlling:

-   morphology
-   growth strategy
-   intake
-   energy production
-   reactions to stimuli

### Step 3: Simulation

The plant grows inside a test environment where the player observes:

-   growth patterns
-   resource use
-   failures
-   unintended mutations

### Step 4: Iteration

The player refines the code until the plant meets the commission
requirements.

### Step 5: Delivery

The plant is delivered and the player receives:

-   payment
-   reputation
-   unlocks

------------------------------------------------------------------------

# 6. World Context

The setting remains **post-apocalyptic**, which provides strong
narrative and gameplay motivation.

Society has partially collapsed, leaving survivors dependent on
improvised solutions.

Plants have become essential infrastructure.

Examples of everyday needs:

-   heat
-   dust
-   poor soil
-   contaminated water
-   ruined buildings
-   scarcity
-   salvage culture

More unusual commissions may come from:

-   religious cults
-   smugglers
-   scientists
-   collectors
-   eccentric survivors

This structure allows missions to range from **practical survival
problems to strange or unsettling requests**.

------------------------------------------------------------------------

# 7. Accessibility with Depth

A key design goal is combining **approachability with extremely deep
systems**, similar to the programming game **The Farmer Was Replaced**.

### Accessible entry point

Early players should be able to succeed using:

-   small scripts
-   simple behaviors
-   template genomes
-   straightforward conditions

Example:

    if water < 0.3:
        close_stomata()

### Depth for advanced players

Advanced players can create:

-   adaptive environmental logic
-   multi-phase lifecycle behaviors
-   dynamic energy strategies
-   mutation-resilient organisms
-   specialized environmental optimizations

This dual-layer design ensures:

-   beginners are not overwhelmed
-   experts have endless experimentation space

------------------------------------------------------------------------

# 8. Advantages of the Commission Model

## Clear goals

Each commission defines:

-   success conditions
-   environment
-   constraints

## Narrative context

Clients bring personality and worldbuilding.

## Variety

Every mission can explore different mechanics.

## Emergent outcomes

Plants can fail in interesting ways:

-   overgrowth
-   inefficient energy cycles
-   strange mutations
-   unintended behavior

------------------------------------------------------------------------

# 9. Lab-Centric Gameplay

Instead of expanding a factory, the player expands their
**bio-programming lab**.

The lab includes:

-   greenhouse testing chambers
-   coding terminal
-   gene library
-   mutation experiments
-   environmental simulation tools

Progression upgrades the player's **ability to design organisms**, not
their ability to produce mass resources.

------------------------------------------------------------------------

# 10. Comparison: Old vs New Direction

  Factory Direction         Bio-Programming Direction
  ------------------------- -----------------------------
  build production chains   program organisms
  optimize throughput       optimize survival behaviors
  scale factories           design bespoke plants
  logistics focus           biology focus
  spatial automation        behavioral programming

------------------------------------------------------------------------

# 11. Design Summary

Compile & Grow should focus on the fantasy of:

**engineering life through code.**

The player becomes a specialist in solving human problems through
programmable organisms.

By shifting away from factory automation and toward commission-based
bio-programming, the game better leverages its existing systems and
offers a unique design space combining:

-   programming gameplay
-   biological simulation
-   post-apocalyptic storytelling
-   creative experimentation

The result is a game where the player does not build machines.

They **write living software.**
