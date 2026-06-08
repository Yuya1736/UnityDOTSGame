using UnityEngine;

/// <summary>
/// 应用启动配置：解除移动端帧率限制，设置目标帧率。
/// </summary>
public class AppSettings : MonoBehaviour
{
    [Tooltip("目标帧率，-1 表示不限制")]
    public int targetFrameRate = 60;

    private void Awake()
    {
        // 关闭垂直同步（移动端默认 vSyncCount=1 会锁 30fps）
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = targetFrameRate;
    }
}
