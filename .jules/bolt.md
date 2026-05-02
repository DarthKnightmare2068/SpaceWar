## 2025-05-14 - Robust Per-Frame Cache Invalidation
**Learning:** In Unity abstract classes, resetting a per-frame cache (e.g., 'hasCachedHit = false') in 'Update()' is risky because subclasses might override 'Update()' without calling 'base.Update()'.
**Action:** Use 'Time.frameCount' for cache invalidation (e.g., 'if (lastCachedFrame != Time.frameCount)') to ensure the cache is refreshed every frame regardless of inheritance chain behavior.

## 2025-05-14 - Range Regressions in Unified Physics Queries
**Learning:** When consolidating multiple physics queries into a single cached call, failing to use the maximum required distance can lead to functional regressions (e.g., a weapon with 500m range using a 200m cached raycast).
**Action:** Always identify and use the 'Mathf.Max' of all potential query distances when unifying physics engine calls.
