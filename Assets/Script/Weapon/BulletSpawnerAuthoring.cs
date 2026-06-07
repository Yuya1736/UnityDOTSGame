using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class BulletSpawnerAuthoring : MonoBehaviour
{
    public GameObject bulletPrefab;
}

public class BulletSpawnerBaker : Baker<BulletSpawnerAuthoring>
{
    public override void Bake(BulletSpawnerAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.None);
        AddComponent(entity, new BulletSpawnerComponent
        {
            bulletPrefab = GetEntity(authoring.bulletPrefab, TransformUsageFlags.Dynamic)
        });
    }
}

public struct BulletSpawnerComponent : IComponentData
{
    public Entity bulletPrefab;
}

public struct BulletComponent : IComponentData
{
    public float3 direction;
    public float speed;
    public float remainingLife;
    public BulletType bulletType;
    public float hitRange;
    public int lastWorldUnitId;
    public int damage;
    public int hitEffectIndex;       // index into BulletSpawner.HitEffects
    public int explosionEffectIndex; // index into BulletSpawner.HitEffects, -1 if none
    public float3 collisionOffset;
    public byte ownerType;           // 0 = Player, 1 = Enemy
}
