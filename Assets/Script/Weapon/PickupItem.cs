using JKFrame;
using UnityEngine;

// 掉落物拾取组件 —— 挂在 QstTiltedNeon 等掉落物预制体根节点上
public class PickupItem : MonoBehaviour
{
    [Header("拾取半径")]
    public float pickupRadius = 1.5f;

    [Header("悬浮振幅 & 频率")]
    public float bobAmplitude = 0.3f;
    public float bobFrequency = 1.5f;

    [Header("旋转速度（度/秒）")]
    public float rotateSpeed = 90f;

    [Header("自动销毁时间（秒）")]
    public float autoDestroyTime = 20f;

    private float _baseY;
    private float _spawnTime;
    private Transform _playerTransform;
    private BulletSpawner _bulletSpawner;
    private bool _collected;

    void Start()
    {
        CachePlayerRefs();
        _baseY = transform.position.y;
        _spawnTime = Time.time;
    }

    // 从对象池取出后调用，重置状态
    public void ResetState()
    {
        _collected = false;
        _baseY = transform.position.y;
        _spawnTime = Time.time;
        CachePlayerRefs();
    }

    void CachePlayerRefs()
    {
        if (_playerTransform != null) return;
        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            _playerTransform = player.transform;
            _bulletSpawner = player.GetComponent<BulletSpawner>();
        }
    }

    void Update()
    {
        if (_collected) return;

        if (Time.time - _spawnTime >= autoDestroyTime)
        {
            Collect();
            return;
        }

        // 悬浮 + 旋转动效
        float y = _baseY + Mathf.Sin(Time.time * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
        transform.position = new Vector3(transform.position.x, y, transform.position.z);
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);

        if (_playerTransform == null) return;

        if (Vector3.Distance(transform.position, _playerTransform.position) <= pickupRadius)
            Collect();
    }

    void Collect()
    {
        _collected = true;
        _bulletSpawner?.SwitchRandomConfig();
        gameObject.SetActive(false);
        PoolSystem.PushGameObject(gameObject.name, gameObject);
    }
}
