## 2025-05-14 - Optimized TurretsManager Targeting Logic
**Learning:** High-frequency targeting logic in Unity (sorting, searching, and distance checks) can be a major CPU bottleneck if run every frame. Separation of "targeting decision" (heavy, low frequency) from "tracking/rotation" (light, high frequency) is critical for performance.
**Action:** Always throttle heavy collection-based logic (Sort, FindWithTag) to a lower frequency (e.g., 2-10Hz) and use per-frame loops only for lightweight visual/physical tracking using cached targets.
