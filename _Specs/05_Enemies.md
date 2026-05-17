# 05 — Enemies

## Current Enemy Features
- Spawns from EnemySpawner
- Chases player
- Has health
- Displays health bar
- Can die
- Corpse remains temporarily
- Can be targeted by skills
- Can be detected by hover system

## Current Enemy Components
Expected root Enemy components:
- Transform
- Mesh Renderer
- Mesh Filter
- Box Collider
- EnemyAI
- EnemyHealth
- HoverHighlight

Optional child objects:
- Canvas / HealthBar
- EnemyOutline

## Enemy AI Current Behavior
- Finds assigned player Transform
- Chases if player is inside chase range
- Stops at stop distance

## Needed Next Enemy Features
### Enemy Attack
Enemies need:
- attack range
- attack cooldown
- damage amount
- attack animation/effect placeholder
- player health interaction

### Enemy Types
Early enemy archetypes:
1. Ghoul
   - basic melee chaser
2. Blood Crawler
   - faster but weaker
3. Cultist
   - ranged projectile caster
4. Rot Brute
   - slow, high health
5. Vampire Initiate
   - lifesteal attacker

## Enemy Spawning
Current system spawns enemies around origin using random positions.

Future spawn system should support:
- spawn zones
- enemy type weights
- max alive count
- wave spawning
- faction-controlled spawns
- elite spawn chance

## Death Design
Current death:
- enemy stops
- enemy lays flat
- health bar hides
- corpse disappears after delay

Future death:
- blood effect
- loot drop
- XP event
- corpse fade
- death sound
