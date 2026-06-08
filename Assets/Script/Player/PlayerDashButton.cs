using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 移动端冲刺按钮。改用 IPointerDownHandler 绕开 Fingers 手势冲突，
/// 移动中也能可靠触发。
/// </summary>
public class PlayerDashButton : MonoBehaviour, IPointerDownHandler
{
    [Tooltip("PlayerController 引用，留空则自动查找场景中第一个")]
    public PlayerController controller;

    [Header("视觉反馈（可选）")]
    public Image buttonImage;

    [Header("冷却显示（可选）")]
    [Tooltip("冷却遮罩 Image，Fill Method = Radial360")]
    public Image cooldownMask;

    private void Awake()
    {
        if (controller == null)
            controller = FindObjectOfType<PlayerController>();
    }

    private void Update()
    {
        UpdateCooldownMask();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (controller != null && controller.IsDashReady)
            controller.dashTrigger = true;
    }

    private void SetSprite(Sprite sprite)
    {
        if (buttonImage != null && sprite != null)
            buttonImage.sprite = sprite;
    }

    private void UpdateCooldownMask()
    {
        if (cooldownMask == null || controller == null) return;
        float ratio = controller.DashCooldownRatio;
        cooldownMask.fillAmount = ratio;
    }
}
