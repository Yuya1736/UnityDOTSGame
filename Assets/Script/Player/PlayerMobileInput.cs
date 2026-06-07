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
        if (!moveJoystick.Executing) return;

        // PlayerController.Update 已写入键盘值，这里叠加摇杆值并限幅
        _controller.moveDir = Vector2.ClampMagnitude(
            _controller.moveDir + moveJoystick.CurrentAmount, 1.0f);
    }
}
