using System;
using System.Collections.Generic;

using Unity.Entities;
using Unity.Mathematics;

using UnityEngine;

public class WorldUnitManager
{
    static WorldUnitManager instance = new WorldUnitManager();
    public static WorldUnitManager Instance => instance;

    public int width = 1;
    int customId = 0;

    public Dictionary<long, WorldUnit> grilds_dct = new Dictionary<long, WorldUnit>();

    static long MakeKey(int z, int x) => ((long)z << 32) | (uint)x;

    public void Clear()
    {
        foreach (var cell in grilds_dct.Values)
            cell.Clear();
        grilds_dct.Clear();
        customId = 0;
    }

    public WorldUnit HasGrild(Vector3 p)
    {
        long key = MakeKey((int)Math.Floor(p.z / width), (int)Math.Floor(p.x / width));
        grilds_dct.TryGetValue(key, out var g);
        return g;
    }

    public WorldUnit Get(Vector3 position)
    {
        var x = (int)Math.Floor(position.x / width);
        var z = (int)Math.Floor(position.z / width);
        long key = MakeKey(z, x);
        if (grilds_dct.TryGetValue(key, out var g)) return g;

        var item = new WorldUnit(++customId,
            x * width, (x + 1) * width,
            z * width, (z + 1) * width, width);
        item.z_id = z;
        item.x_id = x;
        grilds_dct[key] = item;
        return item;
    }

    public WorldUnit Set(AIEntity p)
    {
        if (p != null)
        {
            var g = Get(p.GetPosition());
            if (g != null)
            {
                g.Add(p);
                p.grildInfo = g;
                return g;
            }
        }
        return null;
    }

    public WorldUnit Change(AIEntity p)
    {
        if (p != null)
        {
            if (p.grildInfo != null)
            {
                if (!p.grildInfo.ContainPoint(p.GetPosition()))
                {
                    p.grildInfo.Remove(p);
                    return Set(p);
                }
                return p.grildInfo;
            }
            return Set(p);
        }
        return null;
    }

    public void Remove(AIEntity p)
    {
        if (p != null && p.grildInfo != null)
        {
            if (p.grildInfo.Remove(p))
                p.grildInfo = null;
        }
    }

    public int GetCount(float range)
    {
        return (int)Math.Ceiling(range / width);
    }

    public void GetRangTarget(WorldUnit g, float range, Action<AIEntity> a)
    {
        int count  = GetCount(range);
        int count1 = count * 2;
        int z0     = g.z_id - count;
        int x0     = g.x_id - count;

        for (int j = 0; j <= count1; j++)
        {
            for (int k = 0; k <= count1; k++)
            {
                if (!grilds_dct.TryGetValue(MakeKey(z0 + j, x0 + k), out var cell)) continue;
                foreach (var pp in cell.unitDct.Values)
                    a?.Invoke(pp);
            }
        }
    }

    public AIEntity FindNearest(float3 pos, float range)
    {
        int count  = GetCount(range);
        int x_id   = (int)Math.Floor(pos.x / width);
        int z_id   = (int)Math.Floor(pos.z / width);
        int count1 = count * 2;
        int z0     = z_id - count;
        int x0     = x_id - count;

        AIEntity nearest      = null;
        float nearestDistSq   = range * range;

        for (int j = 0; j <= count1; j++)
        {
            for (int k = 0; k <= count1; k++)
            {
                if (!grilds_dct.TryGetValue(MakeKey(z0 + j, x0 + k), out var cell)) continue;
                foreach (var pp in cell.unitDct.Values)
                {
                    float distSq = math.distancesq(pos, pp.localTransform.Position);
                    if (distSq <= nearestDistSq)
                    {
                        nearestDistSq = distSq;
                        nearest       = pp;
                    }
                }
            }
        }
        return nearest;
    }

    internal void OnHit(float3 bullet_pos, float range, EntityManager manage, EnemySystem es,
        Action<EntityManager, EnemySystem, AIEntity, float3, float, int> a, int bulletId, bool on_hit_destroy)
    {
        int count  = GetCount(range);
        int x_id   = (int)Math.Floor(bullet_pos.x / width);
        int z_id   = (int)Math.Floor(bullet_pos.z / width);
        int count1 = count * 2;
        int z0     = z_id - count;
        int x0     = x_id - count;

        for (int j = 0; j <= count1; j++)
        {
            for (int k = 0; k <= count1; k++)
            {
                if (!grilds_dct.TryGetValue(MakeKey(z0 + j, x0 + k), out var cell)) continue;
                foreach (var pp in cell.unitDct.Values)
                {
                    a?.Invoke(manage, es, pp, bullet_pos, range, bulletId);
                    if (on_hit_destroy) return;
                }
            }
        }
    }
}


public class WorldUnit
{
    public int id;
    public int x_id;
    public int z_id;

    public float x_min;
    public float z_min;
    public float x_max;
    public float z_max;
    public float size;

    public Dictionary<int, AIEntity> unitDct = new Dictionary<int, AIEntity>(50);

    public WorldUnit(int id, float x_min, float x_max, float z_min, float z_max, int size)
    {
        this.id    = id;
        this.x_min = x_min;
        this.z_min = z_min;
        this.x_max = x_max;
        this.z_max = z_max;
        this.size  = size;
    }

    public void Clear()
    {
        unitDct.Clear();
    }

    public bool Add(AIEntity pd)
    {
        if (pd == null) return false;
        unitDct[pd.GetInstanceID()] = pd;
        return true;
    }

    public bool Remove(int _instance_id)
    {
        return unitDct.Remove(_instance_id);
    }

    public bool Remove(AIEntity pd)
    {
        if (pd == null) return false;
        return unitDct.Remove(pd.GetInstanceID());
    }

    public bool ContainPoint(Vector3 point)
    {
        return point.x >= x_min && point.x <= x_max && point.z >= z_min && point.z <= z_max;
    }
}
