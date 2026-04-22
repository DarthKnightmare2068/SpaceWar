## 2025-05-15 - Throttled AI Targeting with Per-Frame Tracking
**Learning:** In Unity, heavy AI logic (like sorting lists for target assignment) can be throttled to low frequencies (e.g., 5Hz) without sacrificing visual quality, provided the tracking/rotation logic remains per-frame using cached results.
**Action:** Always separate target selection/sorting from weapon tracking. Throttle the former and keep the latter smooth.

## 2025-05-15 - Memory Reuse in Update Loops
**Learning:** Frequently creating new `List<T>` or `HashSet<T>` inside `Update()` or even throttled loops causes significant GC pressure.
**Action:** Define collections as class members and `Clear()` them before reuse to maintain zero-allocation execution paths.
