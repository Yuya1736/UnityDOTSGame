using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public class BulletVisualTracker : MonoBehaviour
{
    private Entity _entity;
    private EntityManager _em;
    private bool _initialized;

    public void Init(Entity e)
    {
        _entity = e;
        _em = World.DefaultGameObjectInjectionWorld.EntityManager;
        _initialized = true;
    }

    void Update()
    {
        if (!_initialized) return;

        if (!_em.Exists(_entity))
        {
            Destroy(gameObject);
            return;
        }

        var lt = _em.GetComponentData<LocalTransform>(_entity);
        transform.position = lt.Position;
    }
}
