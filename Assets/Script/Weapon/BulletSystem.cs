using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(EnemySystem))]
public partial class BulletSystem : SystemBase
{
    private EntityQuery _bulletQuery;
    private EnemySystem _enemySystem;

    protected override void OnCreate()
    {
        _bulletQuery = GetEntityQuery(
            ComponentType.ReadWrite<BulletComponent>(),
            ComponentType.ReadWrite<LocalTransform>()
        );
    }

    protected override void OnUpdate()
    {
        if (_enemySystem == null)
        {
            _enemySystem = World.GetExistingSystemManaged<EnemySystem>();
            if (_enemySystem == null) return;
        }

        float dt = SystemAPI.Time.DeltaTime;
        var entityManager = World.EntityManager;
        var entities = _bulletQuery.ToEntityArray(Allocator.Temp);
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var entity in entities)
        {
            var bullet = entityManager.GetComponentData<BulletComponent>(entity);
            var lt = entityManager.GetComponentData<LocalTransform>(entity);

            // 生命周期倒计时
            bullet.remainingLife -= dt;
            if (bullet.remainingLife <= 0f)
            {
                ecb.DestroyEntity(entity);
                continue;
            }

            // 移动
            lt.Position += bullet.direction * bullet.speed * dt;

            // WorldUnit 变化时才做碰撞检测（减少不必要的查询）
            var currentUnit = WorldUnitManager.Instance.HasGrild(lt.Position);
            int currentUnitId = currentUnit != null ? currentUnit.id : -1;

            if (currentUnitId != bullet.lastWorldUnitId)
            {
                bullet.lastWorldUnitId = currentUnitId;

                if (currentUnit != null)
                {
                    bool isExplosion = bullet.bulletType == BulletType.Explosive;
                    float range = bullet.hitRange;
                    float3 pos = lt.Position;
                    Entity bulletEntity = entity;
                    bool hitAny = false;
                    int damage = bullet.damage;

                    int hitEffectIndex = bullet.hitEffectIndex;

                    WorldUnitManager.Instance.OnHit(
                        pos, range, entityManager, _enemySystem,
                        (em, es, aiEntity, bPos, r, _) =>
                        {
                            float distSq = math.distancesq(bPos, aiEntity.localTransform.Position);
                            if (distSq > r * r) return;

                            aiEntity.attacking = false;
                            hitAny = true;

                            // 命中特效
                            if (hitEffectIndex >= 0 && hitEffectIndex < BulletSpawner.HitEffects.Count)
                            {
                                var fx = Object.Instantiate(BulletSpawner.HitEffects[hitEffectIndex],
                                    (Vector3)aiEntity.localTransform.Position, Quaternion.identity);
                                Object.Destroy(fx, 2f);
                            }

                            aiEntity.agentComponent.hp -= damage;
                            if (aiEntity.agentComponent.hp <= 0)
                            {
                                aiEntity.agentComponent.triggerDie = true;
                                em.SetComponentData(aiEntity.entity, aiEntity.agentComponent);
                            }

                            if (!isExplosion)
                                ecb.DestroyEntity(bulletEntity);
                        },
                        0,
                        !isExplosion
                    );

                    // 爆炸子弹命中任意敌人后销毁
                    if (isExplosion && hitAny)
                    {
                        ecb.DestroyEntity(entity);
                        continue;
                    }
                }
            }

            entityManager.SetComponentData(entity, bullet);
            entityManager.SetComponentData(entity, lt);
        }

        ecb.Playback(entityManager);
        ecb.Dispose();
        entities.Dispose();
    }
}
