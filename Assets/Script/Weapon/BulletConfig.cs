using UnityEngine;
using Sirenix.OdinInspector;

public enum BulletType
{
    普通子弹 = 1,
    爆炸子弹 = 2,
}

[CreateAssetMenu(menuName = "配置/创建子弹配置", fileName = "BulletConfig")]
public class BulletConfig : ScriptableObject
{
    [Header("子弹类型")]
    public BulletType bulletType = BulletType.普通子弹;

    [Header("子弹预制体")]
    public GameObject bulletPrefab;

    [Header("命中特效")]
    public GameObject hitEffect;

    [ShowIf("bulletType", BulletType.爆炸子弹)]
    [Header("爆炸特效")]
    public GameObject explosionEffect;

    [ShowIf("bulletType", BulletType.爆炸子弹)]
    [Header("爆炸半径")]
    public float explosionRadius = 3f;

    [Space(10)]
    [Header("一次发射子弹数量")]
    [MinValue(1)]
    public int bulletCount = 1;

    [Header("子弹散布角度（度）")]
    [Range(0f, 180f)]
    public float spreadAngle = 0f;

    [Header("子弹速度")]
    public float bulletSpeed = 20f;

    [Header("子弹存活时间（秒）")]
    public float bulletLifetime = 5f;
}
