using System.Collections;
using JKFrame;
using UnityEngine;

public class EffectPool : MonoBehaviour
{
    private static EffectPool _instance;
    public static EffectPool Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("EffectPool");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<EffectPool>();
            }
            return _instance;
        }
    }

    void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Spawn(GameObject prefab, Vector3 position, Quaternion rotation, float lifetime)
    {
        var go = GetOrCreate(prefab);
        go.transform.SetPositionAndRotation(position, rotation);
        go.SetActive(true);
        StartCoroutine(ReturnAfter(prefab.name, go, lifetime));
    }

    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        var go = GetOrCreate(prefab);
        go.transform.SetPositionAndRotation(position, rotation);
        go.SetActive(true);
        return go;
    }

    public void Return(string key, GameObject instance)
    {
        instance.SetActive(false);
        PoolSystem.PushGameObject(key, instance);
    }

    private GameObject GetOrCreate(GameObject prefab)
    {
        var go = PoolSystem.GetGameObject(prefab.name);
        if (go == null)
        {
            go = Instantiate(prefab);
            go.name = prefab.name;
        }
        return go;
    }

    private IEnumerator ReturnAfter(string key, GameObject instance, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (instance != null && instance.activeSelf)
            Return(key, instance);
    }
}
