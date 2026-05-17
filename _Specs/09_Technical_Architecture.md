# 09 — Technical Architecture

## Engine
Unity

## Current Approach
Prototype-first using simple primitives:
- cubes
- cylinders
- spheres
- placeholder effects

## Current Script Categories
### Player
- PlayerMovement
- PlayerProjectileAttack
- future PlayerHealth
- future PlayerStats

### Combat
- Projectile
- HolyRainEffect
- future DamageNumber
- future DamageSystem

### Enemy
- EnemyAI
- EnemyHealth
- EnemySpawner
- HoverHighlight

### UI/Targeting
- HoverDetector
- Billboard
- future SkillBarUI

## Suggested Folder Structure
```text
Assets/
  Scripts/
    Player/
    Enemy/
    Combat/
    UI/
    Systems/
  Prefabs/
    Player/
    Enemies/
    Projectiles/
    Effects/
    UI/
  Materials/
  Scenes/
  Docs/
```

## Senior Developer Notes
Potential refactors:
- Introduce interfaces:
  - IDamageable
  - ITargetable
  - ICastableSkill
- Separate targeting from highlighting.
- Separate skill selection from skill execution.
- Use ScriptableObjects for skills and enemy definitions.
- Avoid FindObjectsOfType long-term.
- Use object pooling for projectiles/effects.
- Use events for health changes and death.

## Future Multiplayer Considerations
If multiplayer is likely, avoid overcoupling:
- input
- movement
- combat
- damage
- targeting

Potential networking options:
- FishNet
- Mirror
- Photon Fusion
- Unity Netcode

Need to decide before large-scale combat architecture.
