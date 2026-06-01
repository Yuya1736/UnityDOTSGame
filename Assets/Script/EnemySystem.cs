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
    private GameObject player;
    private ORCABundle<Agent> m_simulation;
    public Dictionary<int, AIEntity> etLst = new Dictionary<int, AIEntity>(10000);

    protected override void OnUpdate()
    {

        if (player == null)
        {
            player = GameObject.FindWithTag("Player");
            m_simulation = new ORCABundle<Agent>();
            m_simulation.plane = AxisPair.XZ;
            if (player == null)
            {
                Debug.LogError("Player GameObject not found in the scene.");
                return;
            }
        }

        float3 playerPos = player.transform.position;
        float deltaTime = SystemAPI.Time.DeltaTime;

        var entityManager = World.EntityManager;
        var entities = entityManager.GetAllEntities(Unity.Collections.Allocator.Temp);

        foreach (var entity in entities)
        {
            if (entityManager.HasComponent<AgentComponent>(entity))
            {
                var agentComponentData = entityManager.GetComponentData<AgentComponent>(entity);
                if (agentComponentData.state == 0)
                {
                    var lt = entityManager.GetComponentData<LocalTransform>(entity);

                    float f1 = UnityEngine.Random.Range(-25, 25);
                    float f2 = UnityEngine.Random.Range(-25, 25);

                    lt.Position = playerPos + new float3(f1, 0, f2);

                    agentComponentData.state = 1;
                    entityManager.SetComponentData(entity, lt);
                    entityManager.SetComponentData(entity, agentComponentData);

                    var a = m_simulation.NewAgent(lt.Position);
                    a.id = agentComponentData.global_id;
                    a.radius = 0.35f;
                    a.radiusObst = 0.35f;
                    a.maxSpeed = 1.75f;
                    a.prefVelocity = math.normalize(playerPos - a.pos) * 1.75f;
                    a.velocity = a.prefVelocity;
                    a.timeHorizon = 0.001f;

                    //切换到移动的动作
                    var xx = new AIEntity(entity, agentComponentData, a, new NativeArray<byte>(1, Allocator.Persistent),
                           new NativeArray<quaternion>(1, Allocator.Persistent),
                           new NativeArray<float3>(1, Allocator.Persistent), lt,
                           entityManager.GetComponentData<GpuEcsAnimatorStateComponent>(entity), agentComponentData.unit_id);
                    etLst.Add(xx.GetInstanceID(), xx);
                    xx.Play(ref entityManager, AnimationIds1001.run.GetHashCode());

                }
                else if (agentComponentData.state == 1)
                {

                }
                else if (agentComponentData.state == 2)
                {

                }
            }
        }

        entities.Dispose();
    }
}

public class AIEntity
{
    public Entity e;//通过这个实体获取组件
    public AgentComponent ac;//记录了单位的一些信息
    public Agent a;//确认 下一步移动到哪里 (动态避障)

    //jobsystem 计算移动
    public NativeArray<byte> _rot_update;
    public NativeArray<quaternion> _rot;
    public NativeArray<float3> _prefVelocity;

    public LocalTransform ls;

    //GPU Animation
    public GpuEcsAnimatorControlComponent gpuEcsAnimatorControlComponent;
    public GpuEcsAnimatorControlStateComponent gpuEcsAnimatorControlStateComponent;
    public GpuEcsAnimatorStateComponent stateComponent;

    JobHandle _jobHandle;

    public int unit_id;//单位ID
    public int atk_type;//0近战 1远程

    public bool send_bullet;//是否已发射子弹(攻击)

    //public WorldUnit grildInfo;//表示当前处于地图哪个格子

    public int GetUnitId()
    {
        return ac.unit_id;
    }


    public AIEntity(Entity e, AgentComponent ac, Agent a, NativeArray<byte> rot_update,
        NativeArray<quaternion> rot, NativeArray<float3> prefVelocity, LocalTransform localTransform,
        GpuEcsAnimatorStateComponent stateComponent, int unit_id)
    {
        this.e = e;
        this.ac = ac;
        this.a = a;
        _rot_update = rot_update;
        _rot = rot;
        _prefVelocity = prefVelocity;
        this.ls = localTransform;
        this.unit_id = unit_id;
        //atk_type = UnitData.Get(unit_id).atk_type;

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
        return ac.state;
    }

    //获取唯一ID
    public int GetInstanceID()
    {
        return ac.global_id;
    }

    public bool IsActive()
    {
        return GetState() != -1;
    }

    //移动 更新位置  ..todo
    //public void DOUpdatePositionJob(float3 p_pos, EntityCommandBuffer ecb)
    //{
    //    if (GetState() == 1)
    //    {
    //        if (ac.unit_id == 1002)
    //        {
    //            EnemyJob enemyJob = new EnemyJob(p_pos, a.pos, ls.Position, 1.75f, _rot_update, _rot, _prefVelocity);
    //            _jobHandle = enemyJob.Schedule();
    //        }
    //        else if (ac.unit_id == 1003)
    //        {
    //            var r1 = p_pos - ls.Position;
    //            ls.Rotation = Unity.Mathematics.quaternion.LookRotation(r1, Vector3.up);
    //            ls.Position = ls.Position + ls.Forward() * Time.deltaTime * a.maxSpeed;
    //            ecb.SetComponent(e, ls);
    //        }

    //    }
    //}

    //public void DOUpdatePosition(EntityCommandBuffer ecb)
    //{
    //    if (GetState() == 1)
    //    {
    //        if (ac.unit_id == 1002)
    //        {
    //            _jobHandle.Complete();
    //            if (_rot_update[0] == 1)
    //            {
    //                ls.Rotation = _rot[0];
    //            }
    //            ls.Position = a.pos;
    //            ecb.SetComponent(e, ls);
    //            a.prefVelocity = _prefVelocity[0];
    //        }
    //        UpdateGrild();
    //    }
    //}

    ////地图管理器.. 便于快速寻找指定区域的单位
    //public void UpdateGrild()
    //{
    //    WorldUnitManager.Instance.Change(this);
    //}

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
        entityManager.SetComponentData(e, gpuEcsAnimatorControlComponent);
        entityManager.SetComponentData(e, gpuEcsAnimatorControlStateComponent);
    }


    internal Vector3 GetPosition()
    {
        return ls.Position;
    }

    public void Dispose()
    {
        _rot_update.Dispose();
        _rot.Dispose();
        _prefVelocity.Dispose();

        //WorldUnitManager.Instance.Remove(this);
    }
}