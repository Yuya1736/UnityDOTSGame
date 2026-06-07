using System;
using System.Collections;
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

    public Dictionary<int, Dictionary<int, WorldUnit>> grilds_dct = new Dictionary<int, Dictionary<int, WorldUnit>>();


    public void Clear()
    {
        grilds_dct.Clear();
        foreach (var item in grilds_dct)
        {
            if (item.Value.Count == 0)
            {
                continue;
            }
            foreach (var item2 in item.Value)
            {
                item2.Value.Clear();
            }
        }
        grilds_dct.Clear();
        customId = 0;
    }


    //�ж���������Ƿ��ж�Ӧ�ĸ�����
    public WorldUnit HasGrild(Vector3 p)
    {
        var x = (int)Math.Floor(p.x / width);//3.14 3  -2.8 -3
        var z = (int)Math.Floor(p.z / width);
        if (grilds_dct.TryGetValue(z, out var z_grilds))
        {
            if (z_grilds.TryGetValue(x, out var x_grild))
            {
                return x_grild;
            }
        }
        return null;
    }

    //���������ȡ����
    public WorldUnit Get(Vector3 position)
    {
        var p = position;
        //����ȡ��
        var x = (int)Math.Floor(p.x / width);
        var z = (int)Math.Floor(p.z / width);
        if (grilds_dct.ContainsKey(z) == false)
        {
            grilds_dct[z] = new Dictionary<int, WorldUnit>();
        }

        if (grilds_dct[z].TryGetValue(x, out var g))
        {
            return g;
        }
        else
        {
            var x_min = x * width;
            var z_min = z * width;
            customId += 1;
            WorldUnit item = new WorldUnit(customId, (float)x_min, (float)(x_min + width),
                (float)z_min, (float)(z_min + width), width);

            item.z_id = z;//��ID ǰ��
            item.x_id = x;//��ID ����
            grilds_dct[z][x] = item;
            return item;
        }
    }


    //����AI��Ӧ�ĸ���
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

    //��λ�ƶ���,���������ӿ�,���¸���ID
    public WorldUnit Change(AIEntity p)
    {
        if (p != null)
        {
            if (p.grildInfo != null)
            {
                var result = p.grildInfo.ContainPoint(p.GetPosition());
                if (result == false)
                {
                    p.grildInfo.Remove(p);
                    return Set(p);
                }
                else
                {
                    return p.grildInfo;
                }
            }
            else
            {
                return Set(p);
            }
        }
        return null;
    }

    //�Ƴ�
    public void Remove(AIEntity p)
    {
        if (p != null && p.grildInfo != null)
        {
            var result = p.grildInfo.Remove(p);
            if (result)
            {
                p.grildInfo = null;
            }
        }
    }

    //���ݷ�Χ��ȡ���ӵ�����
    public int GetCount(float range)
    {
        int count = (int)Math.Ceiling(range / width);//����ȡ�� ��Χ��10.5/1 ==11������
        return count;
    }

    //���ݷ�Χ,��ȡһ����Χ�ڵĸ��ӵĶ���
    public void GetRangTarget(WorldUnit g, float range, Action<AIEntity> a)
    {
        //����Ȥ�ĸ�������
        int count = GetCount(range);
        //ȡ�����½ǵ�λ��
        int z = g.z_id - count;
        int x = g.x_id - count;
        //��������2
        var count1 = count * 2;
        if (grilds_dct.Count>0)
        {
            for (int j = 0; j <= count1; j++)
            {
                var z1 = z + j;
                //�����һ�в�����  ֱ��ִ����һ�е��ж�
                if (grilds_dct.ContainsKey(z1) == false) { continue; }

                for (int k = 0; k <= count1; k++)
                {
                    var x1 = x + k;
                    if (grilds_dct[z1].ContainsKey(x1) == false) { continue; }

                    var item = grilds_dct[z1][x1];


                    if (item.unitDct.Count > 0)
                    {
                        foreach (var pp in item.unitDct.Values)
                        {
                            a?.Invoke(pp);
                        }
                    }
                }
            }

        }
    }

    public AIEntity FindNearest(float3 pos, float range)
    {
        int count = GetCount(range);
        var x_id = (int)Math.Floor(pos.x / width);
        var z_id = (int)Math.Floor(pos.z / width);
        int z = z_id - count;
        int x = x_id - count;
        var count1 = count * 2;

        AIEntity nearest = null;
        float nearestDistSq = range * range;

        if (grilds_dct.Count > 0)
        {
            for (int j = 0; j <= count1; j++)
            {
                var z1 = z + j;
                if (!grilds_dct.ContainsKey(z1)) continue;
                for (int k = 0; k <= count1; k++)
                {
                    var x1 = x + k;
                    if (!grilds_dct[z1].ContainsKey(x1)) continue;
                    var item = grilds_dct[z1][x1];
                    foreach (var pp in item.unitDct.Values)
                    {
                        float distSq = math.distancesq(pos, pp.localTransform.Position);
                        if (distSq <= nearestDistSq)
                        {
                            nearestDistSq = distSq;
                            nearest = pp;
                        }
                    }
                }
            }
        }
        return nearest;
    }

    internal void OnHit(float3 bullet_pos, float range, EntityManager manage, EnemySystem es, Action<EntityManager, EnemySystem, AIEntity, float3, float, int> a, int bulletId, bool on_hit_destroy)
    {
        //var t = grilds_dct.Values;
        int count = GetCount(range);

        var x_id = (int)Math.Floor(bullet_pos.x / width);
        var z_id = (int)Math.Floor(bullet_pos.z / width);

        int z = z_id - count;
        int x = x_id - count;
        var count1 = count * 2;
        if (grilds_dct.Count > 0)
        {
            for (int j = 0; j <= count1; j++)
            {
                var z1 = z + j;
                if (grilds_dct.ContainsKey(z1) == false) { continue; }

                for (int k = 0; k <= count1; k++)
                {
                    var x1 = x + k;
                    if (grilds_dct[z1].ContainsKey(x1) == false) { continue; }

                    var item = grilds_dct[z1][x1];

                    if (item.unitDct.Count > 0)
                    {
                        foreach (var pp in item.unitDct.Values)
                        {
                            a?.Invoke(manage, es, pp, bullet_pos, range, bulletId);
                            if (on_hit_destroy)
                            {
                                return;
                            }
                        }
                    }
                }
            }
        }
    }
}


