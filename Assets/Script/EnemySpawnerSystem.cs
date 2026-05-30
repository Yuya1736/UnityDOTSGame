using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public partial struct EnemySpawnerSystem : ISystem
{
    private bool init;

    public Entity spawnerEntity;
    public PrefabEntity enemy;
    public PrefabEntity boss;
    public float spawnRate;
    public float3 spawnPos;
    public float nextSpawnTime;

    public void OnUpdate(ref SystemState state)
    {
        if (!init)
        {
            if(!SystemAPI.TryGetSingletonEntity<EnemySpawnerComponent>(out spawnerEntity))
            {
                return;
            }

            var spawner = SystemAPI.GetComponentRW<EnemySpawnerComponent>(spawnerEntity);
            enemy = spawner.ValueRO.enemyPrefab;
            boss = spawner.ValueRO.bossPrefab;
            spawnRate = spawner.ValueRO.spawnRate;
            spawnPos = spawner.ValueRO.spawnPos;
            nextSpawnTime = 0;
            init = true;
        }    
        else
        {
            
        }
    }
}
