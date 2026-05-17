# DarkEden-Inspired ARPG — Design Journal

## Vision
A modern spiritual successor inspired by DarkEden.

Core goals:
- Fast-paced faction PvP
- Strong class identity
- Build diversity
- Dark atmosphere
- Smooth modern ARPG controls
- Less level grinding
- More focus on:
  - PvP
  - gearing
  - faction conflict
  - activities
  - skill mastery

Target vibe:
- DarkEden
- Diablo 2 Resurrected
- Path of Exile
- Lost Ark combat feel

---

# Core Design Philosophy

## What We Want To Avoid
- Endless repetitive level grind
- 500-hour mandatory progression before PvP
- Boring fetch quests
- Excessive daily chore systems
- Overly bloated UI
- Hyper-casual mobile feeling

## What We Want To Emphasize
- Fun combat immediately
- Meaningful PvP
- Gear progression
- Skill expression
- Build experimentation
- Faction pride
- Dangerous world feeling
- Rewarding endgame

---

# Current Prototype State

## Working Systems

### Player
- Click-to-move movement
- Hold-left-click movement updating
- Directional facing
- Projectile attacks
- Projectile aiming toward cursor
- Melee combat

### Enemy
- Enemy AI chase
- Health system
- Health bar
- Death state
- Corpse remains temporarily
- Hover outline system
- Enemy spawning

### Combat
- Projectile damage
- Melee damage
- Enemy death
- Directional combat
- Cursor-based aiming

---

# Current Gameplay Loop
1. Move around arena
2. Fight enemies
3. Kill enemies
4. More enemies spawn
5. Continue combat

This is now considered the first real playable combat loop.

---

# Immediate Next Priorities

## Priority 1 — Player Survival
- Player health
- Enemy attacks
- Player death
- Respawn system
- Damage feedback

## Priority 2 — Combat Feel
- Attack animations
- Hit effects
- Sound effects
- Better enemy hit reactions
- Dodge/dash system
- Cooldowns

## Priority 3 — Progression
- XP system
- Levels
- Stats
- Skill points
- Loot drops
- Gear rarity

## Priority 4 — World Systems
- Multiple enemy types
- Spawn zones
- Town area
- NPCs
- Shops
- Faction hubs

---

# Combat Design Notes

## Desired Combat Feel
Combat should feel:
- Responsive
- Weighty
- Dangerous
- Skill-based
- Fast but readable

## Desired PvP Feel
- Gear matters
- Skill matters more
- Positioning matters
- Builds matter
- Teamplay matters

Avoid:
- One-shot meta
- Infinite stunlocks
- Unkillable tanks

---

# Class Philosophy

## Slayer Inspiration
Initial inspiration based on:
- Healer Slayer
- Damage-focused PvP build

Potential direction:
- Hybrid combat/support
- Sustain-based fighting
- High survivability through skill use
- Anti-undead specialization

---

# Faction Concepts

## Humans / Slayers
- Technology
- Holy powers
- Discipline
- Tactical coordination

## Vampires
- Speed
- Sustain
- Ambush combat
- Night dominance

## Ousters / Third Faction (Possible)
- Elemental powers
- Ranged pressure
- Area denial

---

# Art Direction

## Desired Style
- Dark fantasy
- Gothic atmosphere
- Slightly realistic
- Moody lighting
- Not cartoonish

## Graphics Goal
Comparable to:
- Diablo 2 Resurrected modernization
- Stylized realism
- Clean readability during PvP

---

# Technical Direction

## Engine
Unity

## Camera
Isometric ARPG camera

## Networking
Eventually multiplayer-capable architecture should be considered early.

Potential future networking options:
- Mirror
- FishNet
- Photon Fusion
- Netcode for GameObjects

---

# Future Systems Wishlist

## Systems
- Guilds
- Territory control
- Open world PvP
- Duels
- Arenas
- World bosses
- Rare drops
- Dynamic events
- Crafting
- Trade economy

## Long-Term Ideas
- Day/night cycle
- Faction war zones
- PvP ranking
- Corruption systems
- Seasonal content
- Castle sieges

---

# Development Strategy

## Important Rule
DO NOT overbuild too early.

Current focus:
- Playable combat
- Fast iteration
- Fun first

## Prototype First
Use:
- Cubes
- Capsules
- Primitive placeholders

DO NOT waste time on:
- Final art
- Final animations
- Massive worldbuilding

until combat is genuinely fun.

---

# Current Milestone

## Milestone 1
"Playable Combat Sandbox"

Requirements:
- Movement
- Enemy AI
- Combat
- Death
- Spawning
- Health systems
- Basic UI

Status:
IN PROGRESS

---

# Notes / Ideas

## Key Insight
The game should not feel like:
"Work before fun."

Players should reach:
- meaningful combat
- faction identity
- PvP opportunities

quickly.

The long-term grind should focus more on:
- gear
- reputation
- prestige
- optimization

rather than mandatory leveling.

