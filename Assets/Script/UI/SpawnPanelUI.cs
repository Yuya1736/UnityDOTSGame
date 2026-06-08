using UnityEngine;
using UnityEngine.UI;

/// <summary>左侧调试面板：批量生成1002怪物 + 玩家无敌开关</summary>
public class SpawnPanelUI : MonoBehaviour
{
    [Header("引用")]
    public Button spawnButton;
    public Toggle invincibleToggle;
    public PlayerController playerController;

    [Header("单次生成数量")]
    public int spawnCount = 1000;

    void Start()
    {
        spawnButton.onClick.AddListener(OnSpawnClicked);
        invincibleToggle.onValueChanged.AddListener(OnInvincibleChanged);

        // 自动查找玩家
        if (playerController == null)
            playerController = FindObjectOfType<PlayerController>();
    }

    void OnDestroy()
    {
        spawnButton.onClick.RemoveListener(OnSpawnClicked);
        invincibleToggle.onValueChanged.RemoveListener(OnInvincibleChanged);
    }

    private void OnSpawnClicked()
    {
        EnemySpawnerSystem.pendingSpawn1002Count += spawnCount;
    }

    private void OnInvincibleChanged(bool value)
    {
        if (playerController != null)
            playerController.isInvincible = value;
    }
}
