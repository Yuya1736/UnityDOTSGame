using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial struct EnemySpawnerSystem : ISystem
{
    private bool init;

    public Entity spawnerEntity;
    public PrefabEntity enemy;
    public PrefabEntity boss;
    public float spawnRate;
    public float nextSpawnTime_enemy;
    public float nextSpawnTime_boss;
    private int globalIdCounter;

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
            nextSpawnTime_enemy = 0;
            nextSpawnTime_boss = 10;
            globalIdCounter = 0;
            init = true;
        }
        else
        {
            float elapsedTime = (float)SystemAPI.Time.ElapsedTime;
            bool spawnEnemy = elapsedTime >= nextSpawnTime_enemy;
            bool spawnBoss = elapsedTime >= nextSpawnTime_boss;

            if (spawnEnemy || spawnBoss)
            {
                EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);

                if (spawnEnemy)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        Entity e = ecb.Instantiate(enemy.entity);
                        ecb.SetComponent(e, LocalTransform.FromPosition(float3.zero));
                        ecb.AddComponent(e, new AgentComponent
                        {
                            unit_id = enemy.id,
                            global_id = globalIdCounter++,
                            state = 0,
                            triggerDie = false,
                            hp = GetInitialHp(enemy.id)
                        });
                    }
                    nextSpawnTime_enemy += spawnRate;
                }

                if (spawnBoss)
                {
                    Entity e = ecb.Instantiate(boss.entity);
                    ecb.SetComponent(e, LocalTransform.FromPosition(float3.zero));
                    ecb.AddComponent(e, new AgentComponent
                    {
                        unit_id = boss.id,
                        global_id = globalIdCounter++,
                        state = 0,
                        triggerDie = false,
                        hp = GetInitialHp(boss.id)
                    });
                    nextSpawnTime_boss += spawnRate * 5;
                }

                ecb.Playback(state.EntityManager);
                ecb.Dispose();
            }
        }
    }

    private static int GetInitialHp(int unitId) => unitId switch
    {
        1002 => 100,
        1003 => 200,
        _ => 100
    };
}

public partial struct AgentComponent : IComponentData
{
    public int unit_id;
    public int global_id;
    public byte state; // 0: idle, 1: move, 2: attack, 3: die
    public bool triggerDie;
    public int hp;
    public float dieTimer;
}
