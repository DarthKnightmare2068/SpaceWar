## 2025-05-14 - Multi-System Performance Optimization
**Learning:** Significant performance gains in Unity can be achieved by:
1. Replacing scene-wide searches (`GameObject.FindGameObjectsWithTag`) with cached references (e.g., `GameManager.Instance.currentPlayer`) or spatial queries (`Physics.OverlapSphereNonAlloc`).
2. Eliminating expensive square root calculations by using `Vector3.sqrMagnitude` for distance comparisons and caching squared range thresholds.
3. Throttling expensive logic (like target sorting and assignments) to lower frequencies (e.g., 2Hz-5Hz) while maintaining smooth frame-by-frame tracking/firing using cached results.
4. Reducing GC pressure by pre-allocating and reusing collection member variables instead of creating new ones in `Update`.
5. Caching `GetComponentsInChildren` results in `Start` to avoid repeated deep-tree traversals in high-frequency methods.

**Action:** Always check for `Vector3.Distance` in `Update` loops and replace with `sqrMagnitude` where possible. Use spatial partitioning for finding targets in a range. Reuse collections to minimize allocations.
