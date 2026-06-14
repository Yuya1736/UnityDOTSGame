# Unity DOTS 游戏

基于 Unity DOTS（数据导向技术栈）开发的手机动作游戏，采用 GPU 加速骨骼动画与大规模敌人 AI 仿真。

## 技术栈

- **Unity 2022.3.55f1c1** · URP 14.0.11
- **Unity Entities 1.3.15** · Entities Graphics 1.4.19
- **GPUECSAnimationBaker** — 骨骼矩阵预烘焙至贴图，顶点着色器蒙皮，替代 CPU SkinnedMeshRenderer
- **Nebukam ORCA** — 互避碰撞算法，用于群体 AI 移动
- **Burst 编译器** · **IJobParallelFor** — 多线程敌人更新与移动

## 功能特性

- **大规模敌人生成** — ECS `EnemySpawnerSystem`，可配置普通敌人与 Boss 的刷新频率
- **群体 AI** — 基于 ORCA 仿真，敌人向玩家靠拢且互不重叠
- **GPU 骨骼动画** — 数千个动画角色，CPU 开销极低
- **玩家战斗系统** — 攻击、冲刺、血条、手机触控输入
- **子弹系统** — 玩家与敌人子弹，VFX（血液、爆炸）对象池复用
- **拾取道具** — 世界物品超时自动回池
- **WorldUnitManager** — 使用压缩 `long` 网格键的 O(1) 空间查询

## 项目结构

```
Assets/
  Script/
    EnemySystem.cs              # 敌人 AI 主逻辑 + ORCA 集成 (SystemBase)
    EnemySpawnerSystem.cs       # ECS 敌人/Boss 生成器 (ISystem)
    EnemyJob.cs / EnemyMoveJob.cs  # Burst IJobParallelFor 移动作业
    WorldUnitManager.cs         # 空间网格（单层 long 键字典）
    Player/                     # 玩家控制器、输入、血条、摄像机
    Weapon/                     # 子弹生成、子弹系统、特效池
    UI/                         # HUD、按钮
  GPUECSAnimationBaker/
    Engine/                     # ECS 动画核心系统与烘焙管线
    Samples/                    # 示例场景与烘焙角色预制体
  Toon_Zombies_extended/        # 僵尸角色资源（3 个变体：01002、01003）
  Resources/Effect/             # VFX 预制体
```

## 快速开始

1. 使用 Unity Hub 以 Unity 2022.3.55f1c1 打开项目文件夹
2. 打开 `Assets/Classical_city/scene_overcast/`
3. 进入 Play 模式 — 敌人自动生成并向玩家移动

### 重新烘焙角色动画

1. 在 Project 窗口中选中蒙皮网格预制体
2. **Tools > GPU ECS Animation Baker > Add baker component**
3. 在 Inspector 中配置动画
4. 点击 **Generate GPU ECS Animator**

## 性能说明

- `EnemySystem` 使用 `EndSimulationEntityCommandBufferSystem`，避免帧中 Sync Point
- `WorldUnitManager` 使用单层 `Dictionary<long, ...>`，通过 `(x << 32 | z)` 压缩键消除嵌套字典的双重哈希开销
- 敌人移动以 `IJobParallelFor`（Burst 编译）分发，消除大规模群体的串行瓶颈
