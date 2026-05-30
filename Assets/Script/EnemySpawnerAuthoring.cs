using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class EnemySpawnerAuthoring : MonoBehaviour
{
    public GameObject enemyPrefab;
    public GameObject bossPrefab;

    public float spawnRate;
}

public class EnemySpawnerBaker : Baker<EnemySpawnerAuthoring>
{
    public override void Bake(EnemySpawnerAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.None);

        PrefabEntity prefabEntity1 = new PrefabEntity()
        {
            id = int.Parse(authoring.enemyPrefab.name),
            entity = GetEntity(authoring.enemyPrefab, TransformUsageFlags.Dynamic)
        };
        PrefabEntity prefabEntity2 = new PrefabEntity()
        {
            id = int.Parse(authoring.bossPrefab.name),
            entity = GetEntity(authoring.bossPrefab, TransformUsageFlags.Dynamic)
        };
   
        AddComponent(entity, new EnemySpawnerComponent
        {
            enemyPrefab = prefabEntity1,
            bossPrefab = prefabEntity2,
            spawnRate = authoring.spawnRate,
            spawnPos = authoring.transform.position
        });
    }
}

public struct EnemySpawnerComponent : IComponentData
{
    public PrefabEntity enemyPrefab;
    public PrefabEntity bossPrefab;
    public float spawnRate;

    public float3 spawnPos;
}

public struct PrefabEntity
{
    public int id;
    public Entity entity;
}
