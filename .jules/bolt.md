# Bolt's Journal - Performance Optimizations

## 2025-05-15 - [Inefficient Component Lookups in Enemy Systems]
**Learning:** Found that `EnemyStats.cs` and `WeaponDmgControl.cs` were frequently calling `GetComponentsInChildren<BigCanon>(true)` and `GameObject.FindObjectsOfType<BigCanon>(true)`. `GetComponentsInChildren` was being called in `Update()` and `TakeDamage()`, leading to significant overhead, especially as the number of enemies increases. `FindObjectsOfType` is even more expensive as it scans the entire scene.
**Action:** Implement caching for these components during initialization (`Awake`/`Start`) and use the cached references in performance-critical methods. This reduces O(N*M) or O(SceneSize) operations to O(1) lookups per frame.
