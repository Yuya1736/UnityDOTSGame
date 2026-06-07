using System.Collections.Generic;
using JKFrame;
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

    [Header("掉落道具预制体（QstTiltedNeon）")]
    public GameObject pickupPrefab;

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

        int explosionEffectIdx = -1;
        if (config.bulletType == BulletType.Explosive && config.explosionEffect != null)
        {
            explosionEffectIdx = HitEffects.IndexOf(config.explosionEffect);
            if (explosionEffectIdx < 0)
            {
                explosionEffectIdx = HitEffects.Count;
                HitEffects.Add(config.explosionEffect);
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
                explosionEffectIndex = explosionEffectIdx,
                collisionOffset = (float3)config.collisionOffset,
            });

            // 生成拖尾视觉并绑定
            if (bulletVisualPrefab != null)
            {
                var visual = EffectPool.Instance.Spawn(bulletVisualPrefab, (Vector3)muzzlePos, Quaternion.LookRotation((Vector3)dir));
                var tracker = visual.GetComponent<BulletVisualTracker>() ?? visual.AddComponent<BulletVisualTracker>();
                tracker.Init(e, bulletVisualPrefab.name);
            }
        }

        // 枪口特效已移除（hitEffect 用于命中，不在此处播放）
    }

    // 随机切换到一个不同的子弹配置（拾取道具时调用）
    public void SwitchRandomConfig()
    {
        if (configs == null || configs.Count <= 1) return;
        int next = activeIndex;
        while (next == activeIndex)
            next = UnityEngine.Random.Range(0, configs.Count);
        activeIndex = next;
        Debug.Log($"[BulletSpawner] 切换到配置 {activeIndex}: {configs[activeIndex]?.name}");
    }

    // 在指定位置生成掉落物（使用 JKFrame 对象池）
    public void SpawnPickup(Vector3 position)
    {
        if (pickupPrefab == null) return;

        var go = PoolSystem.GetGameObject(pickupPrefab.name);
        if (go == null)
        {
            go = Object.Instantiate(pickupPrefab);
            go.name = pickupPrefab.name;
        }

        var item = go.GetComponent<PickupItem>() ?? go.AddComponent<PickupItem>();
        go.transform.SetPositionAndRotation(position + Vector3.up * 0.5f, Quaternion.identity);
        go.SetActive(true);
        item.ResetState();
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
