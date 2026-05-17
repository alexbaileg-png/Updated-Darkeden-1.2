# 03 — Combat System

## Current Combat Features
- Click-to-move movement
- Hold left click to continuously update movement target
- Right click casts selected skill
- F8 selects Holy Bolt
- F10 selects Holy Rain
- Holy Bolt can home onto highlighted/targeted enemies
- Holy Rain damages enemies in an AoE near the mouse position
- Basic enemy AI chases the player
- Enemy health and death state exist

## Movement Goals
Movement should feel:
- responsive
- accurate
- smooth
- ARPG-like
- compatible with kiting and repositioning

## Targeting Goals
Targeting should support:
- mouse hover detection
- target highlighting
- target-aware skills
- non-targeted ground casting
- future PvP targeting

## Skill Casting Rules
### Holy Bolt
- Right click while Holy Bolt is selected.
- If enemy is highlighted, projectile homes toward that enemy.
- If no enemy is highlighted, projectile fires toward cursor direction.

### Holy Rain
- Right click while Holy Rain is selected.
- Casts at mouse location.
- Damages enemies within radius.
- Should later show a visible holy/light impact effect.

## Damage Model
Current damage is simple direct integer damage.

Future damage types:
- Holy
- Physical
- Fire
- Blood
- Shadow
- Arcane
- Poison
- Lightning

Future defensive stats:
- armor
- evasion
- elemental resistance
- holy resistance
- blood resistance
- crowd-control resistance

## Combat Feel Priorities
Near-term improvements:
- hit flash
- damage numbers
- cast effects
- projectile trail
- enemy hit reaction
- player health
- enemy attacks
- cooldown UI
- better death visuals

## PvP Design Warning
Avoid:
- instant one-shot builds
- permanent stunlocks
- unavoidable homing spam
- overly defensive immortal builds

The goal is asymmetrical but fair-feeling combat.
