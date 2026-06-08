using DigitalRubyShared;
using UnityEngine;

/// <summary>
/// 桥接 Fingers 虚拟摇杆与 PlayerController.moveDir。
/// 挂在玩家 GameObject 上，Inspector 中拖入左摇杆引用。
/// </summary>
public class PlayerMobileInput : MonoBehaviour
{
    [Tooltip("左侧移动摇杆（FingersJoystickPrefab 实例）")]
    public FingersJoystickScript moveJoystick;

    private PlayerController _controller;

    private void Awake()
    {
        _controller = GetComponent<PlayerController>();
        if (_controller == null)
            Debug.LogError("[PlayerMobileInput] 未找到 PlayerController，请将本组件挂在玩家身上。");
    }

    private void LateUpdate()
    {
        if (_controller == null || moveJoystick == null) return;
        if (!moveJoystick.Executing)
        {
            _controller.moveDir = Vector2.zero;
            return;
        }

        _controller.moveDir = moveJoystick.CurrentAmount;
    }
}
