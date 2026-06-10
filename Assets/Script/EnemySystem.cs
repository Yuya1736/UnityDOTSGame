using GPUECSAnimationBaker.Engine.AnimatorSystem;
using Nebukam.Common;
using Nebukam.ORCA;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateBefore(typeof(GpuEcsAnimatorSystem))]
public partial class EnemySystem : SystemBase
{
    private GameObject _player;
    private PlayerController _playerController;
    private ORCABundle<Agent> _simulation;
    public Dictionary<int, AIEntity> etLst = new Dictionary<int, AIEntity>(10000);

    private EntityQuery _agentQuery;
    private BulletSpawner _bulletSpawner;
    private Dictionary<int, EnemyBulletConfig> _enemyBulletConfigs;
    private Entity _bulletPrefabEntity;
    private const int MeleeDamage = 1;
    private EndSimulationEntityCommandBufferSystem _ecbSystem;

    protected override void OnCreate()
    {
        _agentQuery = GetEntityQuery(
            ComponentType.ReadWrite<AgentComponent>(),
            ComponentType.ReadWrite<LocalTransform>(),
            ComponentType.ReadOnly<GpuEcsAnimatorStateComponent>()
        );
        _ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {

        if (_player == null)
        {
            _player = GameObject.FindWithTag("Player");
            _simulation = new ORCABundle<Agent>();
            _simulation.plane = AxisPair.XZ;
            if (_player == null)
            {
                Debug.LogError("Player GameObject not found in the scene.");
                return;
            }
            _bulletSpawner = _player.GetComponent<BulletSpawner>();
            _playerController = _player.GetComponent<PlayerController>();

            // 加载怪物子弹配置（Resources1 不在 Unity 标准 Resources 路径，通过 EnemyBulletConfigProvider 获取）
            _enemyBulletConfigs = new Dictionary<int, EnemyBulletConfig>();
            var configProvider = Object.FindObjectOfType<EnemyBulletConfigProvider>();
            if (configProvider != null)
            {
                foreach (var cfg in configProvider.configs)
                {
                    if (cfg != null) _enemyBulletConfigs[cfg.unitId] = cfg;
                }
            }
            else
            {
                Debug.LogError("[EnemySystem] EnemyBulletConfigProvider not found in scene! Boss will not fire.");
            }

        }

        // 每帧尝试获取子弹预制体 Entity，直到 SubScene Bake 完成后才会有值
        if (_bulletPrefabEntity == Entity.Null)
        {
            var bsQuery = World.EntityManager.CreateEntityQuery(typeof(BulletSpawnerComponent));
            if (bsQuery.CalculateEntityCount() > 0)
            {
                var arr = bsQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
                _bulletPrefabEntity = World.EntityManager.GetComponentData<BulletSpawnerComponent>(arr[0]).bulletPrefab;
                arr.Dispose();                                                                                                                                                                                                                           
            }
            bsQuery.Dispose();
        }

        float3 playerPos = _player.transform.position;
        float deltaTime = SystemAPI.Time.DeltaTime;

        var entityManager = World.EntityManager;
        var ecb = _ecbSystem.CreateCommandBuffer();

        // 批量读取三个组件，替代循环内逐 entity GetComponentData
        var entities   = _agentQuery.ToEntityArray(Allocator.Temp);
        var agents     = _agentQuery.ToComponentDataArray<AgentComponent>(Allocator.Temp);
        var transforms = _agentQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
        var animStates = _agentQuery.ToComponentDataArray<GpuEcsAnimatorStateComponent>(Allocator.Temp);

        bool agentsDirty     = false;
        bool transformsDirty = false;

        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity             = entities[i];
            AgentComponent agentComponentData = agents[i];

            if (agentComponentData.triggerDie)
            {
                agentComponentData.triggerDie = false;
                agentComponentData.state = 3;
                agentComponentData.dieTimer = 10f;
                agents[i] = agentComponentData;
                agentsDirty = true;

                if (etLst.TryGetValue(agentComponentData.global_id, out AIEntity dyingEntity))
                {
                    // 5% 概率掉落道具
                    if (_bulletSpawner != null && UnityEngine.Random.value < 0.05f)
                        _bulletSpawner.SpawnPickup((Vector3)dyingEntity.localTransform.Position);

                    dyingEntity.Play(ref entityManager, AnimationIds1001.die.GetHashCode());
                    dyingEntity.agent.maxSpeed = 0;
                    _simulation.agents.Remove(dyingEntity.agent);
                    dyingEntity.Dispose();
                    etLst.Remove(agentComponentData.global_id);
                }
                continue;
            }
            if (agentComponentData.state == 3)
            {
                agentComponentData.dieTimer -= deltaTime;
                if (agentComponentData.dieTimer <= 0f)
                    ecb.DestroyEntity(entity);
                else
                {
                    agents[i] = agentComponentData;
                    agentsDirty = true;
                }
                continue;
            }
            if (agentComponentData.state == 0)
            {
                var lt = transforms[i];

                float f1 = UnityEngine.Random.Range(-25, 25);
                float f2 = UnityEngine.Random.Range(-25, 25);

                lt.Position = playerPos + new float3(f1, 0, f2);

                agentComponentData.state = 1;
                agents[i]     = agentComponentData;
                transforms[i] = lt;
                agentsDirty     = true;
                transformsDirty = true;

                var a = _simulation.NewAgent(lt.Position);
                a.id = agentComponentData.global_id;
                a.radius = 0.35f;
                a.radiusObst = 0.35f;
                a.maxSpeed = 1.75f;
                a.prefVelocity = math.normalize(playerPos - a.pos) * 1.75f;
                a.velocity = a.prefVelocity;
                a.timeHorizon = 3f;

                var xx = new AIEntity(entity, agentComponentData, a, lt,
                       animStates[i], agentComponentData.unit_id);
                etLst.Add(xx.GetInstanceID(), xx);
                WorldUnitManager.Instance.Set(xx);
                xx.Play(ref entityManager, AnimationIds1001.run.GetHashCode());
            }
            else if (agentComponentData.state == 1)
            {
                if (etLst.TryGetValue(agentComponentData.global_id, out var aiEntity))
                {
                    if (aiEntity.attackCooldownTimer > 0)
                        aiEntity.attackCooldownTimer -= deltaTime;

                    float d = 1.5f;
                    if (aiEntity.atk_type == 1 && _enemyBulletConfigs.TryGetValue(aiEntity.unit_id, out var bCfg))
                        d = bCfg.attackRange;

                    if (!aiEntity.attacking && aiEntity.attackCooldownTimer <= 0
                        && math.distancesq(playerPos, aiEntity.localTransform.Position) <= d * d)
                    {
                        aiEntity.attacking = true;
                        aiEntity.hasFiredThisAttack = false;

                        aiEntity.localTransform.Rotation = Quaternion.LookRotation(playerPos - aiEntity.localTransform.Position);
                        transforms[i] = aiEntity.localTransform;
                        transformsDirty = true;

                        aiEntity.Play(ref entityManager, AnimationIds1001.attack.GetHashCode());
                        aiEntity.agent.maxSpeed = 0;
                    }
                    else if (aiEntity.attacking)
                    {
                        var animState = animStates[i]; // 直接从批量数组读，无 GetComponentData

                        if (aiEntity.atk_type == 1 && !aiEntity.hasFiredThisAttack
                            && animState.currentNormalizedTime > 0.4f
                            && _enemyBulletConfigs.TryGetValue(aiEntity.unit_id, out var fireCfg))
                        {
                            aiEntity.hasFiredThisAttack = true;
                            float3 dir = math.normalizesafe(playerPos - aiEntity.localTransform.Position);
                            dir.y = 0;
                            EnemyBulletSpawner.Fire(entityManager, _bulletPrefabEntity,
                                aiEntity.localTransform.Position + new float3(0, 1f, 0),
                                dir, fireCfg);
                        }

                        if (aiEntity.atk_type == 0 && !aiEntity.hasFiredThisAttack
                            && animState.currentNormalizedTime > 0.4f
                            && _playerController != null
                            && math.distancesq(playerPos, aiEntity.localTransform.Position) <= 1.5f * 1.5f)
                        {
                            aiEntity.hasFiredThisAttack = true;
                            _playerController.TakeDamage(MeleeDamage);
                        }

                        if (animState.currentNormalizedTime > 0.95f)
                        {
                            if (aiEntity.atk_type == 1 && _enemyBulletConfigs.TryGetValue(aiEntity.unit_id, out var cdCfg))
                                aiEntity.attackCooldownTimer = cdCfg.attackCooldown;

                            aiEntity.agent.maxSpeed = 1.75f;
                            aiEntity.attacking = false;
                            aiEntity.Play(ref entityManager, AnimationIds1001.run.GetHashCode());
                        }
                    }
                }
            }
        }

        // 只在有修改时才批量写回，替代循环内逐 entity SetComponentData
        if (agentsDirty)     _agentQuery.CopyFromComponentDataArray(agents);
        if (transformsDirty) _agentQuery.CopyFromComponentDataArray(transforms);

        agents.Dispose();
        transforms.Dispose();
        animStates.Dispose();
        entities.Dispose();

        _simulation.orca.TryComplete();
        _simulation.orca.Schedule(SystemAPI.Time.DeltaTime);

        if (etLst.Count > 0)
        {
            int activeCount    = etLst.Count;
            var aiArray        = new AIEntity[activeCount];
            var agentPosList   = new NativeArray<float3>(activeCount, Allocator.TempJob);
            var selfPosList    = new NativeArray<float3>(activeCount, Allocator.TempJob);
            var speedList      = new NativeArray<float>(activeCount, Allocator.TempJob);
            var unitIdList     = new NativeArray<int>(activeCount, Allocator.TempJob);
            var rotUpdates     = new NativeArray<byte>(activeCount, Allocator.TempJob);
            var rotations      = new NativeArray<quaternion>(activeCount, Allocator.TempJob);
            var prefVelocities = new NativeArray<float3>(activeCount, Allocator.TempJob);
            var newPositions   = new NativeArray<float3>(activeCount, Allocator.TempJob);

            int idx = 0;
            foreach (var kv in etLst)
            {
                var ai          = kv.Value;
                aiArray[idx]    = ai;
                agentPosList[idx] = ai.agent.pos;
                selfPosList[idx]  = ai.localTransform.Position;
                speedList[idx]    = ai.agent.maxSpeed;
                unitIdList[idx]   = ai.agentComponent.unit_id;
                idx++;
            }

            new EnemyMoveJob
            {
                playerPos      = playerPos,
                agentPositions = agentPosList,
                selfPositions  = selfPosList,
                speeds         = speedList,
                unitIds        = unitIdList,
                deltaTime      = deltaTime,
                rotUpdates     = rotUpdates,
                rotations      = rotations,
                prefVelocities = prefVelocities,
                newPositions   = newPositions,
            }.Schedule(activeCount, 64, default).Complete();

            for (int i = 0; i < activeCount; i++)
            {
                var ai = aiArray[i];
                if (ai.agentComponent.state != 1) continue;

                if (ai.agentComponent.unit_id == 1002)
                {
                    if (rotUpdates[i] == 1)
                        ai.localTransform.Rotation = rotations[i];
                    ai.localTransform.Position = newPositions[i];
                    ai.agent.prefVelocity      = prefVelocities[i];
                    ecb.SetComponent(ai.entity, ai.localTransform);
                }
                else if (ai.agentComponent.unit_id == 1003 && !ai.attacking)
                {
                    if (rotUpdates[i] == 1)
                        ai.localTransform.Rotation = rotations[i];
                    ai.localTransform.Position = newPositions[i];
                    ecb.SetComponent(ai.entity, ai.localTransform);
                }

                ai.UpdateGrild();
            }

            agentPosList.Dispose();
            selfPosList.Dispose();
            speedList.Dispose();
            unitIdList.Dispose();
            rotUpdates.Dispose();
            rotations.Dispose();
            prefVelocities.Dispose();
            newPositions.Dispose();
        }

    }
}

