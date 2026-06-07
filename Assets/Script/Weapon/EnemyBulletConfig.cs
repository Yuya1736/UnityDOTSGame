using UnityEngine;

[CreateAssetMenu(menuName = "Config/EnemyBulletConfig", fileName = "EnemyBulletConfig")]
public class EnemyBulletConfig : ScriptableObject
{
    [Header("关联的 unit_id（如 1003）")]
    public int unitId;

    [Header("子弹类型")]
    public BulletType bulletType = BulletType.Normal;

    [Header("子弹速度")]
    public float bulletSpeed = 12f;

    [Header("子弹存活时间（秒）")]
    public float bulletLifetime = 4f;

    [Header("对玩家的伤害值")]
    public int damage = 10;

    [Header("命中检测半径")]
    public float hitRadius = 1.2f;

    [Header("触发攻击的距离")]
    public float attackRange = 15f;

    [Header("攻击冷却时间（秒）")]
    public float attackCooldown = 3f;

    [Header("每次发射子弹数量")]
    public int bulletCount = 1;

    [Header("子弹散布角度")]
    public float spreadAngle = 0f;

    [Header("碰撞检测偏移")]
    public Vector3 collisionOffset;

    [Header("命中特效预制体")]
    public GameObject hitEffect;

    [Header("子弹拖尾预制体")]
    public GameObject bulletVisualPrefab;
}
