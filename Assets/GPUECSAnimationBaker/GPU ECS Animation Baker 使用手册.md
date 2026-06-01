# GPU ECS Animation Baker 使用手册

## 目录

- [概述](#概述)
- [安装](#安装)
  - [系统要求](#系统要求)
  - [安装指南](#安装指南)
  - [资源内容概览](#资源内容概览)
  - [限制说明](#限制说明)
- [快速开始](#快速开始)
  - [1. 适配材质/着色器](#1-适配材质着色器)
  - [2. 挂载 Baker 组件](#2-挂载-baker-组件)
  - [3. 配置并生成](#3-配置并生成)
- [配置项说明](#配置项说明)
  - [1. 动画列表](#1-动画列表)
  - [2. 生成 AnimationIds 枚举](#2-生成-animationids-枚举)
  - [3. 使用预定义动画事件 ID](#3-使用预定义动画事件-id)
  - [4. 生成 AnimationEventIds 枚举](#4-生成-animationeventids-枚举)
  - [5. 挂载锚点](#5-挂载锚点)
  - [6. 生成 AttachmentAnchorIds 枚举](#6-生成-attachmentanchorids-枚举)
  - [7. 骨骼用量](#7-骨骼用量)
  - [8. Transform Usage Flags（父级与子级）](#8-transform-usage-flags父级与子级)
  - [生成流程](#生成流程)
- [使用 GpuEcsAnimator 对象](#使用-gpuecsanimator-对象)
- [进阶主题](#进阶主题)
  - [混合（Blending）](#混合blending)
  - [挂件（Attachments）](#挂件attachments)
  - [动画事件（Animation Events）](#动画事件animation-events)
  - [使用预定义动画事件](#使用预定义动画事件)
  - [GPU 实例化](#gpu-实例化)
  - [通过自定义编辑器代码调用 Baking 服务](#通过自定义编辑器代码调用-baking-服务)
- [技术支持](#技术支持)

---

## 概述

**GPU ECS Animation Baker** 将 DOTS ECS 与 GPU 动画实例化技术相结合，让你能够同时为数以万计的角色播放各自独立的动画。

其原理是将所有顶点骨骼权重烘焙到网格的 UV 通道（uv1、uv2、uv3）中，并将每帧的骨骼变换矩阵烘焙到动画纹理里。自定义顶点着色器负责处理其余的渲染工作，ECS 动画系统则负责每帧更新着色器参数。

自定义顶点着色器以 ShaderGraph 子图（Subgraph）的形式实现，可以方便地集成到你现有的任意 ShaderGraph 着色器中。

该 Baker 支持 URP 和 HDRP、LOD、混合采样以及动画过渡。借助这项技术，你可以轻松在屏幕上呈现数万个独立动画角色。

---

## 安装

### 系统要求

GPU ECS Animation Baker v.1 需要 **Unity 2022.2.20f1 或更高版本**，以及 **ECS 1.0.8 或更高版本**。

同时需要安装以下两个包：
- **Entities**
- **Entities Graphics**

该资源会持续更新以保持与最新 ECS 版本的兼容性。

该包完整支持 HDRP 和 URP。但所有示例场景均在 URP 下制作，因此如果你计划在 HDRP 项目中使用，建议先在 URP 测试项目中学习。将示例场景转换为 HDRP 并不复杂——`Samples/Shaders` 文件夹中同时提供了 URP 和 HDRP 两个版本的示例着色器。

### 安装指南

下载资源后，至少需要导入 **Engine** 文件夹中的所有内容，这已足够使用该工具。不过 **Samples** 文件夹对于学习非常重要。

请确保已通过 Package Manager 将 **Entities** 和 **Entities Graphics** 包添加到项目中。

建议将渲染路径切换为 **URP Forward+**，Entities Graphics 需要此设置，否则会收到警告。

如果需要运行示例场景菜单，请将所有示例场景添加到 Build 菜单中。

现在你已准备好使用 GPU ECS Animation Baker！

### 资源内容概览

资源目录结构如下：

```
GPUECSAnimationBaker/
├── Engine/          # Baker 的所有运行时与编辑器代码
└── Samples/
    ├── Characters/  # 所有角色模型、材质、贴图及烘焙动画对象
    └── SampleScenes/
        # 示例场景按复杂度排列
        # Festival 和 Marathon 是大型人群演示场景
        # SampleScenesMenu 可浏览所有示例场景
```

学习建议：跟随教程、研究示例场景，并阅读本文档了解细节。

- 教程：https://www.youtube.com/playlist?list=PLerop1JzrAobUF2d8Iafj5waqSq3Ib8k1
- 演示：https://www.youtube.com/playlist?list=PLerop1JzrAoa8Vpz1p3Ps8EGRduhaDlP3

### 限制说明

> **免责声明：** GPU ECS Animation Baker 专为性能而生，擅长同时为大量角色播放动画，但它在功能多样性和丰富性上无法与 Unity 原生的 Animator / Skinned Mesh Renderer 方案相比。使用前请注意以下几点：

- **UV 通道限制：** 网格中只能使用 UV0。UV 通道 1、2、3 由 Baker 占用，因此不支持使用这些通道的模型。
- **必须使用 ShaderGraph 着色器：** 材质需要使用 ShaderGraph 着色器。虽然理论上可以为代码着色器实现 Baker，但这超出了当前功能范围。如需自行实现，可参考 `/GPUECSAnimationBaker/Engine/Shader` 中的着色器代码。
- **不支持运行时 Animator：** 所有经典 Animator 功能均支持烘焙和采样，但经典 Animator 本身在运行时**不会执行**。这意味着所有需要运行时骨骼结构计算的功能（如 IK）均不受支持，且永远无法支持。
- **HDRP 示例场景：** 该包完整支持 HDRP 和 URP，但所有示例场景均在 URP 下制作。如计划用于 HDRP 项目，建议先在 URP 测试项目中学习，转换并不复杂。`Samples/Shaders` 文件夹中提供了两个版本的示例着色器。

---

## 快速开始

一切从一个包含 Animator 和一个或多个 SkinnedMeshRenderer（可选 LODGroup）的经典 GameObject 开始，烘焙流程将以此对象为起点。

### 1. 适配材质/着色器

首先需要将 SkinnedMeshRenderer 所使用的着色器扩展，加入我们专用的 ShaderGraph **GpuEcsAnimator 子图**。

以一个简单的 URP Lit 着色器（包含基础贴图和法线贴图）为例，只需按如下方式添加 GpuEcsAnimator 子图，即可使其兼容 GPU ECS Animation：

着色器需要额外定义 3 个参数：

**1) EnableAnimation**
这是一个布尔值，用于在禁用动画着色器时正常预览原始资源。ECS Baker/转换器会自动启用动画。
- 需要暴露（Exposed）
- 必须勾选 **Override Property Declaration** 并设置为 **Hybrid Per Instance**

**2) AnimatedBoneMatrices**
该纹理包含所有动画数据。每行代表一帧动画，每 4 列代表一根网格骨骼。
- 需要暴露（Exposed）

**3) AnimationState**
在运行时驱动动画状态。
- 必须勾选 **Override Property Declaration** 并设置为 **Hybrid Per Instance**

> **提示：** 最简单的方式是直接从示例着色器（位于 `/GPUECSAnimationBaker/Samples/Shaders`）中复制粘贴这 3 个参数和子图。

### 2. 挂载 Baker 组件

选中要烘焙 GPU ECS Animator 的 Animator GameObject，通过菜单挂载 Baker 组件。

### 3. 配置并生成

完成所有烘焙设置的配置后，点击生成按钮即可生成 GPU ECS Animator 对象。

---

## 配置项说明

### 1. 动画列表

配置所有需要烘焙的动画：

- **Animation ID**：仅用于生成枚举代码文件时标识动画（见下文）
- **Animator State Name**：采样时使用的 Animator 状态名称（不限层级）
- **Animation Type**，可选以下两种：

  **1) Single Clip（单片段）**
  播放单个（非混合）动画。
  - *Animation Clip*：仅用于确定动画时长（*）

  **2) Dual Clip Blend（双片段混合）**
  通过 Animator 参数在两个动画之间混合，Baker 会在该参数的一定范围内采样。
  - *Blend parameter name*：用于混合的 Animator 参数名
  - *Clip1, Clip2*：两个动画片段
  - *Parameter Value*：混合起止的参数值
  - *Animation Clip*：仅用于确定动画时长（*）
  - *Nbr of in between samples*：采样数量，越多效果越好，但烘焙的动画纹理也越大
  - *Loop*：是否循环播放
  - *Additional parameter values*：采样时使用的其他 Animator 参数（与混合无关），需指定名称、类型和值
  - *Additional animator states per layer*：其他层上需要设置的 Animator 状态，需指定层索引和状态名称

> （*）需要注意：决定动画纹理生成结果的是 Animator 状态（及各层附加状态），而非实际设置的动画片段。动画片段仅作为参考，用于确定烘焙采样的时长。

### 2. 生成 AnimationIds 枚举

生成枚举文件可以让你在代码中以更友好的方式控制动画。若不生成枚举，则需要通过索引来指定播放哪个动画。

同时还会生成一个便捷的初始化行为脚本，可挂载到烘焙后的 GpuEcsAnimator 上，无需额外代码即可初始化动画。

### 3. 使用预定义动画事件 ID

若不勾选此项，Baker 会自动从动画中找到所有事件并根据发现的内容构建 ID 列表。你也可以提供一个预定义列表（详见进阶主题 → 动画事件）。

### 4. 生成 AnimationEventIds 枚举

与 Animation Ids 枚举类似，生成枚举文件可以让你在代码中以更友好的方式标识事件。若使用预定义动画事件 ID，你可以直接控制生成内容。

### 5. 挂载锚点

配置所有需要烘焙的挂载锚点：

- **Attachment Anchor ID**：仅用于生成枚举代码文件时标识锚点（见下文）
- **Attachment Anchor Transform**：需要指向 Animator 骨骼层级中的某个 GameObject

### 6. 生成 AttachmentAnchorIds 枚举

与 Animation Ids 枚举类似，生成枚举文件可以让你在代码中以更友好的方式标识锚点。同时还会生成一个便捷的初始化行为脚本，可挂载到挂件上，无需额外代码即可让其保持在父实体的正确位置。

### 7. 骨骼用量

需要指定每个顶点默认使用的骨骼数量。骨骼数越多，效果越好（顶点定位更精确），但运行时对 GPU 的要求也越高。

可以为每个 LOD 级别单独覆盖骨骼数量，以进一步优化性能。

### 8. Transform Usage Flags（父级与子级）

这些标志将用于 ECS 实体 Baker，分别作用于 Animator（父级）和子级（网格）。详情请参考 Unity ECS Baking 文档中的 `TransformUsageFlags` 枚举说明。

### 生成流程

生成器会创建一个新目录（若不存在），命名格式为 `BakedAssets_` + Animator GameObject 名称。目录内将生成以下文件：

- 用于标识动画、事件和挂件的枚举及初始化 C# 文件（可选）
- 动画纹理
- 已分配动画纹理的原始材质副本
- 已将顶点骨骼权重烘焙到 UV 坐标（uv1、uv2、uv3）的网格副本
- GpuEcsAnimator 对象（可由 Unity ECS Baker 烘焙为实体）

---

## 使用 GpuEcsAnimator 对象

查看 GpuEcsAnimator 可以发现，它会镜像原始 Animator GameObject 的结构。所有 SkinnedMeshRenderer 组件都已替换为普通 MeshRenderer 组件，并分配了生成的材质和网格。所有 LOD 也已完成转换，并挂载了带有 ECS 转换 Baker 的 MonoBehaviour。

使用该实体最简单的方式是将其放入 Subscene（这会告知 Unity 需要将其转换为 ECS 实体）。但在大多数实际游戏场景中，你会将其作为 ECS 实体生成系统的预制体使用。

动画由以下两个系统控制：

- **GpuEcsAnimatorSystem**：负责准备发送给着色器的数据
- **GpuEcsAnimatedMeshSystem**：负责将准备好的数据填充到所有材质覆盖中

作为用户，你只需在想要播放动画的实体上设置 `GpuEcsAnimatorControlComponent` 组件即可：

```csharp
using Unity.Entities;

namespace GPUECSAnimationBaker.Engine.AnimatorSystem
{
    public struct GpuEcsAnimatorControlComponent : IComponentData
    {
        public AnimatorInfo animatorInfo;      // 要播放的动画的所有信息
        public float startNormalizedTime;      // 从任意位置开始动画（0 到 1）
        public float transitionSpeed;          // 切换到另一个动画时的过渡速度
    }

    public struct AnimatorInfo
    {
        public int animationID;    // 唯一动画 ID，可从生成的枚举文件中赋值
        public float blendFactor;  // 0 到 1，从 clip1 过渡到 clip2
        public float speedFactor;  // <1 减慢动画，>1 加快动画
    }

    public enum GpuEcsAnimatorControlStates
    {
        Start,
        Stop,
        KeepCurrentState
    }

    public struct GpuEcsAnimatorControlStateComponent : IComponentData
    {
        public GpuEcsAnimatorControlStates state;
    }
}
```

通常在需要切换到另一个动画，或需要修改正在播放的动画参数（blendFactor 或 speedFactor）时设置此组件。

`GpuEcsAnimatorControlStateComponent` 可用于启动已停止的动画或停止正在播放的动画。当动画未设置为循环且播放完一个周期后，会自动停止。

此外，为了方便使用，还提供了 `GpuEcsAnimatorAspect` Aspect 类来控制动画：

```csharp
using Unity.Entities;

namespace GPUECSAnimationBaker.Engine.AnimatorSystem
{
    public readonly partial struct GpuEcsAnimatorAspect : IAspect
    {
        private readonly RefRW<GpuEcsAnimatorControlComponent> control;
        private readonly RefRW<GpuEcsAnimatorControlStateComponent> controlState;

        public void RunAnimation(int animationID,
            float blendFactor = 0f, float speedFactor = 1f,
            float startNormalizedTime = 0f, float transitionSpeed = 0f)
        {
            control.ValueRW = new GpuEcsAnimatorControlComponent()
            {
                animatorInfo = new AnimatorInfo()
                {
                    animationID = animationID,
                    blendFactor = blendFactor,
                    speedFactor = speedFactor
                },
                startNormalizedTime = startNormalizedTime,
                transitionSpeed = transitionSpeed
            };
            StartAnimation();
        }

        public void StartAnimation()
        {
            controlState.ValueRW = new GpuEcsAnimatorControlStateComponent() {
                state = GpuEcsAnimatorControlStates.Start
            };
        }

        public void StopAnimation()
        {
            controlState.ValueRW = new GpuEcsAnimatorControlStateComponent() {
                state = GpuEcsAnimatorControlStates.Stop
            };
        }
    }
}
```

还有一个便捷的初始化 MonoBehaviour 脚本，可在烘焙时初始化正确的动画：

```csharp
namespace GPUECSAnimationBaker.Engine.AnimatorSystem
{
    [RequireComponent(typeof(GpuEcsAnimatorBehaviour))]
    public class GpuEcsAnimatorInitializerBehaviour<T> : GpuEcsAnimatorInitializerBehaviour where T : Enum
    {}
}
```

定义一个继承自 `GpuEcsAnimatorInitializerBehaviour` 并指定正确动画枚举类型的新行为，将其挂载到 Animator 对象上，即可在设计时选择初始化动画，使其在烘焙期间就完成初始化。该初始化脚本会随枚举文件一起生成。

挂件同样有对应的初始化脚本（详见挂件章节）。

学习如何控制动画的最佳方式是查看资源附带的示例，其中使用了上述所有方法。

---

## 进阶主题

### 混合（Blending）

经典 Unity Animator 通过对骨骼结构相对角度进行插值来实现混合。但在 GpuEcsAnimationBaker 中这是不可能的，因为每个顶点位置是在着色器中根据预先按帧烘焙的骨骼矩阵计算得出的。插值改为在顶点位置之间进行。需要注意的是，当两个混合姿势差异较大时，可能会产生不理想的网格变形。以下是三种会应用顶点插值混合的场景：

**1) 过渡（Transitions）**
两个不同动画之间的过渡通常不超过半秒。在这种情况下，变形不易察觉，尤其是在屏幕上有大量角色的场景中。

**2) 双片段混合（Dual Clip Blends）**
在两个姿势之间混合（例如走路和跑步循环）时，混合是连续的，简单的顶点插值效果不佳。但可以通过在烘焙过程中采样足够多的中间姿势来解决——顶点插值将在两个非常相似的姿势之间进行，效果完全正常。

**3) 帧间插值（Frame-to-frame interpolation）**
帧以 30 FPS 采样和烘焙。这意味着在以 60+ FPS 运行的游戏中，插值会在预烘焙的顶点位置之间进行。这两个姿势在定义上非常相似，因此顶点插值效果完全正常。

### 挂件（Attachments）

有时你需要为角色动态挂载不同的对象（剑、服装、工具等），并希望在替换这些对象时动画仍然正常工作。

有两种实现方式：

**方式一：直接绑定到角色网格**
将对象添加并与角色网格一起绑定。挂载的对象将成为 Skinned MeshRenderer，与角色网格一样处理——通过着色器中的顶点位移移动，就像角色顶点一样。这种方式简单快速，但灵活性较低，无法轻松替换或分离对象（例如射击动画结束后飞出的箭）。

**方式二：使用挂载锚点**
在 Baker 中于源骨骼结构的正确位置定义挂载锚点。Baker 会在烘焙过程中记录这些锚点的位置和旋转。在运行时，一个专用的挂载系统会查找带有 `GpuEcsAttachmentComponent` 的实体，并根据组件中定义的锚点 ID 将其放置在正确位置。

提供了便捷的初始化脚本，可自动为你挂载带有正确值的组件。

### 动画事件（Animation Events）

使用经典 Animator 时，你可以在 Unity 动画编辑器中定义事件并调用自定义函数。显然，这些函数在烘焙后的 ECS 实体上下文中不会被调用。

但你仍然可以利用这个接口：将 `GpuEcsAnimationEventBakerBehaviour` 挂载到源对象上，然后将事件定义为调用该行为的 `RaiseEvent(string)` 函数。

这样，Baker 会捕获事件数据，并在运行时通过向实体的 `GpuEcsAnimatorEventBufferElement` 动态缓冲区添加事件来触发它们。你可以编写自己的事件处理器在 ECS 上下文中处理这些事件。

每帧在添加新事件之前，该缓冲区会被清空，因此需要确保每帧读取所有事件，以免遗漏。

### 使用预定义动画事件

在配置中可以选择使用预定义的事件 ID 列表。这意味着 Baker 不会根据动画中找到的内容自动构建列表，而是使用你提供的列表。这让你可以直接控制 ID 及其顺序。如果 Baker 发现了列表中没有的事件，会在烘焙时忽略并发出警告。

当你希望在不同角色之间共享相同事件（这些角色不一定拥有所有事件，或事件顺序不同）时，这非常有用。你可以为所有这些角色设置同一个预定义列表，它们将触发相同的事件。

### GPU 实例化

由于所有动画着色器都基于 ShaderGraph 着色器，GPU 实例化默认支持。只需在材质中启用即可。

### 通过自定义编辑器代码调用 Baking 服务

除了将 Baker 组件挂载到 Animator 并在编辑器中手动配置设置、点击"Generate GPU ECS Animator"按钮外，你也可以通过调用以下函数在自己的编辑器代码中自动化这一流程：

```csharp
namespace GPUECSAnimationBaker.Engine.Baker
{
    public static class GpuEcsAnimationBakerServices
    {
        public static GameObject GenerateAnimationObject(
            string assetPath,
            GpuEcsAnimationBakerData bakerData,
            string animatorName,
            string generatedAssetsFolder,
            string nameSuffixAsset = "_GpuEcsAnimator",
            string nameSuffixAnimationIDsEnum = "_AnimationIDs",
            string nameSuffixAnimationInitializerBehaviour = "_AnimationInitializerBehaviour",
            string nameSuffixAnimationEventIDsEnum = "_AnimationEventIDs",
            string nameSuffixAnimationAnchorIDsEnum = "_AttachmentAnchorIDs",
            string nameSuffixAttachmentInitializerBehaviour = "_AttachmentInitializerBehaviour",
            string meshPartSuffix = "Mesh",
            string animationMatricesTexturePartSuffix = "AnimationMatricesTexture",
            string materialPartSuffix = "Material")

        public static GameObject GenerateAnimationObjectFromModel(
            GameObject refModel,
            GameObject sourceModel,
            GpuEcsAnimationBakerData bakerData,
            string animatorName,
            string generatedAssetsFolder,
            string nameSuffixAsset = "_GpuEcsAnimator",
            string nameSuffixAnimationIDsEnum = "_AnimationIDs",
            string nameSuffixAnimationInitializerBehaviour = "_AnimationInitializerBehaviour",
            string nameSuffixAnimationEventIDsEnum = "_AnimationEventIDs",
            string nameSuffixAnimationAnchorIDsEnum = "_AttachmentAnchorIDs",
            string nameSuffixAttachmentInitializerBehaviour = "_AttachmentInitializerBehaviour",
            string meshPartSuffix = "Mesh",
            string animationMatricesTexturePartSuffix = "AnimationMatricesTexture",
            string materialPartSuffix = "Material")
    }
}
```

**GenerateAnimationObject 参数说明：**
- `assetPath`：要生成 GPU ECS Animator 的源预制体资源路径

**GenerateAnimationObjectFromModel 参数说明：**
- `refModel`：已实例化的参考模型
- `sourceModel`：源模型

**通用参数说明：**
- `bakerData`：所有 Baker 配置数据，与在编辑器中挂载 Baker 组件时看到的配置等价
- `animatorName`：用作所有生成文件名称前缀的名称（如 `{animatorName}_GpuEcsAnimator`、`{animatorName}_AnimationMatricesTexture_{SkinnedMeshRenderer名称}` 等）。通过编辑器 UI 生成时，此名称即为原始资源名称
- `generatedAssetsFolder`：烘焙过程中存放所有生成资源的文件夹。通过编辑器 UI 生成时，此文件夹为源预制体所在目录下名为 `BakedAsset_{资源名称}` 的子文件夹。使用此函数可以自定义文件夹路径
- `命名后缀参数`：这些字符串参数允许你完全控制各类生成文件的命名，默认值在大多数情况下已足够使用

通过这种方式，你可以编写自己的生成器，一次性为项目中的多个 Animator 生成资源，共享全部或部分配置，并自行决定所有资源的存放位置。

---

## 技术支持

- 官网：http://www.headfirststudios.com/theorangecoder
- Discord：https://discord.gg/DnBzvMC2uG
- 邮箱：pbosteels@gmail.com
