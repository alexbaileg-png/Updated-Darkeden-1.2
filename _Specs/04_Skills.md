# 04 — Skills

## Current Skill System
The player has a selected active skill. Function keys switch the selected skill and right click casts the active skill.

Current controls:
- F8 = Holy Bolt
- F10 = Holy Rain
- Right Click = cast selected skill

## Skill 1 — Holy Bolt
### Role
Single-target ranged damage skill.

### Current Behavior
- Fires projectile from player.
- If an enemy is highlighted/targeted, projectile homes.
- If no target is highlighted, fires toward cursor.

### Future Improvements
- glowing projectile trail
- impact burst
- cast sound
- damage type: Holy
- bonus damage to undead/vampires
- possible healing modifier for allies

## Skill 2 — Holy Rain
### Role
Area-of-effect ground-targeted damage skill.

### Current Behavior
- Casts at mouse world position.
- Damages enemies within radius.
- Has longer cooldown than Holy Bolt.

### Future Improvements
- visible ground circle
- falling light beams
- delayed impact option
- damage over time option
- anti-undead bonus
- PvP warning indicator

## Skill Selection Design
The selected skill should persist until another skill is selected.

Future skill bar:
- bottom-right UI
- active skill icon
- cooldown overlay
- keybind display
- selected skill highlight

## Future Slayer/Healer Skills
Possible skills:
- Holy Lance — piercing projectile
- Sanctify — ground zone that damages enemies and heals allies
- Purge — removes debuffs
- Divine Chain — links holy damage between enemies
- Radiant Shield — temporary absorb barrier
- Judgement — high damage single-target spell
- Exorcism — anti-vampire burst
- Consecrated Step — short dash leaving holy trail
