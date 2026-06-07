using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public class BulletVisualTracker : MonoBehaviour
{
    private Entity _entity;
    private EntityManager _em;
    private string _prefabName;
    private bool _initialized;

    public void Init(Entity e, string prefabName)
    {
        _entity = e;
        _prefabName = prefabName;
        _em = World.DefaultGameObjectInjectionWorld.EntityManager;
        _initialized = true;
    }

    void Update()
    {
        if (!_initialized) return;

        if (!_em.Exists(_entity))
        {
            _initialized = false;
            EffectPool.Instance.Return(_prefabName, gameObject);
            return;
        }

        var lt = _em.GetComponentData<LocalTransform>(_entity);
        transform.position = lt.Position;
    }
}
