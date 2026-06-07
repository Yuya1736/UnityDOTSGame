using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public static class EnemyBulletSpawner
{
    public static void Fire(EntityManager em, Entity bulletPrefab,
        float3 muzzlePos, float3 targetDir, EnemyBulletConfig config)
    {
        if (bulletPrefab == Entity.Null || config == null) return;

        int hitEffectIdx = -1;
        if (config.hitEffect != null)
        {
            hitEffectIdx = BulletSpawner.HitEffects.IndexOf(config.hitEffect);
            if (hitEffectIdx < 0)
            {
                hitEffectIdx = BulletSpawner.HitEffects.Count;
                BulletSpawner.HitEffects.Add(config.hitEffect);
            }
        }

        int count = Mathf.Max(1, config.bulletCount);
        float spread = config.spreadAngle;

        for (int i = 0; i < count; i++)
        {
            float3 dir = ComputeDirection(targetDir, i, count, spread);

            var e = em.Instantiate(bulletPrefab);
            em.SetComponentData(e, LocalTransform.FromPosition(muzzlePos));
            em.AddComponentData(e, new BulletComponent
            {
                direction = dir,
                speed = config.bulletSpeed,
                remainingLife = config.bulletLifetime,
                bulletType = config.bulletType,
                hitRange = config.hitRadius,
                lastWorldUnitId = -1,
                damage = config.damage,
                hitEffectIndex = hitEffectIdx,
                explosionEffectIndex = -1,
                collisionOffset = (float3)config.collisionOffset,
                ownerType = 1,
            });

            if (config.bulletVisualPrefab != null)
            {
                var visual = EffectPool.Instance.Spawn(
                    config.bulletVisualPrefab,
                    (Vector3)muzzlePos,
                    Quaternion.LookRotation((Vector3)dir));
                var tracker = visual.GetComponent<BulletVisualTracker>()
                    ?? visual.AddComponent<BulletVisualTracker>();
                tracker.Init(e, config.bulletVisualPrefab.name);
            }
        }
    }

    static float3 ComputeDirection(float3 forward, int index, int total, float totalAngle)
    {
        if (total <= 1 || totalAngle <= 0f)
            return math.normalizesafe(forward);

        float step = totalAngle / (total - 1);
        float angle = -totalAngle / 2f + step * index;
        quaternion rot = quaternion.RotateY(math.radians(angle));
        return math.normalizesafe(math.mul(rot, forward));
    }
}
