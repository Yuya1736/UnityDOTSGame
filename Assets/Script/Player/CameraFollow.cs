using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float distance = 10f;
    public float mouseSensitivity = 3f;
    public float touchSensitivity = 0.1f;
    public float smoothSpeed = 8f;
    public float pitchMin = 10f;
    public float pitchMax = 70f;

    private float yaw;
    private float pitch = 30f;

    // 触摸摄像机控制的追踪指针 ID，-1 表示没有
    private int _cameraTouchId = -1;
    private Vector2 _lastTouchPos;

    void Start()
    {
#if !UNITY_ANDROID && !UNITY_IOS
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
#endif
    }

    void LateUpdate()
    {
        if (target == null) return;

        float deltaX = 0f;
        float deltaY = 0f;

#if UNITY_ANDROID || UNITY_IOS
        GetMobileCameraDelta(ref deltaX, ref deltaY);
#else
        deltaX = Input.GetAxis("Mouse X") * mouseSensitivity;
        deltaY = Input.GetAxis("Mouse Y") * mouseSensitivity;
#endif

        yaw   += deltaX;
        pitch -= deltaY;
        pitch  = Mathf.Clamp(pitch, pitchMin, pitchMax);

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0);
        Vector3 desiredPos = target.position + rot * new Vector3(0, 0, -distance);

        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
        transform.LookAt(target.position + Vector3.up * 1.2f);
    }

    // 只接受屏幕右半部分的触摸来旋转摄像机，左半部分留给摇杆
    private void GetMobileCameraDelta(ref float deltaX, ref float deltaY)
    {
        float halfWidth = Screen.width * 0.5f;

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch t = Input.GetTouch(i);

            if (t.fingerId == _cameraTouchId)
            {
                if (t.phase == TouchPhase.Moved)
                {
                    deltaX = t.deltaPosition.x * touchSensitivity;
                    deltaY = t.deltaPosition.y * touchSensitivity;
                }
                else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                {
                    _cameraTouchId = -1;
                }
                return;
            }
        }

        // 没有正在追踪的触点，寻找右半屏的新触点
        if (_cameraTouchId == -1)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                if (t.phase == TouchPhase.Began && t.position.x > halfWidth)
                {
                    _cameraTouchId = t.fingerId;
                    _lastTouchPos = t.position;
                    return;
                }
            }
        }
    }
}
