using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class BulletSpawner : MonoBehaviour
{
    public static readonly List<GameObject> HitEffects = new List<GameObject>();

    [Header("子弹配置列表（道具拾取后可随机切换）")]
    public List<BulletConfig> configs = new List<BulletConfig>();

    [Header("当前激活的配置索引")]
    public int activeIndex = 0;

    [Header("枪口挂点")]
    public Transform muzzlePoint;

    [Header("子弹拖尾视觉预制体（如 shoot_projectile）")]
    public GameObject bulletVisualPrefab;

    private EntityManager _entityManager;
    private Entity _bulletPrefabEntity;
    private bool _initialized;

    void Start()
    {
        _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    }

    void EnsureInit()
    {
        if (_initialized) return;

        var query = _entityManager.CreateEntityQuery(typeof(BulletSpawnerComponent));
        if (query.CalculateEntityCount() > 0)
        {
            var arr = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            var comp = _entityManager.GetComponentData<BulletSpawnerComponent>(arr[0]);
            _bulletPrefabEntity = comp.bulletPrefab;
            arr.Dispose();
            _initialized = true;
        }
        query.Dispose();
    }

    public void Fire(float3 targetDir)
    {
        EnsureInit();
        if (!_initialized || configs == null || configs.Count == 0) return;

        var config = configs[activeIndex];
        if (config == null) return;

        // 注册 hitEffect 到静态列表，拿到 index
        int hitEffectIdx = -1;
        if (config.hitEffect != null)
        {
            hitEffectIdx = HitEffects.IndexOf(config.hitEffect);
            if (hitEffectIdx < 0)
            {
                hitEffectIdx = HitEffects.Count;
                HitEffects.Add(config.hitEffect);
            }
        }

        float3 muzzlePos = muzzlePoint != null ? (float3)muzzlePoint.position : (float3)transform.position;
        int count = Mathf.Max(1, config.bulletCount);
        float spread = config.spreadAngle;

        for (int i = 0; i < count; i++)
        {
            float3 dir = ComputeDirection(targetDir, i, count, spread);

            // 直接 Instantiate 拿到真实 entity，才能传给 Tracker
            var e = _entityManager.Instantiate(_bulletPrefabEntity);
            _entityManager.SetComponentData(e, LocalTransform.FromPosition(muzzlePos));
            _entityManager.AddComponentData(e, new BulletComponent
            {
                direction = dir,
                speed = config.bulletSpeed,
                remainingLife = config.bulletLifetime,
                bulletType = config.bulletType,
                hitRange = config.bulletType == BulletType.Explosive ? config.explosionRadius : config.hitRadius,
                lastWorldUnitId = -1,
                damage = config.damage,
                hitEffectIndex = hitEffectIdx,
            });

            // 生成拖尾视觉并绑定
            if (bulletVisualPrefab != null)
            {
                var visual = Object.Instantiate(bulletVisualPrefab, (Vector3)muzzlePos, Quaternion.LookRotation((Vector3)dir));
                var tracker = visual.AddComponent<BulletVisualTracker>();
                tracker.Init(e);
            }
        }

        // 枪口特效已移除（hitEffect 用于命中，不在此处播放）
    }

    float3 ComputeDirection(float3 forward, int index, int total, float totalAngle)
    {
        if (total <= 1 || totalAngle <= 0f)
            return math.normalizesafe(forward);

        // 均匀分布在 [-totalAngle/2, totalAngle/2]
        float step = totalAngle / (total - 1);
        float angle = -totalAngle / 2f + step * index;

        // 绕 Y 轴偏转
        quaternion rot = quaternion.RotateY(math.radians(angle));
        return math.normalizesafe(math.mul(rot, forward));
    }
}
