using Unity.Entities;
using UnityEngine;

public class EnemySpawnerAuthoring : MonoBehaviour
{
    public GameObject enemy;
    public GameObject boss;

    public float spawnRate;
}

public class EnemySpawnerBaker : Baker<EnemySpawnerAuthoring>
{
    public override void Bake(EnemySpawnerAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.None);

        PrefabEntity prefabEntity1 = new PrefabEntity()
        {

            id = int.Parse(authoring.enemy.name),
            entity = GetEntity(authoring.enemy, TransformUsageFlags.Dynamic)
        };
        PrefabEntity prefabEntity2 = new PrefabEntity()
        {
            id = int.Parse(authoring.boss.name),
            entity = GetEntity(authoring.boss, TransformUsageFlags.Dynamic)
        };
   
        AddComponent(entity, new EnemySpawnerComponent
        {
            enemyPrefab = prefabEntity1,
            bossPrefab = prefabEntity2,
            spawnRate = authoring.spawnRate,
        });
    }
}

public struct EnemySpawnerComponent : IComponentData
{
    public PrefabEntity enemyPrefab;
    public PrefabEntity bossPrefab;
    public float spawnRate;
}

public struct PrefabEntity
{
    public int id;
    public Entity entity;
}
