using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial struct EnemySpawnerSystem : ISystem
{
    // 外部请求批量生成：count个unit_id=1002的敌人
    public static int pendingSpawn1002Count;

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
            nextSpawnTime_enemy = 5;
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
                    for (int i = 0; i < 50; i++)
                    {
                        Entity e = ecb.Instantiate(enemy.entity);
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
                    ecb.AddComponent(e, new AgentComponent
                    {
                        unit_id = boss.id,
                        global_id = globalIdCounter++,
                        state = 0,
                        triggerDie = false,
                        hp = GetInitialHp(boss.id)
                    });
                    nextSpawnTime_boss += spawnRate * 4;
                }

                ecb.Playback(state.EntityManager);
                ecb.Dispose();
            }

            // 处理外部批量生成请求
            int toSpawn = pendingSpawn1002Count;
            if (toSpawn > 0)
            {
                pendingSpawn1002Count = 0;
                EntityCommandBuffer ecb2 = new EntityCommandBuffer(Allocator.TempJob);
                for (int i = 0; i < toSpawn; i++)
                {
                    Entity e = ecb2.Instantiate(enemy.entity);
                    ecb2.AddComponent(e, new AgentComponent
                    {
                        unit_id = 1002,
                        global_id = globalIdCounter++,
                        state = 0,
                        triggerDie = false,
                        hp = GetInitialHp(1002)
                    });
                }
                ecb2.Playback(state.EntityManager);
                ecb2.Dispose();
            }
        }
    }

    private static int GetInitialHp(int unitId) => unitId switch
    {
        1002 => 200,
        1003 => 1000,
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
