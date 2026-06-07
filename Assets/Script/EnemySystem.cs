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
    private ORCABundle<Agent> _simulation;
    public Dictionary<int, AIEntity> etLst = new Dictionary<int, AIEntity>(10000);

    private EntityQuery _agentQuery;
    private BulletSpawner _bulletSpawner;
    private Dictionary<int, EnemyBulletConfig> _enemyBulletConfigs;
    private Entity _bulletPrefabEntity;

    protected override void OnCreate()
    {
        _agentQuery = GetEntityQuery(
            ComponentType.ReadWrite<AgentComponent>(),
            ComponentType.ReadWrite<LocalTransform>(),
            ComponentType.ReadOnly<GpuEcsAnimatorStateComponent>()
        );
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

            // 加载怪物子弹配置
            _enemyBulletConfigs = new Dictionary<int, EnemyBulletConfig>();
            var cfg1003 = Resources.Load<EnemyBulletConfig>("Config/EnemyBulletConfig1003");
            if (cfg1003 != null) _enemyBulletConfigs[cfg1003.unitId] = cfg1003;

            // 获取子弹预制体 Entity（与 BulletSpawner 共用同一个）
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
        var entities = _agentQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
        EntityCommandBuffer ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        foreach (Entity entity in entities)
        {
            AgentComponent agentComponentData = entityManager.GetComponentData<AgentComponent>(entity);
            if (agentComponentData.triggerDie)
            {
                agentComponentData.triggerDie = false;
                agentComponentData.state = 3;
                agentComponentData.dieTimer = 10f;
                entityManager.SetComponentData(entity, agentComponentData);

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
                    entityManager.SetComponentData(entity, agentComponentData);
                continue;
            }
            if (agentComponentData.state == 0)
            {
                var lt = entityManager.GetComponentData<LocalTransform>(entity);

                float f1 = UnityEngine.Random.Range(-25, 25);
                float f2 = UnityEngine.Random.Range(-25, 25);

                lt.Position = playerPos + new float3(f1, 0, f2);

                agentComponentData.state = 1;
                entityManager.SetComponentData(entity, lt);
                entityManager.SetComponentData(entity, agentComponentData);

                var a = _simulation.NewAgent(lt.Position);
                a.id = agentComponentData.global_id;
                a.radius = 0.35f;
                a.radiusObst = 0.35f;
                a.maxSpeed = 1.75f;
                a.prefVelocity = math.normalize(playerPos - a.pos) * 1.75f;
                a.velocity = a.prefVelocity;
                a.timeHorizon = 3f;

                //切换到移动的动作
                var xx = new AIEntity(entity, agentComponentData, a, new NativeArray<byte>(1, Allocator.Persistent),
                       new NativeArray<quaternion>(1, Allocator.Persistent),
                       new NativeArray<float3>(1, Allocator.Persistent), lt,
                       entityManager.GetComponentData<GpuEcsAnimatorStateComponent>(entity), agentComponentData.unit_id);
                etLst.Add(xx.GetInstanceID(), xx);
                WorldUnitManager.Instance.Set(xx);
                xx.Play(ref entityManager, AnimationIds1001.run.GetHashCode());
            }
            else if (agentComponentData.state == 1)
            {
                if (etLst.TryGetValue(agentComponentData.global_id, out var aiEntity))
                {
                    // 更新攻击冷却计时器
                    if (aiEntity.attackCooldownTimer > 0)
                        aiEntity.attackCooldownTimer -= deltaTime;

                    float d = 1.5f;
                    //判断与主角的距离
                    if (aiEntity.atk_type == 1 && _enemyBulletConfigs.TryGetValue(aiEntity.unit_id, out var bCfg))
                    {
                        d = bCfg.attackRange;
                    }

                    if (!aiEntity.attacking && aiEntity.attackCooldownTimer <= 0
                        && math.distancesq(playerPos, aiEntity.localTransform.Position) <= d * d)
                    {
                        aiEntity.attacking = true;
                        aiEntity.hasFiredThisAttack = false;
                        entityManager.SetComponentData(entity, agentComponentData);

                        aiEntity.localTransform.Rotation = Quaternion.LookRotation(playerPos - aiEntity.localTransform.Position);
                        entityManager.SetComponentData(entity, aiEntity.localTransform);

                        aiEntity.Play(ref entityManager, AnimationIds1001.attack.GetHashCode());
                        aiEntity.agent.maxSpeed = 0;
                    }
                    else if (aiEntity.attacking)
                    {
                        var animState = entityManager.GetComponentData<GpuEcsAnimatorStateComponent>(entity);

                        // 远程怪物：在动画中途发射子弹
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

                        // 动画结束，恢复移动
                        if (animState.currentNormalizedTime > 0.95f)
                        {
                            if (aiEntity.atk_type == 1 && _enemyBulletConfigs.TryGetValue(aiEntity.unit_id, out var cdCfg))
                                aiEntity.attackCooldownTimer = cdCfg.attackCooldown;

                            agentComponentData.state = 1;
                            aiEntity.agent.maxSpeed = 1.75f;
                            aiEntity.attacking = false;
                            entityManager.SetComponentData(entity, agentComponentData);
                            aiEntity.Play(ref entityManager, AnimationIds1001.run.GetHashCode());
                        }
                    }
                }
            }
            else if (agentComponentData.state == 2)
            {

            }
        }

        _simulation.orca.TryComplete();
        _simulation.orca.Schedule(SystemAPI.Time.DeltaTime);

        if (etLst.Count > 0)
        {
            foreach (var item in etLst)
            {
                //执行更新位置的job
                item.Value.DOUpdatePositionJob(playerPos, ecb);
            }
            foreach (var item in etLst)
            {
                item.Value.DOUpdatePosition(ecb);
            }
        }

        ecb.Playback(entityManager);
        entities.Dispose();
    }
}

