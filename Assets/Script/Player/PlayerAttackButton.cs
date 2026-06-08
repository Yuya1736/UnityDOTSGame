using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 移动端攻击按钮。改用 IPointerDownHandler 绕开 Fingers 手势冲突，
/// 移动中也能可靠触发。
/// </summary>
public class PlayerAttackButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Tooltip("PlayerController 引用，留空则自动查找场景中第一个")]
    public PlayerController controller;

    [Header("视觉反馈（可选）")]
    [Tooltip("按钮 Image 组件")]
    public Image buttonImage;
    [Tooltip("默认精灵")]
    public Sprite idleSprite;
    [Tooltip("按下精灵")]
    public Sprite pressedSprite;

    private void Awake()
    {
        if (controller == null)
            controller = FindObjectOfType<PlayerController>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (controller != null)
            controller.shootTrigger = true;

        SetSprite(pressedSprite);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        SetSprite(idleSprite);
    }

    private void SetSprite(Sprite sprite)
    {
        if (buttonImage != null && sprite != null)
            buttonImage.sprite = sprite;
    }
}
