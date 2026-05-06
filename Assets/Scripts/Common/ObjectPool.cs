using System.Collections.Generic;
using UnityEngine;

// Reusable Queue-based object pool. Replaces hand-rolled Queue<T> + GetX/ReleaseX pairs across
// DmgPopUp, AudioSetting, and (delegated) BulletPool/PlayerProjectilePool. Keep their public APIs
// stable — this just centralizes the pattern.
public sealed class ObjectPool<T> where T : Component
{
    private readonly Queue<T> pool = new Queue<T>();
    private readonly T prefab;
    private readonly Transform parent;
    private readonly System.Action<T> onGet;
    private readonly System.Action<T> onRelease;

    public int Count => pool.Count;

    public ObjectPool(T prefab, int prewarm = 0, Transform parent = null,
                      System.Action<T> onGet = null, System.Action<T> onRelease = null)
    {
        this.prefab = prefab;
        this.parent = parent;
        this.onGet = onGet;
        this.onRelease = onRelease;
        for (int i = 0; i < prewarm; i++)
        {
            T item = Instantiate();
            if (item == null) break;
            item.gameObject.SetActive(false);
            pool.Enqueue(item);
        }
    }

    private T Instantiate()
    {
        if (prefab == null) return null;
        return parent != null ? Object.Instantiate(prefab, parent) : Object.Instantiate(prefab);
    }

    public T Get()
    {
        T item = null;
        while (pool.Count > 0 && item == null) item = pool.Dequeue();
        if (item == null) item = Instantiate();
        if (item == null) return null;
        item.gameObject.SetActive(true);
        onGet?.Invoke(item);
        return item;
    }

    public T Get(Vector3 position, Quaternion rotation)
    {
        T item = Get();
        if (item != null)
        {
            item.transform.SetPositionAndRotation(position, rotation);
        }
        return item;
    }

    public void Release(T item)
    {
        if (item == null) return;
        onRelease?.Invoke(item);
        item.gameObject.SetActive(false);
        pool.Enqueue(item);
    }

    public void Clear(bool destroy = true)
    {
        while (pool.Count > 0)
        {
            T item = pool.Dequeue();
            if (item != null && destroy) Object.Destroy(item.gameObject);
        }
    }
}
