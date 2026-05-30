# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Unity Project Overview

- **Unity 2022.3.55f1c1**, URP 14.0.11, **Entities 1.3.15** / Entities Graphics 1.4.19
- Core focus: **GPUECSAnimationBaker** — replaces CPU skinned-mesh rendering with GPU vertex animation. Bone matrices are pre-baked into textures (30 FPS sampling), bone weights stored in UV channels, and vertex shaders do skinning.
- Project template: `com.unity.template.urp-blank` — minimal scene setup.
- **No git repo**, no CI/CD, no test framework set up.

## Architecture

### Assembly Definitions (3 project assemblies)

| Assembly | Path | Purpose |
|---|---|---|
| `GPUECSAnimationBakerEngine` | `Assets/GPUECSAnimationBaker/Engine/` | Core ECS systems, components, bakers, data types |
| `GPUECSAnimationBakerSamples` | `Assets/GPUECSAnimationBaker/Samples/` | Demo scenes, spawners, UI control scripts (references Engine) |
| `Sirenix.OdinInspector.Modules.UnityMathematics` | `Plugins/Sirenix/...` | Editor-only Odin extension |

Engine has `autoReferenced: true`, so it's available to all assemblies. Samples explicitly references Engine.

### ECS Architecture (3 Unmanaged Systems)

All engine systems use **unmanaged `ISystem` + `IJobEntity`** (Burst-compiled). No `[UpdateInGroup]` attributes — all run in default `SimulationSystemGroup`.

1. **`GpuEcsAnimatorSystem`** — Core animation state machine. Reads control component, advances animation state, computes packed `float4x4` shader data (frame indices + blend factors for up to 2 concurrent animation layers), handles crossfade transitions, raises animation events.

2. **`GpuEcsAnimatedMeshSystem`** — Copies the `float4x4` from the animator entity's `GpuEcsAnimatorShaderDataComponent` to the mesh entity's `GpuEcsMaterialAnimationState` `[MaterialProperty]` component, pushing it to the GPU each frame.

3. **`GpuEcsAttachmentSystem`** — Reads interpolated anchor transforms from the animator buffer and updates attachment entity `LocalTransform`.

### Entity Archetypes

- **Animator Entity**: Core animation data + buffers for animation metadata, events, attachment anchors. Controlled via `GpuEcsAnimatorControlComponent`.
- **Animated Mesh Entity**: `GpuEcsAnimatedMeshComponent` (links to animator entity) + material property components. Receives shader data each frame.
- **Attachment Entity**: `GpuEcsAttachmentComponent` + `LocalTransform` + `Parent`.

### Baking Pipeline (Editor-Only, `#if UNITY_EDITOR`)

- `GpuEcsAnimationBakerServices` — Core pipeline: validates, samples animations at 30 FPS, bakes bone matrices to Texture2D, bakes bone weights to mesh UVs, generates enum/initializer code files.
- `GpuEcsAnimationBakerEditor` — CustomEditor with "Generate GPU ECS Animator" button.
- Located in `Engine/Baker/` and `Engine/Data/`.

### Sample Systems

Sample scenes use **managed `SystemBase`** for UI interaction (polling input, updating sliders). Spawner systems (CrowdSpawner, RunnerSpawner) use the unmanaged `ISystem` pattern. Control is done via `GpuEcsAnimatorAspect` which wraps `GpuEcsAnimatorControlComponent`.

### Key ECS Packages

- `com.unity.entities` 1.3.15
- `com.unity.entities.graphics` 1.4.19
- `com.unity.render-pipelines.universal` 14.0.11

### Precompiled DLL References (in Engine asmdef)

- `DOTween.dll`, `DOTweenEditor.dll`
- `Newtonsoft.Json.dll`

## Key Scripting Defines

`ODIN_INSPECTOR;ODIN_INSPECTOR_3;ODIN_INSPECTOR_3_1;ODIN_INSPECTOR_3_2;ODIN_INSPECTOR_3_3`

## Burst Settings

Enabled, optimized, safety checks disabled. CPU targets: SSE4.1 (x86) / AVX2 (x64).

## Project Structure

```
Assets/
  GPUECSAnimationBaker/
    Engine/
      AnimatorSystem/   -- ECS components, systems, aspects, bakers, behaviours
      Baker/            -- Editor-only baking pipeline (#if UNITY_EDITOR)
      Data/             -- Serializable data types + property drawers
      Shader/           -- GpuEcsAnimator.cginc (vertex skinning include)
    Samples/
      Characters/       -- Baked character prefabs with auto-generated code
      SampleScenes/     -- 8 demo scenes (Basics, LODs, Transitions, Blending, Attachments, Events, Festival, Marathon)
      Shaders/          -- URP/HDRP Shader Graph variants
  Scenes/               -- SampleScene.unity only (template default)
  Plugins/              -- Odin Inspector
  Resources/Effect/     -- VFX prefabs (blood, explosions, projectiles)
```

## Common Tasks

### Open project in Unity
Open `ECSExample.sln` in Rider/VS, or open project folder directly in Unity Hub.

### Build scenes
Only `Assets/Scenes/SampleScene.unity` is in Build Settings. To build for StandaloneWindows:
```
Unity.exe -quit -batchmode -buildWindowsPlayer Build/ECSExample.exe
```

### Run the animation baker
1. Select a skinned-mesh prefab in the Project window
2. Click **Tools > GPU ECS Animation Baker > Add baker component**
3. Configure animations in the Inspector
4. Click **Generate GPU ECS Animator**

### ECS code generation
The baker auto-generates:
- `*_AnimationIDs.cs` — enum of animation IDs
- `*_AnimationInitializerBehaviour.cs` — initializer MonoBehaviour
- `*_AnimationEventIDs.cs` (if events are configured)
- `*_AttachmentAnchorIDs.cs` (if attachments are configured)
- `*_AttachmentInitializerBehaviour.cs` (if attachments are configured)

Generated files appear in subfolders under the baked prefab's directory.

## Important Code Patterns

- **Adding a new animation**: Add it to `GpuEcsAnimationBakerData.animations[]` on the baker behaviour, then re-bake.
- **Adding a new ECS system**: Create an unmanaged `partial struct : ISystem` with `IJobEntity`, add to `GPUECSAnimationBakerEngine` assembly.
- **Controlling animation at runtime**: Use `GpuEcsAnimatorAspect.RunAnimation(animationID, blendFactor, speedFactor, startNormalizedTime, transitionSpeed)`.
- **Attachments**: Must be pre-baked with anchors defined in the baker. Anchor transforms are baked per-frame into a `GpuEcsAttachmentAnchorData` ScriptableObject.
