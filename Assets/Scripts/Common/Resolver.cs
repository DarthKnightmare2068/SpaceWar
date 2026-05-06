using UnityEngine;

// Centralizes the GetComponent ?? GetComponentInParent ?? player-registry-fallback chain
// repeated across most weapons. Single swap point if we ever move to DI/job-system.
public static class Resolver
{
    public static T Find<T>(Component owner) where T : Component
    {
        if (owner == null) return null;

        T c = owner.GetComponent<T>();
        if (c != null) return c;

        c = owner.GetComponentInParent<T>();
        if (c != null) return c;

        if (GameEntityRegistry.TryGetPlayerComponent(out T fromRegistry))
            return fromRegistry;

        return null;
    }
}