public class AIEntity
{
    public Entity entity;//通过这个实体获取组件
    public AgentComponent agentComponent;//记录了单位的一些信息
    public Agent agent;//确认 下一步移动到哪里 (动态避障)

    public LocalTransform localTransform;

    //GPU Animation
    public GpuEcsAnimatorControlComponent gpuEcsAnimatorControlComponent;
    public GpuEcsAnimatorControlStateComponent gpuEcsAnimatorControlStateComponent;
    public GpuEcsAnimatorStateComponent stateComponent;

    public int unit_id;//单位ID
    public int atk_type;//0近战 1远程

    public bool attacking; //是否正在攻击
    public float attackCooldownTimer; // 距下次可攻击的剩余时间
    public bool hasFiredThisAttack;   // 本次攻击是否已发射子弹

    public WorldUnit grildInfo;//表示当前处于地图哪个格子

    public int GetUnitId()
    {
        return agentComponent.unit_id;
    }


    public AIEntity(Entity e, AgentComponent ac, Agent a, LocalTransform localTransform,
        GpuEcsAnimatorStateComponent stateComponent, int unit_id)
    {
        this.entity = e;
        this.agentComponent = ac;
        this.agent = a;
        this.localTransform = localTransform;
        this.unit_id = unit_id;
        atk_type = Game.Config.UnitData.Get(unit_id)?.atk_type ?? 0;

        this.gpuEcsAnimatorControlComponent = new GpuEcsAnimatorControlComponent()
        {
            animatorInfo = new AnimatorInfo()
            {
                animationID = AnimationIds1001.run.GetHashCode(),
                blendFactor = 0,
                speedFactor = 1
            },
            startNormalizedTime = 0,
            transitionSpeed = 0
        };

        this.gpuEcsAnimatorControlStateComponent = new GpuEcsAnimatorControlStateComponent()
        {
            state = GpuEcsAnimatorControlStates.Start,
            reset = true
        };

        this.stateComponent = stateComponent;
    }