public class AIEntity
{
    public Entity entity;//通过这个实体获取组件
    public AgentComponent agentComponent;//记录了单位的一些信息
    public Agent agent;//确认 下一步移动到哪里 (动态避障)

    //jobsystem 计算移动
    public NativeArray<byte> _rot_update;
    public NativeArray<quaternion> _rot;
    public NativeArray<float3> _prefVelocity;

    public LocalTransform localTransform;

    //GPU Animation
    public GpuEcsAnimatorControlComponent gpuEcsAnimatorControlComponent;
    public GpuEcsAnimatorControlStateComponent gpuEcsAnimatorControlStateComponent;
    public GpuEcsAnimatorStateComponent stateComponent;

    JobHandle _jobHandle;

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


    public AIEntity(Entity e, AgentComponent ac, Agent a, NativeArray<byte> rot_update,
        NativeArray<quaternion> rot, NativeArray<float3> prefVelocity, LocalTransform localTransform,
        GpuEcsAnimatorStateComponent stateComponent, int unit_id)
    {
        this.entity = e;
        this.agentComponent = ac;
        this.agent = a;
        _rot_update = rot_update;
        _rot = rot;
        _prefVelocity = prefVelocity;
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
    public void DOUpdatePositionJob(float3 p_pos, EntityCommandBuffer ecb)
    {
        if (GetState() == 1)
        {
            if (agentComponent.unit_id == 1002)
            {
                EnemyJob enemyJob = new EnemyJob(p_pos, agent.pos, localTransform.Position, 1.75f, _rot_update, _rot, _prefVelocity);
                _jobHandle = enemyJob.Schedule(_jobHandle);
            }
            else if (agentComponent.unit_id == 1003)
            {
                var r1 = p_pos - localTransform.Position;
                localTransform.Rotation = Unity.Mathematics.quaternion.LookRotation(r1, Vector3.up);
                localTransform.Position = localTransform.Position + localTransform.Forward() * Time.deltaTime * agent.maxSpeed;
                ecb.SetComponent(entity, localTransform);
                UpdateGrild();
            }

        }
    }

    public void DOUpdatePosition(EntityCommandBuffer ecb)
    {
        if (GetState() == 1)
        {
            if (agentComponent.unit_id == 1002)
            {
                _jobHandle.Complete();
                if (_rot_update[0] == 1)
                {
                    localTransform.Rotation = _rot[0];
                }
                localTransform.Position = agent.pos;
                ecb.SetComponent(entity, localTransform);
                agent.prefVelocity = _prefVelocity[0];
            }
            UpdateGrild();
        }
    }

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
        _rot_update.Dispose();
        _rot.Dispose();
        _prefVelocity.Dispose();

        WorldUnitManager.Instance.Remove(this);
    }
}