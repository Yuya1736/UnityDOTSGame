# Unity DOTS Game

A mobile action game built with Unity DOTS (Data-Oriented Technology Stack), featuring GPU-accelerated skeletal animation and large-scale enemy AI simulation.

## Tech Stack

- **Unity 2022.3.55f1c1** · URP 14.0.11
- **Unity Entities 1.3.15** · Entities Graphics 1.4.19
- **GPUECSAnimationBaker** — bone matrices pre-baked into textures, vertex shader skinning, replaces CPU SkinnedMeshRenderer
- **Nebukam ORCA** — reciprocal collision avoidance for crowd AI movement
- **Burst Compiler** · **IJobParallelFor** — multithreaded enemy update and movement

## Features

- **Large-scale enemy spawning** via ECS `EnemySpawnerSystem` with configurable spawn rates for normal enemies and bosses
- **Crowd AI** using ORCA simulation — enemies navigate toward the player without overlapping
- **GPU skeletal animation** — thousands of animated characters with minimal CPU overhead
- **Player combat system** — attack, dash, health bar, mobile touch input
- **Projectile system** — player bullets and enemy bullets, pooled VFX (blood, explosions)
- **Pickup items** — world items with timeout auto-return to pool
- **WorldUnitManager** — O(1) spatial lookup using packed `long` grid keys

## Project Structure

```
Assets/
  Script/
    EnemySystem.cs              # Main enemy AI + ORCA integration (SystemBase)
    EnemySpawnerSystem.cs       # ECS enemy/boss spawner (ISystem)
    EnemyJob.cs / EnemyMoveJob.cs  # Burst IJobParallelFor for movement
    WorldUnitManager.cs         # Spatial grid (single-layer long key dictionary)
    Player/                     # PlayerController, input, health, camera
    Weapon/                     # BulletSpawner, BulletSystem, EffectPool
    UI/                         # HUD, buttons
  GPUECSAnimationBaker/
    Engine/                     # Core ECS animation systems & baking pipeline
    Samples/                    # Demo scenes and baked character prefabs
  Toon_Zombies_extended/        # Zombie character assets (3 variants: 01002, 01003)
  Resources/Effect/             # VFX prefabs
```

## Getting Started

1. Open the project folder in **Unity Hub** with Unity 2022.3.55f1c1
2. Open `Assets/Classical_city/scene_overcast/` or the sample scenes under `GPUECSAnimationBaker/Samples/SampleScenes/`
3. Enter Play mode — enemies spawn automatically and navigate toward the player

### Re-baking a character animation

1. Select a skinned-mesh prefab in the Project window
2. **Tools > GPU ECS Animation Baker > Add baker component**
3. Configure animations in the Inspector
4. Click **Generate GPU ECS Animator**

## Performance Notes

- `EnemySystem` uses `EndSimulationEntityCommandBufferSystem` to avoid mid-frame sync points
- `WorldUnitManager` uses a single `Dictionary<long, ...>` with packed `(x << 32 | z)` keys — eliminates the nested dictionary double-hash per cell lookup
- Enemy movement dispatched as `IJobParallelFor` (Burst-compiled), removing the serial bottleneck on large crowds
