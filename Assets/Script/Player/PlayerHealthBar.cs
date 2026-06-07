using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 显示玩家血条，挂在任意 GameObject 上。
/// Inspector 中拖入 PlayerController 和 Slider 引用。
/// Slider 放在屏幕顶部 Canvas 中（Screen Space - Overlay）。
/// </summary>
public class PlayerHealthBar : MonoBehaviour
{
    [Tooltip("PlayerController 引用，留空则自动查找")]
    public PlayerController controller;

    [Tooltip("血条 Slider")]
    public Slider healthSlider;

    private void Awake()
    {
        if (controller == null)
            controller = FindObjectOfType<PlayerController>();
    }

    private void Update()
    {
        if (controller == null || healthSlider == null) return;
        // maxHp 防止除零
        float ratio = controller.maxHp > 0
            ? (float)controller.currentHp / controller.maxHp
            : 0f;
        healthSlider.value = Mathf.Clamp01(ratio);
    }
}
