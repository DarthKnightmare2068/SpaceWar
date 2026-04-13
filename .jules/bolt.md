## 2024-04-13 - Scene-wide searches in Unity targeting systems
**Learning:** `GameObject.FindGameObjectsWithTag` is a massive performance killer in Unity, especially when called inside high-frequency updates (like targeting). It performs a linear search through the entire scene's hierarchy.
**Action:** Always replace scene-wide tag searches with direct references from a manager (like `GameManager`) that already maintains lists of active entities.

## 2024-04-13 - Math optimization for distance checks
**Learning:** `Vector3.Distance` calls `Mathf.Sqrt` internally, which is expensive when executed hundreds of times per second.
**Action:** Use `sqrMagnitude` for distance comparisons whenever possible. Cache squared range values to avoid repeated multiplications.
