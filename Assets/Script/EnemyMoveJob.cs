using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct EnemyMoveJob : IJobParallelFor
{
    [ReadOnly] public float3 playerPos;
    [ReadOnly] public NativeArray<float3> agentPositions; // ORCA 计算后的位置
    [ReadOnly] public NativeArray<float3> selfPositions;  // entity 当前位置
    [ReadOnly] public NativeArray<float>  speeds;
    [ReadOnly] public NativeArray<int>    unitIds;
    [ReadOnly] public float               deltaTime;

    public NativeArray<byte>       rotUpdates;
    public NativeArray<quaternion> rotations;
    public NativeArray<float3>     prefVelocities;
    public NativeArray<float3>     newPositions; // 仅 unit 1003 使用

    public void Execute(int i)
    {
        int unitId = unitIds[i];

        if (unitId == 1002)
        {
            float3 r1 = agentPositions[i] - selfPositions[i];
            if (math.lengthsq(r1) > 0f)
            {
                rotations[i]  = quaternion.LookRotation(r1, math.up());
                rotUpdates[i] = 1;
            }
            else
            {
                rotUpdates[i] = 0;
            }

            float s = speeds[i] * math.min(1f, math.distance(agentPositions[i], playerPos) / math.max(speeds[i], 0.001f));
            prefVelocities[i] = math.normalize(playerPos - agentPositions[i]) * s;
            newPositions[i]   = agentPositions[i]; // ORCA 输出的位置直接用
        }
        else if (unitId == 1003)
        {
            float3 dir = playerPos - selfPositions[i];
            if (math.lengthsq(dir) > 0f)
            {
                rotations[i]  = quaternion.LookRotation(dir, math.up());
                rotUpdates[i] = 1;
            }
            else
            {
                rotUpdates[i] = 0;
            }

            // 沿当前朝向前进，Forward = (0,0,1) 旋转后的方向
            float3 forward    = math.mul(rotations[i], new float3(0, 0, 1));
            newPositions[i]   = selfPositions[i] + forward * speeds[i] * deltaTime;
            prefVelocities[i] = float3.zero;
        }
    }
}
