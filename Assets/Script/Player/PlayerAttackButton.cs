using DigitalRubyShared;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 移动端攻击按钮，仿照 FingersJoystickScript 模式。
/// 挂在攻击按钮 RectTransform 上，用 TapGestureRecognizer 检测触点。
/// Inspector 中拖入 PlayerController 引用（或自动 FindObjectOfType）。
/// </summary>
public class PlayerAttackButton : MonoBehaviour
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

    private TapGestureRecognizer _tapGesture;
    private RectTransform _rect;

    private void Awake()
    {
        _rect = transform as RectTransform;
        if (controller == null)
            controller = FindObjectOfType<PlayerController>();
    }

    private void OnEnable()
    {
        _tapGesture = new TapGestureRecognizer();
        _tapGesture.StateUpdated += OnTap;
        // 允许与摇杆手势同时识别
        _tapGesture.AllowSimultaneousExecutionWithAllGestures();
        FingersScript.Instance.AddGesture(_tapGesture);
    }

    private void OnDisable()
    {
        if (FingersScript.HasInstance)
            FingersScript.Instance.RemoveGesture(_tapGesture);
        _tapGesture = null;
    }

    private void OnTap(GestureRecognizer gesture)
    {
        if (gesture.State == GestureRecognizerState.Ended)
        {
            // 仅响应落在本按钮区域内的触点
            if (!RectTransformUtility.RectangleContainsScreenPoint(
                    _rect, new Vector2(gesture.FocusX, gesture.FocusY)))
                return;

            if (controller != null)
                controller.shootTrigger = true;

            SetSprite(idleSprite);
        }
        else if (gesture.State == GestureRecognizerState.Began)
        {
            if (!RectTransformUtility.RectangleContainsScreenPoint(
                    _rect, new Vector2(gesture.FocusX, gesture.FocusY)))
                return;

            SetSprite(pressedSprite);
        }
    }

    private void SetSprite(Sprite sprite)
    {
        if (buttonImage != null && sprite != null)
            buttonImage.sprite = sprite;
    }
}
