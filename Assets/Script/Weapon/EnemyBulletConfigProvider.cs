using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;

/// <summary>
/// 挂载在场景中，持有所有敌人子弹配置的引用，供 EnemySystem 在运行时读取。
/// 在 Inspector 中将 EnemyBulletConfig1003 等 asset 拖入 configs 列表。
/// </summary>
[Preserve]
public class EnemyBulletConfigProvider : MonoBehaviour
{
    [Preserve]
    public List<EnemyBulletConfig> configs = new List<EnemyBulletConfig>();
}