    //获取状态
    public int GetState()
    {
        return agentComponent.state;
    }

    //获取唯一ID
    public int GetInstanceID()
    {
        return agentComponent.global_id;
    }

    public bool IsActive()
    {
        return GetState() != -1;
    }

    // 移动 更新位置..todo
    //地图管理器.. 便于快速寻找指定区域的单位
    public void UpdateGrild()
    {
        WorldUnitManager.Instance.Change(this);
    }

    //播放动作
    public void Play(ref EntityManager entityManager, int id)
    {
        gpuEcsAnimatorControlComponent.animatorInfo.animationID = id;// AnimationIdsAnmState1001.attack.GetHashCode();
        gpuEcsAnimatorControlComponent.animatorInfo.blendFactor = 0;
        gpuEcsAnimatorControlComponent.animatorInfo.speedFactor = 1;

        gpuEcsAnimatorControlComponent.startNormalizedTime = 0;
        gpuEcsAnimatorControlComponent.transitionSpeed = 0;

        gpuEcsAnimatorControlStateComponent.state = GpuEcsAnimatorControlStates.Start;
        gpuEcsAnimatorControlStateComponent.reset = true;

        //Debug.Log($"[Play] animationID={gpuEcsAnimatorControlComponent.animatorInfo.animationID} entity={e.Index} hasControl={entityManager.HasComponent<GpuEcsAnimatorControlComponent>(e)}");
        entityManager.SetComponentData(entity, gpuEcsAnimatorControlComponent);
        entityManager.SetComponentData(entity, gpuEcsAnimatorControlStateComponent);
    }


    internal Vector3 GetPosition()
    {
        return localTransform.Position;
    }

    public void Dispose()
    {
        WorldUnitManager.Instance.Remove(this);
    }
}