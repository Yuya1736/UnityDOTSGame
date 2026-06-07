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
    private GameObject _player;
    private PlayerController _playerController;

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

        if (_player == null)
        {
            _player = GameObject.FindWithTag("Player");
            if (_player != null)
                _playerController = _player.GetComponent<PlayerController>();
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
            int w = WorldUnitManager.Instance.width;
            int currentUnitId = ((int)math.floor(lt.Position.z / w) << 16) | ((int)math.floor(lt.Position.x / w) & 0xFFFF);


            if (currentUnitId != bullet.lastWorldUnitId)
            {
                bullet.lastWorldUnitId = currentUnitId;

                float range = bullet.hitRange;
                float3 pos = lt.Position + bullet.collisionOffset;
                int damage = bullet.damage;
                int hitEffectIndex = bullet.hitEffectIndex;

                if (bullet.ownerType == 1) // 敌人子弹 → 检测玩家
                {
                    if (_player != null && _playerController != null)
                    {
                        float3 playerPos = (float3)_player.transform.position;
                        float distSq = math.distancesq(pos, playerPos);
                        if (distSq <= range * range)
                        {
                            if (hitEffectIndex >= 0 && hitEffectIndex < BulletSpawner.HitEffects.Count)
                                EffectPool.Instance.Spawn(BulletSpawner.HitEffects[hitEffectIndex],
                                    (Vector3)playerPos, Quaternion.identity, 2f);

                            _playerController.TakeDamage(damage);
                            ecb.DestroyEntity(entity);
                            continue;
                        }
                    }
                }
                else // 玩家子弹 → 现有逻辑
                {
                    bool isExplosion = bullet.bulletType == BulletType.Explosive;
                    bool hitAny = false;

                    if (isExplosion)
                    {
                        WorldUnitManager.Instance.OnHit(
                            pos, range, entityManager, _enemySystem,
                            (em, es, aiEntity, bPos, r, _) =>
                            {
                                float distSq = math.distancesq(bPos, aiEntity.localTransform.Position);
                                if (distSq > r * r) return;

                                aiEntity.attacking = false;
                                hitAny = true;

                                if (hitEffectIndex >= 0 && hitEffectIndex < BulletSpawner.HitEffects.Count)
                                    EffectPool.Instance.Spawn(BulletSpawner.HitEffects[hitEffectIndex],
                                        (Vector3)aiEntity.localTransform.Position, Quaternion.identity, 2f);

                                aiEntity.agentComponent.hp -= damage;
                                if (aiEntity.agentComponent.hp <= 0)
                                {
                                    aiEntity.agentComponent.triggerDie = true;
                                    em.SetComponentData(aiEntity.entity, aiEntity.agentComponent);
                                }
                            },
                            0, false
                    );

                    if (hitAny)
                    {
                        int exIdx = bullet.explosionEffectIndex;
                        if (exIdx >= 0 && exIdx < BulletSpawner.HitEffects.Count)
                            EffectPool.Instance.Spawn(BulletSpawner.HitEffects[exIdx],
                                (Vector3)pos, Quaternion.identity, 3f);

                        ecb.DestroyEntity(entity);
                        continue;
                    }
                }
                else
                {
                    var nearest = WorldUnitManager.Instance.FindNearest(pos, range);
                    if (nearest != null)
                    {
                        nearest.attacking = false;

                        if (hitEffectIndex >= 0 && hitEffectIndex < BulletSpawner.HitEffects.Count)
                            EffectPool.Instance.Spawn(BulletSpawner.HitEffects[hitEffectIndex],
                                (Vector3)nearest.localTransform.Position, Quaternion.identity, 2f);

                        nearest.agentComponent.hp -= damage;
                        if (nearest.agentComponent.hp <= 0)
                        {
                            nearest.agentComponent.triggerDie = true;
                            entityManager.SetComponentData(nearest.entity, nearest.agentComponent);
                        }

                        ecb.DestroyEntity(entity);
                        continue;
                    }
                }
                } // end else (player bullet)
            } // end if (currentUnitId != bullet.lastWorldUnitId)

            entityManager.SetComponentData(entity, bullet);
            entityManager.SetComponentData(entity, lt);
        }

        ecb.Playback(entityManager);
        ecb.Dispose();
        entities.Dispose();
    }
}