public class WorldUnit
{
    public int id;//���ӵ�ID

    //public int y_id;
    public int x_id;
    public int z_id;

    public float x_min;//���������x
    //public float y_min;
    public float z_min;//���ӵ���Сz

    public float x_max;
    //public float y_max;
    public float z_max;

    public float size;//ÿ�����ӵĴ�С

    //public Vector3 pos;

    //public Dictionary<string, PlayerData> unit = new Dictionary<string, PlayerData>();

    public Dictionary<int, AIEntity> unitDct = new Dictionary<int, AIEntity>(50);

    public WorldUnit(int id, float x_min, float x_max, float z_min, float z_max, int size)
    {
        this.id = id;
        this.x_min = x_min;
        this.z_min = z_min;

        //pos = new Vector3(x_min, z_min);
        this.x_max = x_max;
        this.z_max = z_max;
        this.size = size;
    }
    //��������ϻ�������е�λ
    public void Clear()
    {
        if (unitDct.Count > 0)
        {
            unitDct.Clear();
        }
    }

    //������������ӵ�λ
    public bool Add(AIEntity pd)
    {
        if (pd == null)
        {
            return false;
        }
        unitDct[pd.GetInstanceID()] = pd;
        //pd.grildInfo = this;
        return true;
    }

    public bool Remove(int _instance_id)
    {
        var id = _instance_id;
        if (unitDct.ContainsKey(id))
        {
            unitDct.Remove(id);
            return true;
        }
        return false;
    }

    public bool Remove(AIEntity pd)
    {
        if (pd == null)
        {
            return false;
        }
        var id = pd.GetInstanceID();
        if (unitDct.ContainsKey(id))
        {
            unitDct.Remove(id);
            //pd.grildInfo = null;
            return true;
        }
        return false;
    }

    //�ж����λ���Ƿ�����������ڲ�
    public bool ContainPoint(Vector3 point)
    {
        if (point.x >= x_min && point.x <= x_max && point.z >= z_min && point.z <= z_max)
        {
            return true;
        }
        return false;
    }

    //��ȡ��Χ�˸�����
    //public void AddToList(List<AIEntity> lst)
    //{
    //    if (lst != null && unitDct.Count > 0)
    //    {
    //        var valus = unitDct.Values;
    //        foreach (var item in valus)
    //        {
    //            if (item != null && item.IsActive())
    //            {
    //                lst.Add(item);
    //            }
    //        }
    //    }
    //}

}