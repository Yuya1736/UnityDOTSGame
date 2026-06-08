using TMPro;
using Unity.Entities;
using UnityEngine;

/// <summary>
/// 显示当前存活怪物数量，挂在 TextMeshProUGUI GameObject 上。
/// </summary>
public class EnemyCountDisplay : MonoBehaviour
{
    private TextMeshProUGUI _text;
    private EnemySystem _enemySystem;

    private void Awake()
    {
        _text = GetComponent<TextMeshProUGUI>();
    }

    private void Update()
    {
        if (_enemySystem == null)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null)
                _enemySystem = world.GetExistingSystemManaged<EnemySystem>();
        }

        int count = _enemySystem != null ? _enemySystem.etLst.Count : 0;
        _text.text = $"怪物: {count}";
    }
}
